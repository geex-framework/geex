import { Routes } from "@angular/router";
import { MessagingAdminListPage } from "./admin-messages-list.page";
import { MessagingUnreadListPage } from "./unread-list.page";

export const messagingPagesRoutes: Routes = [
  { path: "", redirectTo: "unread", pathMatch: "full" },
  { path: "unread", component: MessagingUnreadListPage },
  { path: "admin", component: MessagingAdminListPage },
];
