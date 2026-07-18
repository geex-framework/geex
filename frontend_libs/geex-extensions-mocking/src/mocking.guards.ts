import { inject } from "@angular/core";
import { CanMatchFn } from "@angular/router";
import { OAuthService } from "angular-oauth2-oidc";
import { GeexMockingCapabilitiesService } from "./mocking-capabilities.service";
import { GEEX_SUPER_ADMIN_ID } from "./types";

export const mockingEnabledCanMatch: CanMatchFn = () =>
  inject(GeexMockingCapabilitiesService).getCapabilities().then(capabilities => capabilities.enabled);

export const mockingSuperAdminCanMatch: CanMatchFn = () => {
  const capabilities = inject(GeexMockingCapabilitiesService);
  const oauth = inject(OAuthService, { optional: true });
  return capabilities.getCapabilities().then(result => {
    if (!result.enabled || !result.management) {
      return false;
    }
    const claims = oauth?.getIdentityClaims() as { sub?: string } | null;
    return claims?.sub === GEEX_SUPER_ADMIN_ID;
  });
};
