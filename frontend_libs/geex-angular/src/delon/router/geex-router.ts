import { effect, Injectable, Injector } from "@angular/core";
import {
  Router,
  NavigationExtras,
  UrlTree,
  NavigationEnd,
  RouteConfigLoadEnd,
  Route,
} from "@angular/router";
import { Location } from "@angular/common";
import { P } from "ts-pattern";
import { match } from "ts-pattern";
import { ReuseTabService } from "@delon/abc/reuse-tab";
import rison from "../../rison";

import { RoutedComponent } from "../base/routed.component.base";

@Injectable()
export class GeexRouter extends Router {
  lastRoute!: Route;
  constructor(injector: Injector) {
    super();
    const routerEvent = (this.events as any).toSignal();
    effect(() => {
      match(routerEvent())
        .with(P.instanceOf(RouteConfigLoadEnd), (x: RouteConfigLoadEnd) => {
          this.lastRoute = x.route;
        })
        .with(P.instanceOf(NavigationEnd), async (x: NavigationEnd) => {
          const tabSrv = injector.get(ReuseTabService);
          const location = injector.get(Location);
          const cachedTabs = tabSrv.items;
          const deepest = (this.routerState.snapshot.root as any).getDeepestRouteConfig?.();
          const activeRoutedPage = deepest?.component;
          if (!(activeRoutedPage?.prototype instanceof RoutedComponent)) {
            return;
          }
          const currentUrl = this.lastSuccessfulNavigation?.extractedUrl?.toString();
          const previousUrl = this.lastSuccessfulNavigation?.previousNavigation?.extractedUrl?.toString();

          const cachedTab = cachedTabs.find(tab => tab.url === previousUrl);
          if (
            this.lastRoute?.data?.["reuse"] === false ||
            ((this.lastSuccessfulNavigation?.extras as any)?.replaceUrl &&
              currentUrl !== previousUrl &&
              this.isDifferentPath(currentUrl, previousUrl))
          ) {
            cachedTab && tabSrv.close(previousUrl!);
          }
          if ((this.lastSuccessfulNavigation?.extras as any)?.forceReload || cachedTabs.every(tab => tab.url !== currentUrl)) {
            (this as any).navigationReload.set({
              ...x,
              ...this.lastSuccessfulNavigation,
            });
          }
          location.replaceState(currentUrl!);
        });
    }, {});
  }

  private isDifferentPath(currentUrl?: string, previousUrl?: string): boolean {
    if (!currentUrl || !previousUrl) {
      return true;
    }
    const getCurrentPath = (url: string) => {
      const questionMarkIndex = url.indexOf("?");
      return questionMarkIndex === -1 ? url : url.substring(0, questionMarkIndex);
    };
    return getCurrentPath(currentUrl) !== getCurrentPath(previousUrl);
  }

  override createUrlTree(commands: any[], navigationExtras: NavigationExtras = {}): UrlTree {
    if (navigationExtras.queryParams) {
      const processedParams: Record<string, unknown> = {};
      for (const key in navigationExtras.queryParams) {
        const value = navigationExtras.queryParams[key];
        try {
          processedParams[key] = rison.encode(value);
        } catch (e) {
          console.warn(e);
          processedParams[key] = value;
        }
      }
      navigationExtras = {
        ...navigationExtras,
        queryParams: processedParams,
      };
    }
    return super.createUrlTree(commands, navigationExtras);
  }
}
