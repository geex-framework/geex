import { ChangeDetectionStrategy, Component, Inject, Injector } from "@angular/core";
import { Router } from "@angular/router";
import { ACLService } from "@delon/acl";
import { DA_SERVICE_TOKEN, ITokenService } from "@delon/auth";
import { SettingsService, User } from "@delon/theme";
import { Select, Store } from "@ngxs/store";
import { OAuthService } from "angular-oauth2-oidc";
import { Apollo, gql } from "apollo-angular";
import { Observable } from "rxjs";
import { take } from "rxjs/operators";

import { environment } from "../../../../environments/environment";
import { LoginProviderEnum } from "../../../shared/graphql/.generated/type";
import { AppStates } from "../../../shared/states/AppStates";
import { TenantState } from "../../../shared/states/tenant.state";
import { UserDataState, UserDataStateModel } from "../../../shared/states/user-data.state";
@Component({
  selector: "header-user",
  template: `
    <div class="alain-default__nav-item d-flex align-items-center px-sm" nz-dropdown nzPlacement="bottomRight" [nzDropdownMenu]="userMenu">
      <nz-avatar
        nzIcon="user"
        style="color:#f56a00; background-color:#fde3cf;"
        [nzSrc]="user.avatar"
        nzSize="small"
        class="mr-sm"
      ></nz-avatar>
      {{ tenantCode }}:{{ user.username }}
    </div>
    <nz-dropdown-menu #userMenu="nzDropdownMenu">
      <div nz-menu class="width-sm">
        <div nz-menu-item routerLink="/personal-center">
          <i nz-icon nzType="user" class="mr-sm"></i>
          个人中心
        </div>
        <!-- <div nz-menu-item routerLink="/account/settings">
          <i nz-icon nzType="setting" class="mr-sm"></i>
          个人设置
        </div> -->
        <li nz-menu-divider></li>
        <div nz-menu-item (click)="logout()">
          <i nz-icon nzType="logout" class="mr-sm"></i>
          退出登录
        </div>
      </div>
    </nz-dropdown-menu>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderUserComponent {
  get user(): User {
    return this.settings.user;
  }
  get tenantCode(): string {
    return this.store.selectSnapshot(AppStates.TenantState).code;
  }
  @Select(UserDataState)
  user$: Observable<UserDataStateModel>;
  constructor(
    private settings: SettingsService,
    @Inject(DA_SERVICE_TOKEN) private tokenService: ITokenService,
    private aclService: ACLService,
    private apiClient: Apollo,
    private router: Router,
    private oAuthSrv: OAuthService,
    private store: Store,
  ) {}
  async logout(): Promise<void> {
    await this.apiClient
      .mutate({
        mutation: gql`
          mutation cancelAuthenticate {
            cancelAuthentication
          }
        `,
      })
      .toPromise();
    this.tokenService.clear();
    this.settings.setUser({});
    this.aclService.set({});
    let user = await this.user$.pipe(take(1)).toPromise();
    switch (user.loginProvider) {
     /*  case LoginProviderEnum.XOrgX:
        clearHistory();
        break; */
      default:
        await this.router.navigateByUrl(this.tokenService.login_url!).then(() => {
          clearHistory();
        });
        break;
    }
  }
}
