import { Routes } from "@angular/router";

export const mockingRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/mocking-pages.routes").then(m => m.mockingPagesRoutes),
  },
];
