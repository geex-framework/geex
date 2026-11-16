import { Location } from "@angular/common";
import { inject, Injectable } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { NavigationEnd, NavigationExtras, Router, UrlTree } from "@angular/router";
import { ReuseTabService } from "@delon/abc/reuse-tab";
import { GeexTabPolicy } from "./geex-tab-policy";

interface TabOrigin { key: string; url: string; }
interface TabContext { url: string; origin: TabOrigin; }
export interface GeexCloseTabOptions { fallbackUrl: string; refreshOrigin?: boolean; }

@Injectable({ providedIn: "root" })
export class GeexTabService {
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly reuse = inject(ReuseTabService);
  private readonly policy = inject(GeexTabPolicy);
  private readonly origins = new Map<string, TabOrigin>();

  constructor() {
    this.router.events.pipe(takeUntilDestroyed()).subscribe(event => {
      if (!(event instanceof NavigationEnd)) return;
      const context = (this.location.getState() as { geexTab?: TabContext })?.geexTab;
      if (context?.url === this.router.url && context.origin?.url?.startsWith("/") && !context.origin.url.startsWith("//")) {
        this.origins.set(this.currentKey(), context.origin);
      }
    });
  }

  private currentKey(): string {
    return this.policy.key(this.router.url, this.router.routerState.snapshot.root);
  }

  private contextFor(url: string): { geexTab?: TabContext } {
    const item = this.reuse.get(url);
    const key = this.policy.key(url, item?._snapshot);
    const origin = item ? this.origins.get(key) : undefined;
    return origin ? { geexTab: { url, origin } } : {};
  }

  async open(commands: any[], extras: NavigationExtras = {}): Promise<boolean> {
    const tree = this.router.createUrlTree(commands, extras);
    const url = this.router.serializeUrl(tree);
    const item = this.reuse.get(url);
    const key = this.policy.key(url, item?._snapshot);
    const retained = item && this.policy.resolve(url, item._snapshot).reuse !== "none" ? this.origins.get(key) : undefined;
    const origin = retained ?? { key: this.currentKey(), url: this.router.url };
    return this.router.navigateByUrl(tree, { ...extras, state: { ...extras.state, geexTab: { url, origin } } });
  }

  async activate(url: string): Promise<boolean> {
    return this.router.navigateByUrl(url, { state: this.contextFor(url) });
  }

  async openMenu(url: string | UrlTree): Promise<boolean> {
    const requested = typeof url === "string" ? url : this.router.serializeUrl(url);
    if (/[?#]/.test(requested)) return this.activate(requested);
    return this.activate(this.reuse.get(requested)?.url ?? requested);
  }

  async closeCurrent(options: GeexCloseTabOptions): Promise<boolean> {
    const closingUrl = this.router.url;
    const closingKey = this.currentKey();
    const restored = (this.location.getState() as { geexTab?: TabContext })?.geexTab;
    const origin = restored?.url === closingUrl ? restored.origin : this.origins.get(closingKey);
    const destination = origin?.key !== closingKey && origin?.url ? origin.url : options.fallbackUrl;
    const succeeded = await this.router.navigateByUrl(destination, {
      replaceUrl: true, forceReload: options.refreshOrigin, state: this.contextFor(destination),
    });
    if (!succeeded || this.currentKey() === closingKey) return false;
    this.reuse.close(closingUrl, true);
    this.origins.delete(closingKey);
    return true;
  }

  async close(url: string, includeNonCloseable = false): Promise<boolean> {
    const item = this.reuse.get(url);
    if (!item || (!includeNonCloseable && !item.closable)) return false;
    if (this.policy.key(item.url, item._snapshot) === this.currentKey()) {
      const index = this.reuse.index(url);
      const adjacent = this.reuse.items[index + 1] ?? this.reuse.items[index - 1];
      if (!adjacent) return false;
      return this.closeCurrent({ fallbackUrl: adjacent.url });
    }
    this.origins.delete(this.policy.key(item.url, item._snapshot));
    return this.reuse.close(item.url, includeNonCloseable);
  }

  async closeOthers(url: string, rightOnly: boolean, includeNonCloseable = false): Promise<boolean> {
    const index = this.reuse.index(url);
    const candidates = this.reuse.items.filter((item, i) => item.url !== url && (!rightOnly || i > index));
    if (candidates.some(item => this.policy.key(item.url, item._snapshot) === this.currentKey())) {
      if (!await this.activate(url)) return false;
    }
    for (const item of candidates) await this.close(item.url, includeNonCloseable);
    return true;
  }
}
