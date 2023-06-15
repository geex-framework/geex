/* eslint-disable no-irregular-whitespace */
import { Injectable, Injector, Inject } from "@angular/core";
import { Router } from "@angular/router";
import { ReuseTabService } from "@delon/abc/reuse-tab";
import { ACLService } from "@delon/acl";
import { DA_SERVICE_TOKEN, ITokenService, JWTTokenModel } from "@delon/auth";
import { ALAIN_I18N_TOKEN, Menu, MenuService, SettingsService, TitleService } from "@delon/theme";
import { deepCopy } from "@delon/util";
import { Store } from "@ngxs/store";
import { OAuthService } from "angular-oauth2-oidc";
import { Apollo, gql } from "apollo-angular";
import _ from "lodash";
import { NzIconService } from "ng-zorro-antd/icon";
import { NzModalService } from "ng-zorro-antd/modal";
import { take } from "rxjs/operators";
import {
  AuthenticateResultFragmentFragment,
  CheckTenantGql,
  CheckTenantMutation,
  CheckTenantMutationVariables,
  FederateAuthenticateGql,
  InitSettingsGql,
  InitSettingsQuery,
  LoginProviderEnum,
  SettingDefinition,
  UserToken,
} from "src/app/shared/graphql/.generated/type";

import { environment } from "../../../environments/environment";
import { ICONS } from "../../../style-icons";
import { ICONS_AUTO } from "../../../style-icons-auto";
import { AppActions } from "../../shared/states/AppActions";
import { CacheDataState } from "../../shared/states/cache-data.state";
import { TenantState } from "../../shared/states/tenant.state";
import { UserDataState, UserDataState$ } from "../../shared/states/user-data.state";
import { IdentityClaims } from "../../shared/utils/angular-oauth2-oidc.extensions";
import { I18NService } from "../i18n/i18n.service";

/**
 * Used for application startup
 * Generally used to get the basic data of the application, like: Menu Data, User Data, etc.
 */
@Injectable()
export class StartupService {
  constructor(private injector: Injector) {
    let iconSrv = injector.get(NzIconService);
    iconSrv.addIcon(...ICONS_AUTO, ...ICONS);
  }
  private async viaHttp(): Promise<void> {}

  load(): Promise<void> {
    // only works with promises
    // https://github.com/angular/angular/issues/15088
    return (async () => {
      let oAuthService = this.injector.get(OAuthService);
      let modalService = this.injector.get(NzModalService);
      let tokenService = this.injector.get(DA_SERVICE_TOKEN);
      let settings = this.injector.get(SettingsService);
      let aclService = this.injector.get(ACLService);
      let user$ = this.injector.get(UserDataState$);
      let router = this.injector.get(Router);

      oAuthService.configure(environment.auth.x_org_x);
      try {
        await oAuthService.loadDiscoveryDocumentAndTryLogin({
          customRedirectUri: location.href.split(/[&\?#]code=[^&\$]*/)[0].trimEnd("/"),
        });
      } catch (error) {}
      await this.trySwitchTenant();
      let url = new URL(location.href);
      let autoLogin = url.searchParams.get("_autoLogin");
      if (autoLogin) {
        url.searchParams.delete("_autoLogin");
        oAuthService.redirectUri = url.href;
        oAuthService.initCodeFlow();
        return;
      }

      if (oAuthService.hasValidAccessToken()) {
        await oAuthService.loadUserProfile();
        var claims = oAuthService.getIdentityClaims();
        const tokenModel = tokenService.get<JWTTokenModel>(JWTTokenModel);
        if (tokenModel == undefined || tokenModel.token == undefined || tokenModel.isExpired()) {
          await this.initUserInfos(claims);
        }
        let cacheDataState = this.injector.get(CacheDataState);
        await cacheDataState.init(true);
        oAuthService["initSessionCheck"]();
        oAuthService.events.subscribe(e => {
          if (e.type == "session_terminated") {
            console.error(e);
            modalService.info({
              nzTitle: "检测到账号切换, 请从Geex重新发起会话",
              nzOkText: "确认",
              nzOnOk: async () => {
                tokenService.clear();
                settings.setUser({});
                aclService.set({});
                let user = await user$.pipe(take(1)).toPromise();
                switch (user.loginProvider) {
                  /*  case LoginProviderEnum.XOrgX:
                    clearHistory();
                    break; */
                  default:
                    await router.navigateByUrl(tokenService.login_url!).then(() => {
                      clearHistory();
                    });
                    break;
                }
              },
              nzClosable: false,
            });
          }
        });
      }
    })();
  }
  async initUserInfos(identityClaims: IdentityClaims) {
    let settingsService = this.injector.get(SettingsService);
    let apollo = this.injector.get(Apollo);
    let oAuthService = this.injector.get(OAuthService);
    let tokenService = this.injector.get(DA_SERVICE_TOKEN);
    let store = this.injector.get(Store);
    let aclService = this.injector.get(ACLService);
    let res = await apollo
      .mutate({
        mutation: FederateAuthenticateGql,
        variables: {
          code: oAuthService.getAccessToken(),
          loginProvider: identityClaims.login_provider,
        },
      })
      .toPromise();
    const authResult = res.data.federateAuthenticate;
    if (!authResult.token) {
      alert(res.errors?.firstOrDefault()?.message);
      return;
    } else {
      // 设置用户Token信息,过期时间
      // 重新获取 StartupService 内容，我们始终认为应用信息一般都会受当前用户授权范围而影响
      // only works with promises
      // https://github.com/angular/angular/issues/15088
      let tasks: Array<Promise<any>> = [];
      tokenService.set({ token: authResult.token });
      const user = deepCopy(authResult.user);
      tasks.add(
        store
          .dispatch(
            new AppActions.UserDataLoaded({
              ...user,
            }),
          )
          .toPromise(),
      );
      await Promise.all(tasks);
      settingsService.setUser({
        avatar: user.avatarFile?.url,
        id: user.id,
        phoneNumber: user.phoneNumber,
        email: user.email,
        username: user.username,
        roleName: user.roleNames,
      });
      // 设置权限
      if (user.id == "000000000000000000000001") {
        aclService.setFull(true);
      } else {
        aclService.setRole(authResult.user.permissions);
      }
    }
  }
  // 校验租户
  private async trySwitchTenant() {
    let store = this.injector.get(Store);
    let router = this.injector.get(Router);

    let url = new URL(location.href);
    let targetTenantCode = url.searchParams.get("_tenant");
    if (targetTenantCode == undefined) {
      return;
    }

    let currentTenantCode = store.selectSnapshot(TenantState)?.code;
    url.searchParams.delete("_tenant");
    if (targetTenantCode == currentTenantCode) {
      await router.navigateByUrl(url.pathname + url.search + url.hash);
    }
    if (targetTenantCode) {
      await store.dispatch(new AppActions.TenantChanged(targetTenantCode)).toPromise();
      let tokenService = this.injector.get(DA_SERVICE_TOKEN);
      tokenService.clear();
      await router.navigateByUrl(url.pathname + url.search + url.hash);
    }
  }
}
