import { Routes } from "@angular/router";

export const auditLogsRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/audit-logs-pages.routes").then(m => m.auditLogsPagesRoutes),
  },
];
