import { Routes } from "@angular/router";

import { ApprovalFlowTemplateEditPage } from "./edit/edit.page";
import { ApprovalFlowTemplateListPage } from "./list/list.page";

export const approvalFlowsPagesRoutes: Routes = [
  { path: "", component: ApprovalFlowTemplateListPage },
  { path: "edit", component: ApprovalFlowTemplateEditPage },
  { path: "edit/:id", component: ApprovalFlowTemplateEditPage },
];
