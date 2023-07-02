import { Injector } from "@angular/core";
import { ActivatedRouteSnapshot, Navigation, Router, RouterStateSnapshot } from "@angular/router";
import { ReuseTabService, ReuseTabStrategy } from "@delon/abc/reuse-tab";
import { TitleService } from "@delon/theme";

export class GeexReuseTabStrategy extends ReuseTabStrategy {
  /**
   *
   */
  constructor(srv: ReuseTabService) {
    super(srv);
  }
  store(route: ActivatedRouteSnapshot, handle: unknown): void {
    return super.store(route, handle);
  }
  shouldDetach(route: ActivatedRouteSnapshot): boolean {
    return super.shouldDetach(route);
  }

  shouldAttach(route: ActivatedRouteSnapshot): boolean {
    return super.shouldAttach(route);
  }

  shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    return super.shouldReuseRoute(future, curr);
  }
}
