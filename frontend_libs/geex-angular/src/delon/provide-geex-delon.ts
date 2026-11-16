import { importProvidersFrom, type EnvironmentProviders, type Provider, type Type } from "@angular/core";
import { RouteReuseStrategy, Router } from "@angular/router";
import { provideReuseTabConfig, ReuseTabMatchMode, ReuseTabService } from "@delon/abc/reuse-tab";
import { AlainThemeModule } from "@delon/theme";
import { DelonFormModule } from "@delon/form";

import { GEEX_APP_PERMISSION, type GeexAppPermission } from "./tokens";
import { GeexRouter } from "./router/geex-router";
import { GeexReuseTabStrategy } from "./router/geex-reuse-tab.strategy";
import { GeexReuseTabService } from "./router/geex-reuse-tab.service";
import { GEEX_TAB_DEFAULTS, GeexTabDefaults } from "./router/geex-tab-policy";

export type GeexReuseTabOptions = NonNullable<Parameters<typeof provideReuseTabConfig>[0]> & {
  defaults?: Partial<GeexTabDefaults>;
};

export interface GeexDelonProvideOptions {
  router?: Type<Router>;
  reuseStrategy?: Type<RouteReuseStrategy>;
  appPermission?: GeexAppPermission;
  reuseTab?: GeexReuseTabOptions;
}

/**
 * Delon-coupled Core providers (Router subclass + ReuseTab + optional AppPermission).
 */
export function provideGeexDelonBase(
  options: GeexDelonProvideOptions = {},
): Array<Provider | EnvironmentProviders> {
  return [
    ...(options.appPermission ? [{ provide: GEEX_APP_PERMISSION, useValue: options.appPermission }] : []),
    provideReuseTabConfig({ mode: ReuseTabMatchMode.URL, ...options.reuseTab }),
    { provide: GEEX_TAB_DEFAULTS, useValue: { menu: "path", other: "full", ...options.reuseTab?.defaults } },
    { provide: ReuseTabService, useClass: GeexReuseTabService },
    { provide: Router, useClass: options.router ?? GeexRouter },
    { provide: RouteReuseStrategy, useClass: options.reuseStrategy ?? GeexReuseTabStrategy },
    importProvidersFrom(AlainThemeModule.forRoot(), DelonFormModule.forRoot()),
  ];
}
