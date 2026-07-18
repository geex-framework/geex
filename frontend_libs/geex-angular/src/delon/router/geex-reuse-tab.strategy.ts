import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, DetachedRouteHandle } from "@angular/router";
import { ReuseTabStrategy } from "@delon/abc/reuse-tab";

/**
 * Hardens Delon ReuseTabStrategy against undefined snapshots / empty-path leaves
 * that otherwise throw during createRouterState / outlet.detach (NG04012).
 */
@Injectable()
export class GeexReuseTabStrategy extends ReuseTabStrategy {
  override shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    if (!future || !curr) {
      return false;
    }
    return super.shouldReuseRoute(future, curr);
  }

  override shouldDetach(route: ActivatedRouteSnapshot): boolean {
    if (!route?.routeConfig || route.routeConfig.path === "") {
      return false;
    }
    return super.shouldDetach(route);
  }

  override retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle | null {
    if (!route?.routeConfig) {
      return null;
    }
    return super.retrieve(route);
  }

  override shouldAttach(route: ActivatedRouteSnapshot): boolean {
    if (!route?.routeConfig) {
      return false;
    }
    return super.shouldAttach(route);
  }
}
