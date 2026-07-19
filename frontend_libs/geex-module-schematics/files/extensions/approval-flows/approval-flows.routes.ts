import { Routes } from "@angular/router";

export const approvalFlowsRoutes: Routes = [
  { path: "", loadChildren: () => import("./pages/approval-flows-pages.routes").then(m => m.approvalFlowsPagesRoutes) },
];
