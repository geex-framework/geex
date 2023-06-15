import { Injector } from "@angular/core";
import { ActivatedRouteSnapshot, NavigationExtras, Router, ActivatedRoute } from "@angular/router";
import { ReuseTabService } from "@delon/abc/reuse-tab";
import { Observable } from "rxjs";

function getResolvedUrl(this: ActivatedRouteSnapshot): string {
  return this["_routerState"].url;
}

function getConfiguredUrl(this: ActivatedRouteSnapshot): string {
  return `/${this.pathFromRoot
    .filter(v => v.routeConfig)
    .map(v => v.routeConfig!.path)
    .where(x => x != "")
    .join("/")}`;
}

ActivatedRouteSnapshot.prototype.getResolvedUrl = getResolvedUrl;
ActivatedRouteSnapshot.prototype.getConfiguredUrl = getConfiguredUrl;

declare module "@angular/router" {
  interface ActivatedRouteSnapshot {
    getResolvedUrl(): string;
    getConfiguredUrl(): string;
  }
  interface RouteData extends Data {
    singleton?: boolean;
    reuse?: boolean;
    title?: string;
  }

  interface NavigationExtras extends UrlCreationOptions, NavigationBehaviorOptions {
    forceReload?: boolean;
  }

  type GeexRoutes = GeexRoute[];
  interface GeexRoute extends Route {
    data?: RouteData;
  }

  interface Router {
    navigationStart$: Observable<NavigationStart>;
    navigationEnd$: Observable<NavigationEnd>;
    virtualNavigate(commands: any[], extras?: NavigationExtras): void;
  }
}
