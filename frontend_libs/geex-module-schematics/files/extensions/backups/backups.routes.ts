import { Routes } from "@angular/router";

export const backupsRoutes: Routes = [
  { path: "", loadChildren: () => import("./pages/backups-pages.routes").then(m => m.backupsPagesRoutes) },
];
