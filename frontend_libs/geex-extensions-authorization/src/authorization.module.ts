import { Injector } from "@angular/core";
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from "@angular/router";
import { OAuthService } from "angular-oauth2-oidc";
import { geex, GEEX_SUPER_ADMIN_USER_ID, type IdentityClaims } from "@geexcode/geex-angular";
import json5 from "json5";
import type { AuthorizationAclData, AuthorizationModule } from "./authorization.types";

type AuthorizationMultiTenantModule = {
  current(): { code: string } | null;
  loadTenantData(code: string): Promise<unknown>;
};

type AuthorizationAuthenticationModule = {
  init(force?: boolean): Promise<unknown>;
  user(): unknown;
};

export function createAuthorizationModule(injector: Injector): AuthorizationModule {
  const oauth = () => injector.get(OAuthService);
  const router = () => injector.get(Router);
  const superAdminUserId = () => injector.get(GEEX_SUPER_ADMIN_USER_ID);

  const module: AuthorizationModule = {
    canActivate: async (_route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean | UrlTree> => {
      if (!oauth().hasValidAccessToken()) {
        return router().parseUrl(`/authentication/login?redirect_uri=${encodeURIComponent(state.url)}`);
      }

      const claims = oauth().getIdentityClaims() as IdentityClaims | null;
      if (claims?.sub === superAdminUserId()) {
        return true;
      }

      const multiTenant = geex["multiTenant"] as AuthorizationMultiTenantModule;
      const currentTenantCode = multiTenant.current()?.code;
      const tokenTenantCode = claims?.__tenant;

      if (currentTenantCode && tokenTenantCode === currentTenantCode) {
        return true;
      }

      return router().parseUrl("/exception/403");
    },
    checkTenant: async (pathTenant: string) => {
      return (geex["multiTenant"] as AuthorizationMultiTenantModule).loadTenantData(pathTenant);
    },
    loadAcl: () => {
      try {
        return json5.parse(localStorage.getItem("acl") ?? "{}") as AuthorizationAclData;
      } catch (error) {
        console.error("failed to load acl from localStorage.", error);
        return { roles: [], abilities: [], full: false };
      }
    },
    persistAcl: (data: AuthorizationAclData) => {
      localStorage.setItem("acl", json5.stringify(data));
    },
    syncAclFromAuth: async () => {
      const authentication = geex["authentication"] as AuthorizationAuthenticationModule;
      await authentication.init();
      if (authentication.user() == undefined) {
        return null;
      }
      return module.loadAcl();
    },
    init: async () => undefined,
  };

  return module;
}
