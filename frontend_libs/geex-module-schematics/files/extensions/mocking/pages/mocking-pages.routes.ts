import type { Routes } from "@angular/router";
import {
  mockingEnabledCanMatch,
  mockingSuperAdminCanMatch,
} from "@geexcode/geex-extensions-mocking";
import { MockingHomePage } from "./mocking-home.page";
import { MockingPaymentsPage } from "./mocking-payments.page";
import { MockingSmsPage } from "./mocking-sms.page";
import { MockingWechatProfilesPage } from "./mocking-wechat-profiles.page";

export const mockingPagesRoutes: Routes = [
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
