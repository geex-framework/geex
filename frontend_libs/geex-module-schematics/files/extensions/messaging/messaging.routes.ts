import { Routes } from "@angular/router";

export const messagingRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/messaging-pages.routes").then(m => m.messagingPagesRoutes),
  },
];
