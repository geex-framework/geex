import { Inject, Injectable, Injector } from "@angular/core";
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot, CanActivateChild, UrlTree } from "@angular/router";
import { ACLService } from "@delon/acl";
import { DA_SERVICE_TOKEN, ITokenService, JWTGuard, JWTTokenModel } from "@delon/auth";
import { SettingsService } from "@delon/theme";
import { deepCopy } from "@delon/util";
import { Store } from "@ngxs/store";
import { OAuthService } from "angular-oauth2-oidc";
import { Apollo } from "apollo-angular";
import { take } from "rxjs/operators";

import { environment } from "../../../environments/environment";
import { StartupService } from "../../core";
import { CheckTenantGql, CheckTenantMutation, CheckTenantMutationVariables, FederateAuthenticateGql } from "../graphql/.generated/type";
import { AppActions } from "../states/AppActions";
import { CacheDataState } from "../states/cache-data.state";
import { TenantState } from "../states/tenant.state";

@Injectable({ providedIn: "root" })
export class AuthorizeGuard implements CanActivate {
  constructor(
    private oAuthService: OAuthService,
    private router: Router,
    private injector: Injector,
    private store: Store,
    private apollo: Apollo,
  ) {}
  async canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean | UrlTree> {
    if (!this.oAuthService.hasValidAccessToken()) {
      if (this.oAuthService.getAccessTokenExpiration() < new Date().getTime()) {
        try {
          var refresh = await this.oAuthService.silentRefresh();
        } catch (error) {
          console.error(error);
          return this.router.navigate(["/passport/login"]);
        }
      } else {
        await this.router.navigate(["exception", "403"]);
      }
    }
    let tenantCode = this.store.selectSnapshot(TenantState)?.code;
    const targetTenant = this.oAuthService.getIdentityClaims()?.__tenant;
    if (targetTenant == tenantCode) {
      return true;
    }
    await this.router.navigate(["exception", "403"]);
    return false;
  }
  async checkTenant(pathTenant: string) {
    const userRes = await this.apollo
      .use("silent")
      .mutate<CheckTenantMutation, CheckTenantMutationVariables>({
        mutation: CheckTenantGql,
        variables: {
          code: pathTenant,
        },
      })
      .toPromise();

    let tenant = userRes?.data?.checkTenant;
    return tenant;
  }
}
