/* eslint-disable import/order */
import { ModuleWithProviders, NgModule, Optional, SkipSelf } from "@angular/core";
import { throwIfAlreadyLoaded } from "@core";
import { ReuseTabService, ReuseTabStrategy } from "@delon/abc/reuse-tab";
import { DelonACLModule } from "@delon/acl";
import { AlainThemeModule } from "@delon/theme";
import { AlainConfig, ALAIN_CONFIG } from "@delon/util/config";
import { environment } from "@env/environment";
import { GeexReuseTabStrategy } from "./shared/services/reuse-tab-strategy";

// Please refer to: https://ng-alain.com/docs/global-config
// #region NG-ALAIN Config
const alainConfigFactory = () =>
  ({
    st: {
      rowClassName: (record, i) => (i % 2 == 0 ? "even" : undefined),
      size: "small",
      modal: { size: "lg" },
      bordered: true,
      page: {
        front: false,
        showSize: true,
        total: "当前 {{range[0]}}-{{range[1]}} 条，共 {{total}} 条记录",
      },
    },
    se: {},
    sf: {
      autocomplete: "off",
    },
    sv: {},
    pageHeader: { recursiveBreadcrumb: true, autoTitle: true },
    lodop: {
      license: `A59B099A586B3851E0F0D7FDBF37B603`,
      licenseA: `C94CEE276DB2187AE6B65D56B3FC2848`,
    },
    auth: {
      login_url: "/passport/login",
      ignores: [new RegExp(`/${new URL(environment.api.baseUrl).host}/`), /x_org_x.com/],
    },
    acl: {},
  } as AlainConfig);
// autoBreadcrumb
const alainModules: any[] = [AlainThemeModule.forRoot(), DelonACLModule.forRoot()];
const alainProvides = [{ provide: ALAIN_CONFIG, useFactory: alainConfigFactory }];

// #region reuse-tab
import { RouteReuseStrategy } from "@angular/router";
alainProvides.push({
  provide: RouteReuseStrategy,
  useClass: GeexReuseTabStrategy,
  deps: [ReuseTabService],
} as any);

// #endregion

// #endregion

// Please refer to: https://ng.ant.design/docs/global-config/en#how-to-use
// #region NG-ZORRO Config

import { NzConfig, NZ_CONFIG } from "ng-zorro-antd/core/config";

const ngZorroConfig: NzConfig = {};

const zorroProvides = [{ provide: NZ_CONFIG, useValue: ngZorroConfig }];

// #endregion

@NgModule({
  imports: [...alainModules, ...(environment.modules || [])],
})
export class GlobalConfigModule {
  constructor(@Optional() @SkipSelf() parentModule: GlobalConfigModule) {
    throwIfAlreadyLoaded(parentModule, "GlobalConfigModule");
  }

  static forRoot(): ModuleWithProviders<GlobalConfigModule> {
    return {
      ngModule: GlobalConfigModule,
      providers: [...alainProvides, ...zorroProvides],
    };
  }
}
