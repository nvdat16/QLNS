#!/usr/bin/env python3
"""End-to-end smoke test for the Identity & Access API (ADM-01, ADM-02) against a real PostgreSQL.

Prerequisites (see src/backend/README.md, "Local testing"):
  1. PostgreSQL loaded with database/schema.sql, database/seed_roles.sql and database/seed_dev.sql.
  2. API running in Development:  ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Qlns.Api
Usage:  python3 tests/smoke/identity_smoke.py [http://localhost:5080]

The script provisions a throwaway account with a random e-mail on every run, so locking it out and resetting
its password leaves the seeded accounts untouched and the script stays re-runnable.
Lines starting with "!!" are results that differ from the expected HTTP status.
"""
import json, sys, urllib.request, urllib.error, uuid

B = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://localhost:5080"
SEED_PASSWORD = "Qlns@2026"

def req(label, method, path, token=None, body=None, ifmatch=None, expect=None):
    hdr = {}
    if token: hdr["Authorization"] = f"Bearer {token}"
    if ifmatch: hdr["If-Match"] = f'"{ifmatch}"'
    data = None
    if body is not None:
        data = json.dumps(body).encode(); hdr["Content-Type"] = "application/json"
    r = urllib.request.Request(B + path, data=data, method=method, headers=hdr)
    try:
        with urllib.request.urlopen(r) as resp: status, raw, etag = resp.status, resp.read(), resp.headers.get("ETag")
    except urllib.error.HTTPError as e: status, raw, etag = e.code, e.read(), None
    try: js = json.loads(raw) if raw else None
    except Exception: js = raw[:60]
    summ = ""
    if isinstance(js, dict):
        d = {k: js[k] for k in ("code", "id", "email", "status", "version", "mustChangePassword", "expiresIn") if k in js}
        if "errors" in js: d["errors"] = list(js["errors"].keys())
        if "unknownRoles" in js: d["unknownRoles"] = js["unknownRoles"]
        if "retryAfterSeconds" in js: d["retryAfter"] = js["retryAfterSeconds"]
        if "refreshToken" in js: d["refresh"] = "yes" if js["refreshToken"] else "none"
        if "user" in js and isinstance(js["user"], dict):
            d["perms"] = len(js["user"]["permissions"]); d["scope"] = js["user"]["dataScope"]
            d["pwdChange"] = js["user"]["passwordChangeRequired"]
        if "page" in js and isinstance(js["page"], dict): d["total"] = js["page"]["totalItems"]
        if "roles" in js and isinstance(js["roles"], list): d["roles"] = len(js["roles"])
        summ = json.dumps(d, ensure_ascii=False)
    elif isinstance(js, list): summ = f"list[{len(js)}]"
    ok = "  " if expect is None or expect == status else "!!"
    print(f"{ok} {method:<6} {label:<52} -> {status}{(' ETag='+etag) if etag else '':<10} {summ}")
    return status, js, etag

def login(label, email, password, expect=200):
    return req(label, "POST", "/api/v1/auth/login", body={"email": email, "password": password}, expect=expect)

print("== ADM-01 sign-in ==")
_, hr, _ = login("hr-manager, correct password", "hr.manager@qlns.local", SEED_PASSWORD)
HR = hr["accessToken"]; HR_REFRESH = hr["refreshToken"]
login("wrong password -> 401 invalid_credentials", "hr.manager@qlns.local", "not-the-password", expect=401)
login("unknown e-mail -> same 401, no enumeration", "nobody@qlns.local", SEED_PASSWORD, expect=401)
login("e-mail is case-insensitive", "HR.Manager@QLNS.local", SEED_PASSWORD)
req("me with token", "GET", "/api/v1/auth/me", HR, expect=200)
req("me without token -> 401", "GET", "/api/v1/auth/me", expect=401)

print("== ADM-01 refresh rotation ==")
_, renewed, _ = req("refresh rotates the token", "POST", "/api/v1/auth/refresh", body={"refreshToken": HR_REFRESH}, expect=200)
req("replaying the consumed token -> 401", "POST", "/api/v1/auth/refresh", body={"refreshToken": HR_REFRESH}, expect=401)
req("successor was revoked by reuse detection", "POST", "/api/v1/auth/refresh", body={"refreshToken": renewed["refreshToken"]}, expect=401)
_, hr2, _ = login("sign in again after the family was dropped", "hr.manager@qlns.local", SEED_PASSWORD)
req("logout", "POST", "/api/v1/auth/logout", body={"refreshToken": hr2["refreshToken"]}, expect=204)
req("logout is idempotent and silent", "POST", "/api/v1/auth/logout", body={"refreshToken": hr2["refreshToken"]}, expect=204)
req("refresh after logout -> 401", "POST", "/api/v1/auth/refresh", body={"refreshToken": hr2["refreshToken"]}, expect=401)

print("== ADM-01 restricted session (seeded with must_change_password, no role) ==")
_, ceo, _ = login("ceo signs in", "ceo@qlns.local", SEED_PASSWORD)
req("no refresh token, no permission", "GET", "/api/v1/auth/me", ceo["accessToken"], expect=200)
req("restricted session cannot read employees -> 403", "GET", "/api/v1/employees", ceo["accessToken"], expect=403)

print("== ADM-02 authorization ==")
req("hr-manager lacks admin.user.read -> 403", "GET", "/api/v1/admin/users", HR, expect=403)
_, admin, _ = login("super admin signs in", "admin@qlns.local", SEED_PASSWORD)
AD = admin["accessToken"]
req("role catalogue", "GET", "/api/v1/admin/roles", AD, expect=200)
req("admin cannot read employees -> 403", "GET", "/api/v1/employees", AD, expect=403)
req("list accounts", "GET", "/api/v1/admin/users?pageSize=5", AD, expect=200)
req("bad status filter -> 422", "GET", "/api/v1/admin/users?status=nope", AD, expect=422)

print("== ADM-02 provisioning ==")
EMAIL = f"smoke.{uuid.uuid4().hex[:8]}@qlns.local"
FIRST, SECOND = "Smoke-Test-Pass-1", "Smoke-Test-Pass-2"
req("weak initial password -> 422", "POST", "/api/v1/admin/users", AD,
    {"email": EMAIL, "displayName": "Smoke Test", "initialPassword": "short"}, expect=422)
req("unknown role -> 409", "POST", "/api/v1/admin/users", AD,
    {"email": EMAIL, "displayName": "Smoke Test", "initialPassword": FIRST,
     "roles": [{"roleCode": "ROLE_NOPE", "dataScopeType": "organization"}]}, expect=409)
_, created, etag = req("create account", "POST", "/api/v1/admin/users", AD,
    {"email": EMAIL, "displayName": "Smoke Test", "initialPassword": FIRST,
     "roles": [{"roleCode": "ROLE_EMPLOYEE", "dataScopeType": "self"}]}, expect=201)
UID, V = created["id"], created["version"]
req("duplicate e-mail -> 409", "POST", "/api/v1/admin/users", AD,
    {"email": EMAIL, "displayName": "Duplicate", "initialPassword": FIRST}, expect=409)
req("read it back", "GET", f"/api/v1/admin/users/{UID}", AD, expect=200)
req("stale If-Match -> 409", "PUT", f"/api/v1/admin/users/{UID}", AD,
    {"email": EMAIL, "displayName": "Renamed"}, ifmatch=V + 99, expect=409)
_, updated, _ = req("rename", "PUT", f"/api/v1/admin/users/{UID}", AD,
    {"email": EMAIL, "displayName": "Smoke Test Renamed"}, ifmatch=V, expect=200)
V = updated["version"]

print("== ADM-02 role grants ==")
req("department scope without an id -> 422", "PUT", f"/api/v1/admin/users/{UID}/roles", AD,
    {"roles": [{"roleCode": "ROLE_LINE_MGR", "dataScopeType": "department"}]}, ifmatch=V, expect=422)
req("unknown department -> 409", "PUT", f"/api/v1/admin/users/{UID}/roles", AD,
    {"roles": [{"roleCode": "ROLE_LINE_MGR", "dataScopeType": "department", "dataScopeId": 99999}]}, ifmatch=V, expect=409)
_, granted, _ = req("grant line manager on departments 2 and 4", "PUT", f"/api/v1/admin/users/{UID}/roles", AD,
    {"roles": [{"roleCode": "ROLE_LINE_MGR", "dataScopeType": "department", "dataScopeId": 2},
               {"roleCode": "ROLE_LINE_MGR", "dataScopeType": "department", "dataScopeId": 4}]}, ifmatch=V, expect=200)
V = granted["version"]

print("== ADM-02 status and self-management guard ==")
req("admin cannot disable their own account -> 403", "POST", f"/api/v1/admin/users/{admin['user']['userId']}/disable",
    AD, ifmatch=1, expect=403)
_, disabled, _ = req("disable the account", "POST", f"/api/v1/admin/users/{UID}/disable", AD, ifmatch=V, expect=200)
V = disabled["version"]
req("disabling again is idempotent", "POST", f"/api/v1/admin/users/{UID}/disable", AD, ifmatch=1, expect=200)
login("disabled account cannot sign in -> 401", EMAIL, FIRST, expect=401)
_, enabled, _ = req("enable it again", "POST", f"/api/v1/admin/users/{UID}/enable", AD, ifmatch=V, expect=200)
V = enabled["version"]

print("== ADM-01 forced password change ==")
_, pending, _ = login("first sign-in of the new account", EMAIL, FIRST)
NEW = pending["accessToken"]
req("the restricted session cannot read employees -> 403", "GET", "/api/v1/employees", NEW, expect=403)
req("wrong current password -> 401", "POST", "/api/v1/auth/change-password", NEW,
    {"currentPassword": "wrong-password", "newPassword": SECOND}, expect=401)
req("new password containing the e-mail name -> 422", "POST", "/api/v1/auth/change-password", NEW,
    {"currentPassword": FIRST, "newPassword": f"{EMAIL.split('@')[0]}-Aa1"}, expect=422)
req("new password equal to the current one -> 422", "POST", "/api/v1/auth/change-password", NEW,
    {"currentPassword": FIRST, "newPassword": FIRST}, expect=422)
req("change password", "POST", "/api/v1/auth/change-password", NEW,
    {"currentPassword": FIRST, "newPassword": SECOND}, expect=204)
_, full, _ = login("sign in with the new password -> full session", EMAIL, SECOND)
req("granted permissions now work", "GET", "/api/v1/employees?pageSize=1", full["accessToken"], expect=200)

print("== ADM-02 password reset ==")
req("admin cannot reset their own password -> 403", "POST", f"/api/v1/admin/users/{admin['user']['userId']}/password-reset",
    AD, {"newPassword": "Irrelevant-Pass-1"}, expect=403)
req("reset the account's password", "POST", f"/api/v1/admin/users/{UID}/password-reset", AD,
    {"newPassword": FIRST}, expect=200)
req("the reset revoked the open session", "POST", "/api/v1/auth/refresh",
    body={"refreshToken": full["refreshToken"]}, expect=401)
login("the old password no longer works -> 401", EMAIL, SECOND, expect=401)

print("== ADM-01 lockout ==")
for attempt in range(1, 6):
    login(f"failed attempt {attempt}/5", EMAIL, "definitely-wrong", expect=401)
login("6th attempt -> 401 account_locked with retryAfter", EMAIL, FIRST, expect=401)
req("reset clears the lockout counters", "POST", f"/api/v1/admin/users/{UID}/password-reset", AD,
    {"newPassword": SECOND}, expect=200)
login("sign-in works again right away", EMAIL, SECOND)
