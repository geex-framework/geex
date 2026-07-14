import { inject } from "@angular/core";
import { CanMatchFn, Route } from "@angular/router";
import { OAuthService } from "angular-oauth2-oidc";
import { GeexMockingCapabilitiesService } from "./mocking-capabilities.service";
import { GEEX_SUPER_ADMIN_ID } from "./types";
import { MockingHomePage } from "./pages/mocking-home.page";
import { MockingWechatProfilesPage } from "./pages/mocking-wechat-profiles.page";
import { MockingSmsPage } from "./pages/mocking-sms.page";
import { MockingPaymentsPage } from "./pages/mocking-payments.page";
import { MockWechatAuthorizePage } from "./pages/mock-wechat-authorize.page";
import { MockPaymentCheckoutPage } from "./pages/mock-payment-checkout.page";

export const mockingEnabledCanMatch: CanMatchFn = () => {
  const capabilities = inject(GeexMockingCapabilitiesService);
  return capabilities.getCapabilities().then(caps => caps.enabled);
};

export const mockingSuperAdminCanMatch: CanMatchFn = () => {
  const capabilities = inject(GeexMockingCapabilitiesService);
  const oauth = inject(OAuthService, { optional: true });
  return capabilities.getCapabilities().then(caps => {
    if (!caps.enabled || !caps.management) {
      return false;
    }
    const claims = oauth?.getIdentityClaims() as { sub?: string } | null;
    return claims?.sub === GEEX_SUPER_ADMIN_ID;
  });
};

export const mockingRoutes: Route[] = [
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
      {
        path: "",
        canMatch: [mockingSuperAdminCanMatch],
        component: MockingHomePage,
      },
      {
        path: "wechat",
        canMatch: [mockingSuperAdminCanMatch],
        component: MockingWechatProfilesPage,
      },
      {
        path: "sms",
        canMatch: [mockingSuperAdminCanMatch],
        component: MockingSmsPage,
      },
      {
        path: "payments",
        canMatch: [mockingSuperAdminCanMatch],
        component: MockingPaymentsPage,
      },
    ],
  },
];

export const mockingNavigation = {
  text: "Mocking",
  link: "/mocking",
  icon: "experiment",
} as const;
