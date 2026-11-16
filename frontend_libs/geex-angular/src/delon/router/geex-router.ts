import { DestroyRef, Injectable, Injector } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Router, NavigationExtras, UrlTree, NavigationStart, NavigationEnd, RoutesRecognized } from "@angular/router";
import { ReuseTabService } from "@delon/abc/reuse-tab";
import rison from "../../rison";

@Injectable()
export class GeexRouter extends Router {
  constructor(injector: Injector) {
    super();
    let previousUrl = this.url;
    this.events.pipe(takeUntilDestroyed(injector.get(DestroyRef))).subscribe(event => {
      if (event instanceof NavigationStart) previousUrl = this.url;
      if (event instanceof RoutesRecognized) {
        const routes = [event.state.root];
        while (routes.length) {
          const route = routes.shift()!;
          const config = route.routeConfig;
          if (config?.canDeactivate?.length && config.runGuardsAndResolvers === undefined) {
            config.runGuardsAndResolvers = "always";
          }
          routes.push(...route.children);
        }
      }
      if (event instanceof NavigationEnd) {
        this.navigationReload.set({ ...event, ...this.getCurrentNavigation() });
        if (this.getCurrentNavigation()?.extras.closeCurrentTab) {
          const tabs = injector.get(ReuseTabService);
          if (tabs.get(previousUrl) && tabs.index(previousUrl) !== tabs.index(event.urlAfterRedirects)) tabs.close(previousUrl, true);
        }
      }
    });
  }

  override createUrlTree(commands: any[], navigationExtras: NavigationExtras = {}): UrlTree {
    if (navigationExtras.queryParams) {
      const processedParams: Record<string, unknown> = {};
      for (const key in navigationExtras.queryParams) {
        const value = navigationExtras.queryParams[key];
        try {
          processedParams[key] = rison.encode(value);
        } catch (error) {
          console.warn(error);
          processedParams[key] = value;
        }
      }
      navigationExtras = { ...navigationExtras, queryParams: processedParams };
    }
    return super.createUrlTree(commands, navigationExtras);
  }
}
