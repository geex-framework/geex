import type { Routes } from "@angular/router";
import {
  mockingEnabledCanMatch,
  mockingSuperAdminCanMatch,
} from "@geexcode/geex-extensions-mocking";
import { MockPaymentCheckoutPage } from "./pages/mock-payment-checkout.page";
import { MockWechatAuthorizePage } from "./pages/mock-wechat-authorize.page";
import { MockingHomePage } from "./pages/mocking-home.page";
import { MockingPaymentsPage } from "./pages/mocking-payments.page";
import { MockingSmsPage } from "./pages/mocking-sms.page";
import { MockingWechatProfilesPage } from "./pages/mocking-wechat-profiles.page";

export const mockingRoutes: Routes = [
  {
    path: "wechat/authorize/:token",
    canMatch: [mockingEnabledCanMatch],
    component: MockWechatAuthorizePage,
  },
  {
    path: "payments/:token",
    canMatch: [mockingEnabledCanMatch],
    component: MockPaymentCheckoutPage,
  },
  {
    path: "",
    canMatch: [mockingEnabledCanMatch],
    children: [
      { path: "", canMatch: [mockingSuperAdminCanMatch], component: MockingHomePage },
      { path: "wechat", canMatch: [mockingSuperAdminCanMatch], component: MockingWechatProfilesPage },
      { path: "sms", canMatch: [mockingSuperAdminCanMatch], component: MockingSmsPage },
      { path: "payments", canMatch: [mockingSuperAdminCanMatch], component: MockingPaymentsPage },
    ],
  },
];
