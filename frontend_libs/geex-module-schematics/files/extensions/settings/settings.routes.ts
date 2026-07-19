import { Routes } from "@angular/router";

export const settingsRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/settings-pages.routes").then(m => m.settingsPagesRoutes),
  },
];
