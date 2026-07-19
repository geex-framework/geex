import { CanMatchFn } from "@angular/router";
import { OAuthService } from "angular-oauth2-oidc";
import { inject } from "@angular/core";
import { geex } from "@geexcode/geex-angular";
import { GEEX_DEFAULT_SUPER_ADMIN_USER_ID } from "@geexcode/geex-extensions-identity";

export const mockingEnabledCanMatch: CanMatchFn = () =>
  geex.mocking.getCapabilities().then(capabilities => capabilities.enabled);

export const mockingSuperAdminCanMatch: CanMatchFn = () => {
  const oauth = inject(OAuthService, { optional: true });
  return geex.mocking.getCapabilities().then(result => {
    if (!result.enabled || !result.management) {
      return false;
    }
    const claims = oauth?.getIdentityClaims() as { sub?: string } | null;
    return claims?.sub === GEEX_DEFAULT_SUPER_ADMIN_USER_ID;
  });
};
