import { Provider, Type } from "@angular/core";
import { RouteReuseStrategy, Router } from "@angular/router";

import { GeexRouter } from "./router/geex-router";
import { GeexReuseTabStrategy } from "./router/geex-reuse-tab.strategy";

/**
 * Delon-coupled Core providers (Router subclass + hardened ReuseTab strategy).
 * Call after `provideReuseTabConfig(...)` so GeexReuseTabStrategy wins.
 */
export function provideGeexDelonBase(options?: {
  router?: Type<Router>;
  reuseStrategy?: Type<RouteReuseStrategy>;
}): Provider[] {
  return [
    { provide: Router, useClass: options?.router ?? GeexRouter },
    { provide: RouteReuseStrategy, useClass: options?.reuseStrategy ?? GeexReuseTabStrategy },
  ];
}
