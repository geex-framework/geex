import { DestroyRef, inject, Injectable, Injector } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { ActivatedRouteSnapshot, DetachedRouteHandle, NavigationCancel, NavigationError, NavigationEnd, Router } from "@angular/router";
import { REUSE_TAB_CACHED_MANAGER, ReuseTabCached, ReuseTabService } from "@delon/abc/reuse-tab";
import { GeexTabOptions, GeexTabPolicy, geexTabKey } from "./geex-tab-policy";

export interface GeexTabCached extends ReuseTabCached {
  tabKey?: string;
  tabPolicy?: GeexTabOptions;
}

@Injectable()
export class GeexReuseTabService extends ReuseTabService {
  private readonly context = inject(Injector);
  private readonly policy = inject(GeexTabPolicy);
  private readonly cache = inject(REUSE_TAB_CACHED_MANAGER);
  private active?: { url: string; key: string; policy: GeexTabOptions };
  private listening = false;

  constructor() {
    super();
    this.change.pipe(takeUntilDestroyed()).subscribe(event => {
      if (event?.active !== "title" || !event.url || !event.title) return;
      const item = this.get(event.url);
      if (item) item.title = event.title;
    });
  }

  override get curUrl(): string {
    return this.context.get(Router).url;
  }

  override getUrl(route: ActivatedRouteSnapshot): string {
    return (route as ActivatedRouteSnapshot & { getResolvedUrl(): string }).getResolvedUrl();
  }

  key(url: string, snapshot?: ActivatedRouteSnapshot): string {
    return this.policy.key(url, snapshot);
  }

  override index(url: string): number {
    return this.items.findIndex((item: GeexTabCached) => {
      const policy = item.tabPolicy ?? this.policy.resolve(item.url, item._snapshot);
      return geexTabKey(url, policy) === (item.tabKey ?? geexTabKey(item.url, policy));
    });
  }

  override get(url?: string): GeexTabCached | null {
    return url ? this.items[this.index(url)] ?? null : null;
  }

  override saveCache(snapshot: ActivatedRouteSnapshot, handle: any, pos?: number): void {
    const url = this.getUrl(snapshot);
    const existing = this.get(url);
    if (existing && existing.url !== url && this.cache) {
      for (const values of [this.cache.title, this.cache.closable]) {
        if (Object.prototype.hasOwnProperty.call(values, existing.url)) {
          values[url] = values[existing.url];
          delete values[existing.url];
        }
      }
    }
    super.saveCache(snapshot, handle, pos);
    const item = this.get(url)!;
    item.tabPolicy = this.policy.resolve(url, snapshot);
    item.tabKey = geexTabKey(url, item.tabPolicy);
  }

  override shouldDetach(route: ActivatedRouteSnapshot): boolean {
    return this.policy.resolve(this.getUrl(route), route).reuse !== "none" && super.shouldDetach(route);
  }

  override shouldAttach(route: ActivatedRouteSnapshot): boolean {
    if (!route.routeConfig || route.routeConfig.children || route.routeConfig.loadChildren) return false;
    return this.policy.resolve(this.getUrl(route), route).reuse !== "none" && !!this.get(this.getUrl(route))?._handle;
  }

  override retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle | null {
    return this.shouldAttach(route) ? super.retrieve(route) : null;
  }

  override shouldReuseRoute(future: ActivatedRouteSnapshot, current: ActivatedRouteSnapshot): boolean {
    if (future.routeConfig !== current.routeConfig) return false;
    if (!future.component || future.routeConfig?.children || future.routeConfig?.loadChildren) return true;
    const policy = this.policy.resolve(this.getUrl(future), future);
    return policy.reuse !== "none" && geexTabKey(this.getUrl(future), policy) === geexTabKey(this.getUrl(current), policy);
  }

  override init(): void {
    if (!this.listening) {
      this.listening = true;
      this.context.get(Router).events.pipe(takeUntilDestroyed(this.context.get(DestroyRef))).subscribe(event => {
        if (event instanceof NavigationEnd) {
          this.synchronize();
        } else if (event instanceof NavigationCancel || event instanceof NavigationError) {
          this.refresh();
        }
      });
    }
    super.init();
  }

  private synchronize(): void {
    const router = this.context.get(Router);
    const url = router.url;
    if (this.isExclude(url)) {
      if (this.active?.policy.reuse === "none") super.close(this.active.url, true);
      this.active = undefined;
      return;
    }
    const snapshot = router.routerState.snapshot.root;
    const policy = this.policy.resolve(url, snapshot);
    const key = geexTabKey(url, policy);
    const previous = this.active;
    this.saveCache(snapshot, null);
    this.active = { url, key, policy };
    if (previous?.policy.reuse === "none" && previous.key !== key) super.close(previous.url, true);
    this.refresh();
  }
}
