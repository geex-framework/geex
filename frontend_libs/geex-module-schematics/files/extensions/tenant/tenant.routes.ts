import { Routes } from "@angular/router";

export const tenantRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/tenant-list.routes").then(m => m.tenantListRoutes),
  },
];
