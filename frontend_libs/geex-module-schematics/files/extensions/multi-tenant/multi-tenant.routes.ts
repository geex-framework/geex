import { Routes } from "@angular/router";

export const multiTenantRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/tenant-list.routes").then(m => m.tenantListRoutes),
  },
];
