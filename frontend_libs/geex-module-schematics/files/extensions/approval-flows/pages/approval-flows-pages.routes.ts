import { Routes } from "@angular/router";

import { ApprovalFlowTemplateEditPage } from "./edit/edit.page";
import { InboxDetailPage } from "./inbox/detail.page";
import { InboxEditPage } from "./inbox/edit.page";
import { InboxListPage } from "./inbox/list.page";
import { ApprovalFlowTemplateListPage } from "./list/list.page";
import { ApprovalTagsPage } from "./tags/tags.page";

export const approvalFlowsPagesRoutes: Routes = [
  { path: "tags", component: ApprovalTagsPage },
  { path: "", component: ApprovalFlowTemplateListPage },
  { path: "edit", component: ApprovalFlowTemplateEditPage },
  { path: "edit/:id", component: ApprovalFlowTemplateEditPage },
  { path: "inbox", component: InboxListPage },
  { path: "inbox/:id/edit", component: InboxEditPage },
  { path: "inbox/:id", component: InboxDetailPage },
];
