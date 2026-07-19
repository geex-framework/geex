import { Routes } from "@angular/router";

export const identityRoutes: Routes = [
  { path: "", redirectTo: "me", pathMatch: "full" },
  {
    path: "me",
    loadChildren: () => import("./pages/me/me.routes").then(m => m.meRoutes),
  },
  {
    path: "user",
    loadChildren: () => import("./pages/user/user.routes").then(m => m.userRoutes),
  },
  {
    path: "role",
    loadChildren: () => import("./pages/role/role.routes").then(m => m.roleRoutes),
  },
  {
    path: "org",
    loadChildren: () => import("./pages/org/org.routes").then(m => m.orgRoutes),
  },
];
