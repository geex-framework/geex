import type { Routes } from "@angular/router";
import {
  mockingEnabledCanMatch,
  mockingSuperAdminCanMatch,
} from "@geexcode/geex-extensions-mocking";
import { MockingHomePage } from "./pages/mocking-home.page";
import { MockingPaymentsPage } from "./pages/mocking-payments.page";
import { MockingSmsPage } from "./pages/mocking-sms.page";
import { MockingWechatProfilesPage } from "./pages/mocking-wechat-profiles.page";

/** Admin management routes mounted under LayoutBasic. */
export const mockingAdminRoutes: Routes = [
  {
    path: "",
    canMatch: [mockingEnabledCanMatch, mockingSuperAdminCanMatch],
    children: [
      { path: "", component: MockingHomePage },
      { path: "wechat", component: MockingWechatProfilesPage },
      { path: "sms", component: MockingSmsPage },
      { path: "payments", component: MockingPaymentsPage },
    ],
  },
];
