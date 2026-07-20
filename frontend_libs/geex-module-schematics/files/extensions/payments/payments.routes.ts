import { Routes } from "@angular/router";

export const paymentsRoutes: Routes = [
  {
    path: "",
    loadChildren: () => import("./pages/payments-pages.routes").then(m => m.paymentsPagesRoutes),
  },
];
