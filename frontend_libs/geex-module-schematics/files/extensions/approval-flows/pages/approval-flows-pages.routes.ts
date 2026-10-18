import { Routes } from "@angular/router";

import { ApprovalCategoriesPage } from "./categories/categories.page";
import { ApprovalFlowTemplateEditPage } from "./edit/edit.page";
import { InboxDetailPage } from "./inbox/detail.page";
import { InboxEditPage } from "./inbox/edit.page";
import { InboxListPage } from "./inbox/list.page";
import { ApprovalFlowTemplateListPage } from "./list/list.page";

export const approvalFlowsPagesRoutes: Routes = [
  { path: "categories", component: ApprovalCategoriesPage },
  { path: "", component: ApprovalFlowTemplateListPage },
  { path: "edit", component: ApprovalFlowTemplateEditPage },
  { path: "edit/:id", component: ApprovalFlowTemplateEditPage },
  { path: "inbox", component: InboxListPage },
  { path: "inbox/:id/edit", component: InboxEditPage },
  { path: "inbox/:id", component: InboxDetailPage },
];
