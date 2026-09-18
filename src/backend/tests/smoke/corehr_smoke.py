#!/usr/bin/env python3
"""End-to-end smoke test for the Core HR API against a real PostgreSQL.

Prerequisites (see src/backend/README.md, "Local testing"):
  1. PostgreSQL loaded with database/schema.sql, database/seed_roles.sql and database/seed_dev.sql (fresh seed each run).
  2. API running in Development:  ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Qlns.Api
Usage:  python3 tests/smoke/corehr_smoke.py [http://localhost:5080]
Lines starting with "!!" are results that differ from the expected HTTP status.
"""
import json, sys, urllib.request, urllib.error, uuid, subprocess
B = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://localhost:5080"
def tok(p):
    # Development-only persona shortcut. The real sign-in path is covered by tests/smoke/identity_smoke.py.
    with urllib.request.urlopen(f"{B}/dev/token?persona={p}") as r: return json.load(r)["token"]
HR, EMP, LM = tok("hr-manager"), tok("employee"), tok("line-manager")
def req(label, method, path, token=None, body=None, ifmatch=None, ct="application/json", multipart=None, expect=None):
    hdr = {}
    if token: hdr["Authorization"] = f"Bearer {token}"
    if ifmatch: hdr["If-Match"] = f'"{ifmatch}"'
    data = None
    if multipart:
        bnd = uuid.uuid4().hex
        parts = b""
        for k, v in multipart.items():
            if isinstance(v, tuple):
                fn, c, ctype = v
                parts += f'--{bnd}\r\nContent-Disposition: form-data; name="{k}"; filename="{fn}"\r\nContent-Type: {ctype}\r\n\r\n'.encode() + c + b"\r\n"
            else:
                parts += f'--{bnd}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode()
        data = parts + f"--{bnd}--\r\n".encode(); hdr["Content-Type"] = f"multipart/form-data; boundary={bnd}"
    elif body is not None:
        data = json.dumps(body).encode(); hdr["Content-Type"] = ct
    r = urllib.request.Request(B + path, data=data, method=method, headers=hdr)
    try:
        with urllib.request.urlopen(r) as resp: status, raw, etag = resp.status, resp.read(), resp.headers.get("ETag")
    except urllib.error.HTTPError as e: status, raw, etag = e.code, e.read(), None
    try: js = json.loads(raw) if raw else None
    except Exception: js = raw[:60]
    summ = ""
    if isinstance(js, dict):
        keys = [k for k in ("code","totalItems","id","version","phone","personalEmail","name","overdue","eventType","documentType","expiresAt") if k in js]
        d = {k: js[k] for k in keys}
        if "status" in js and isinstance(js["status"], str): d["status"] = js["status"]
        if "page" in js and isinstance(js["page"], dict): d["total"] = js["page"]["totalItems"]
        if "items" in js: d["items"] = len(js["items"])
        if "errors" in js: d["errors"] = list(js["errors"].keys())
        for k in ("childDepartments","employees","openRequisitions","conflictingEventId","fields"):
            if k in js: d[k] = js[k]
        summ = json.dumps(d, ensure_ascii=False)
    elif isinstance(js, list): summ = f"list[{len(js)}]"
    ok = "  " if expect is None or expect == status else "!!"
    print(f"{ok} {method:<6} {label:<50} -> {status}{(' ETag='+etag) if etag else '':<10} {summ}")
    return status, js, etag
print("== Auth ==")
req("no token -> 401", "GET", "/api/v1/employees", expect=401)
req("employee lacks org.manage -> 403", "POST", "/api/v1/organization/departments", EMP, {"code":"X","name":"X"}, expect=403)
print("== EMP-01 Employees ==")
req("list hr sort=hireDate", "GET", "/api/v1/employees?pageSize=5&sort=hireDate", HR, expect=200)
req("search 'Dev'", "GET", "/api/v1/employees?search=Dev", HR, expect=200)
req("bad sort -> 422", "GET", "/api/v1/employees?sort=salary", HR, expect=422)
_, d, et = req("self detail full fields", "GET", "/api/v1/employees/4", EMP, expect=200)
req("colleague out of scope -> 404", "GET", "/api/v1/employees/2", EMP, expect=404)
req("line-manager sees masked", "GET", "/api/v1/employees/4", LM, expect=200)
v = d["version"]
_, d, _ = req("patch phone self", "PATCH", "/api/v1/employees/4/profile", EMP, {"phone":"0912345678","emergencyContact":{"name":"Nguyễn Thị B","phone":"0987654321"}}, ifmatch=v, ct="application/merge-patch+json", expect=200)
req("patch work field -> 403", "PATCH", "/api/v1/employees/4/profile", EMP, {"departmentId":2}, ifmatch=d["version"], ct="application/merge-patch+json", expect=403)
req("patch stale version -> 409", "PATCH", "/api/v1/employees/4/profile", EMP, {"phone":"0911111111"}, ifmatch=v, ct="application/merge-patch+json", expect=409)
req("patch bad email -> 422", "PATCH", "/api/v1/employees/4/profile", EMP, {"personalEmail":"nope"}, ifmatch=d["version"], ct="application/merge-patch+json", expect=422)
req("hr patches other employee", "PATCH", "/api/v1/employees/5/profile", HR, {"phone":"0933333333"}, ifmatch=1, ct="application/merge-patch+json", expect=200)
req("line-manager patches other -> 403", "PATCH", "/api/v1/employees/4/profile", LM, {"phone":"0933333333"}, ifmatch=d["version"], ct="application/merge-patch+json", expect=403)
print("== EMP-02 Organization ==")
req("chart depth 3 -> 404 (out of scope)", "GET", "/api/v1/organization/chart?depth=3", EMP, expect=404)
req("list departments", "GET", "/api/v1/organization/departments", EMP, expect=200)
_, dep, det = req("create QA", "POST", "/api/v1/organization/departments", HR, {"code":"QA","name":"Đội QA","parentDepartmentId":2}, expect=201)
req("duplicate code (case-insens) -> 409", "POST", "/api/v1/organization/departments", HR, {"code":"qa","name":"Dup"}, expect=409)
req("bad code pattern -> 422", "POST", "/api/v1/organization/departments", HR, {"code":"QA TEAM!","name":"Dup"}, expect=422)
req("unknown parent -> 422", "POST", "/api/v1/organization/departments", HR, {"code":"ZZ","name":"Z","parentDepartmentId":999}, expect=422)
req("replace ENG under BE = cycle -> 409", "PUT", "/api/v1/organization/departments/2", HR, {"code":"ENG","name":"Khối Kỹ thuật","parentDepartmentId":4}, ifmatch=1, expect=409)
req("replace QA name", "PUT", f"/api/v1/organization/departments/{dep['id']}", HR, {"code":"QA","name":"Đội QA & Test","parentDepartmentId":2}, ifmatch=dep["version"], expect=200)
req("delete BE has employees -> 409", "DELETE", "/api/v1/organization/departments/4", HR, ifmatch=1, expect=409)
req("delete QA (empty) -> 204", "DELETE", f"/api/v1/organization/departments/{dep['id']}", HR, ifmatch=dep["version"]+1, expect=204)
_, pos, _ = req("create position", "POST", "/api/v1/organization/positions", HR, {"code":"QA-ENG","name":"QA Engineer","level":"IC-2"}, expect=201)
req("replace position", "PUT", f"/api/v1/organization/positions/{pos['id']}", HR, {"code":"QA-ENG","name":"QA Engineer II","level":"IC-3"}, ifmatch=pos["version"], expect=200)
print("== EMP-03 Onboarding ==")
req("overdue tasks (hr)", "GET", "/api/v1/onboarding/tasks?overdue=true", HR, expect=200)
req("my tasks (employee self)", "GET", "/api/v1/onboarding/tasks", EMP, expect=200)
req("line-manager sees dept tasks", "GET", "/api/v1/onboarding/tasks?employeeId=4", LM, expect=200)
_, t, _ = req("start task 3", "POST", "/api/v1/onboarding/tasks/3/start", HR, ifmatch=1, expect=200)
req("start again -> 409", "POST", "/api/v1/onboarding/tasks/3/start", HR, ifmatch=t["version"], expect=409)
_, t, _ = req("complete task 3", "POST", "/api/v1/onboarding/tasks/3/complete", HR, ifmatch=t["version"], expect=200)
req("reopen by line-manager -> 403", "POST", "/api/v1/onboarding/tasks/3/reopen", LM, {"reason":"x"}, ifmatch=t["version"], expect=403)
req("reopen no reason -> 422", "POST", "/api/v1/onboarding/tasks/3/reopen", HR, ifmatch=t["version"], expect=422)
req("reopen by hr with reason", "POST", "/api/v1/onboarding/tasks/3/reopen", HR, {"reason":"Thẻ in sai tên"}, ifmatch=t["version"], expect=200)
req("update unknown assignee -> 422", "PUT", "/api/v1/onboarding/tasks/5", HR, {"taskName":"Bố trí buddy","assignedToUserId":999}, ifmatch=1, expect=422)
req("update assignment ok", "PUT", "/api/v1/onboarding/tasks/5", HR, {"taskName":"Bố trí buddy/mentor","assignedToUserId":3,"dueAt":"2026-09-20T03:00:00Z"}, ifmatch=1, expect=200)
req("bogus action -> 404", "POST", "/api/v1/onboarding/tasks/5/delete", HR, ifmatch=1, expect=404)
print("== EMP-04 Employee events ==")
_, ev, _ = req("create promotion", "POST", "/api/v1/employees/4/events", HR, {"eventType":"promotion","effectiveDate":"2026-10-01","beforeData":{"positionId":3},"afterData":{"positionId":2},"reason":"Thăng chức theo đánh giá Q3"}, expect=201)
req("same field same day -> 409", "POST", "/api/v1/employees/4/events", HR, {"eventType":"transfer","effectiveDate":"2026-10-01","beforeData":{},"afterData":{"positionId":4},"reason":"conflict"}, expect=409)
req("unknown afterData key -> 422", "POST", "/api/v1/employees/4/events", HR, {"eventType":"promotion","effectiveDate":"2026-11-01","beforeData":{},"afterData":{"foo":1},"reason":"x"}, expect=422)
req("termination w/o status -> 422", "POST", "/api/v1/employees/4/events", HR, {"eventType":"termination","effectiveDate":"2026-11-01","beforeData":{},"afterData":{"positionId":1},"reason":"x"}, expect=422)
req("employee creates event -> 403", "POST", "/api/v1/employees/4/events", EMP, {"eventType":"promotion","effectiveDate":"2026-12-01","beforeData":{},"afterData":{"positionId":2},"reason":"x"}, expect=403)
req("approve while draft -> 409", "POST", f"/api/v1/employee-events/{ev['id']}/approve", HR, ifmatch=ev["version"], expect=409)
_, ev, _ = req("submit", "POST", f"/api/v1/employee-events/{ev['id']}/submit", HR, ifmatch=ev["version"], expect=200)
req("approve by line-manager -> 403", "POST", f"/api/v1/employee-events/{ev['id']}/approve", LM, ifmatch=ev["version"], expect=403)
_, ev, _ = req("approve by hr-manager", "POST", f"/api/v1/employee-events/{ev['id']}/approve", HR, ifmatch=ev["version"], expect=200)
req("cancel without reason -> 422", "POST", f"/api/v1/employee-events/{ev['id']}/cancel", HR, ifmatch=ev["version"], expect=422)
req("employee still probation/position 3", "GET", "/api/v1/employees/4", HR, expect=200)
req("list events (self)", "GET", "/api/v1/employees/4/events", EMP, expect=200)
req("list events out of scope -> 404", "GET", "/api/v1/employees/2/events", EMP, expect=404)
print("== EMP-05 Documents ==")
pdf = b"%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n"
_, doc, _ = req("upload degree.pdf (hr)", "POST", "/api/v1/employees/4/documents", HR, multipart={"documentType":"degree","file":("bang.pdf",pdf,"application/pdf")}, expect=201)
req("upload again -> version 2", "POST", "/api/v1/employees/4/documents", HR, multipart={"documentType":"degree","file":("bang.pdf",pdf,"application/pdf")}, expect=201)
req("upload .exe -> 415", "POST", "/api/v1/employees/4/documents", HR, multipart={"documentType":"other","file":("x.exe",pdf,"application/x-msdownload")}, expect=415)
req("unknown documentType -> 422", "POST", "/api/v1/employees/4/documents", HR, multipart={"documentType":"passport","file":("x.pdf",pdf,"application/pdf")}, expect=422)
req("employee uploads disciplinary -> 403", "POST", "/api/v1/employees/4/documents", EMP, multipart={"documentType":"disciplinary_record","file":("x.pdf",pdf,"application/pdf")}, expect=403)
req("employee uploads own photo", "POST", "/api/v1/employees/4/documents", EMP, multipart={"documentType":"photo","file":("me.png",b"\x89PNG\r\n\x1a\n0000","image/png")}, expect=201)
_, disc, _ = req("upload disciplinary (hr)", "POST", "/api/v1/employees/4/documents", HR, multipart={"documentType":"disciplinary_record","file":("kl.pdf",pdf,"application/pdf")}, expect=201)
req("list as employee (no disciplinary)", "GET", "/api/v1/employees/4/documents", EMP, expect=200)
req("list as hr (all)", "GET", "/api/v1/employees/4/documents", HR, expect=200)
_, sd, _ = req("download-url degree (employee)", "POST", f"/api/v1/employee-documents/{doc['id']}/download-url", EMP, expect=200)
url = sd["url"]
try:
    with urllib.request.urlopen(url) as r: print(f"   GET    {'follow signed url':<50} -> {r.status} bytes={len(r.read())}")
except urllib.error.HTTPError as e: print(f"!! GET    follow signed url -> {e.code}")
try: urllib.request.urlopen(url + "x"); print("!! tampered signature accepted")
except urllib.error.HTTPError as e: print(f"   GET    {'tampered signature -> 403':<50} -> {e.code}")
req("download disciplinary (employee) -> 403", "POST", f"/api/v1/employee-documents/{disc['id']}/download-url", EMP, expect=403)
req("download unknown -> 404", "POST", "/api/v1/employee-documents/9999/download-url", HR, expect=404)
