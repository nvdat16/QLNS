/** Permission strings of the ADM module, mirroring IdentityPermissions on the server. Used only to decide
 *  which affordances to render; the authoritative check always happens server-side. */
export const IdentityPermissions = {
  userRead: "admin.user.read",
  userManage: "admin.user.manage",
  roleRead: "admin.role.read",
} as const;
