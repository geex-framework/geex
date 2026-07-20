import { Routes } from "@angular/router";

export const backgroundJobRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/background-job-pages.routes").then(m => m.backgroundJobPagesRoutes),
  },
];
