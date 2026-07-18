import { inject, Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from "@angular/router";
import { OAuthService } from "angular-oauth2-oidc";
import { geex, GEEX_SUPER_ADMIN_USER_ID, type IdentityClaims } from "@geexcode/geex-angular";

type AuthorizationTenantModule = {
  current(): { code: string } | null;
  loadTenantData(code: string): Promise<unknown>;
};

@Injectable({ providedIn: "root" })
export class AuthorizeGuard {
  private readonly superAdminUserId = inject(GEEX_SUPER_ADMIN_USER_ID);

  constructor(
    private oAuthService: OAuthService,
    private router: Router,
  ) {}

  /**
   * Route guard: token + tenant claim vs current tenant.
   * Token refresh stays with host interceptor / OAuthService.
   */
  async canActivate(_route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean | UrlTree> {
    if (!this.oAuthService.hasValidAccessToken()) {
      return this.router.parseUrl(`/auth/login?redirect_uri=${encodeURIComponent(state.url)}`);
    }

    const claims = this.oAuthService.getIdentityClaims() as IdentityClaims | null;

    if (claims?.sub === this.superAdminUserId) {
      return true;
    }

    const tenant = geex["tenant"] as AuthorizationTenantModule;
    const currentTenantCode = tenant.current()?.code;
    const tokenTenantCode = claims?.__tenant;

    if (currentTenantCode && tokenTenantCode === currentTenantCode) {
      return true;
    }

    return this.router.parseUrl("/exception/403");
  }

  async checkTenant(pathTenant: string) {
    return (geex["tenant"] as AuthorizationTenantModule).loadTenantData(pathTenant);
  }
}
