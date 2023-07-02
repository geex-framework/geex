import { Inject, Injectable } from "@angular/core";
import { Resolve, Routes, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { ReuseTabCached, ReuseTabService } from "@delon/abc/reuse-tab";
import { ACLService } from "@delon/acl";
import { DA_SERVICE_TOKEN, ITokenService, JWTTokenModel } from "@delon/auth";
import { ALAIN_I18N_TOKEN, Menu, MenuService, SettingsService } from "@delon/theme";
import { deepCopy } from "@delon/util";
import { Store } from "@ngxs/store";
import { Apollo } from "apollo-angular";
import _ from "lodash";

import { environment } from "../../../environments/environment";
import { I18NService } from "../../core";
import { InitSettingsQuery, InitSettingsGql, SettingDefinition } from "../graphql/.generated/type";
import { CacheDataState } from "../states/cache-data.state";
import { UserDataState$, UserDataState } from "../states/user-data.state";

@Injectable({ providedIn: "root" })
export class InitLayoutResolver implements Resolve<void> {
  private initialized = false;
  constructor(
    private menuService: MenuService,
    private settingService: SettingsService,
    @Inject(ALAIN_I18N_TOKEN) private i18n: I18NService,
    @Inject(DA_SERVICE_TOKEN) private tokenService: ITokenService,
    private apollo: Apollo,
    private reuseTabService: ReuseTabService,
    private router: Router,
    private store: Store,
    private aclSrv: ACLService,
    private cacheDataState: CacheDataState,
    private userDataState: UserDataState,
  ) {}

  async resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    if (this.initialized) {
      return;
    }
    // await this.tenantState.init();
    //1 清空路由复用信息
    this.reuseTabService.clear();
    //2 设置settings
    let settingQuery = await this.apollo
      .query<InitSettingsQuery>({
        query: InitSettingsGql,
        variables: {},
      })
      .toPromise();
    const settings = settingQuery.data.initSettings;
    if (settings.any()) {
      this.settingService.setApp({
        name: settings.first(x => x.name == SettingDefinition.AppAppMenu).value,
      });
      let settingMenus = settings.first(x => x.name == SettingDefinition.AppAppMenu).value;
      let menus = _.merge(environment.menus, settingMenus) as Menu[];
      menus
        .flatMapDeep<Menu>(x => x.children)
        .forEach(x => {
          if (!window.isMobile && x.visibility == "vertical") {
            x.hide = true;
          }
          if (window.isMobile && x.visibility == "horizontal") {
            x.hide = true;
          }
        });
      this.menuService.add(menus);
      this.menuService.resume();
      this.i18n.merge(settings.first(x => x.name == SettingDefinition.LocalizationData).value);
      this.i18n.use(settings.first(x => x.name == SettingDefinition.LocalizationLanguage).value);
      window.settings = deepCopy(settings);
    }
    this.initialized = true;
  }
}
