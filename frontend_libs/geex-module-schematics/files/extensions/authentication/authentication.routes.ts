import { Routes } from "@angular/router";

export const authenticationRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/auth-pages.routes").then(m => m.authPagesRoutes),
  },
];
