import { inject, Injectable, InjectionToken } from "@angular/core";
import { ActivatedRouteSnapshot, DefaultUrlSerializer, Params, UrlTree } from "@angular/router";
import { MenuService } from "@delon/theme";

export type GeexTabReuse = "none" | "path" | "query" | "full";
export type GeexTabOptions =
  | { reuse: "none" | "path" | "full" }
  | { reuse: "query"; queryKeys?: readonly string[] };
export interface GeexTabDefaults { menu: GeexTabReuse; other: GeexTabReuse; }
export const GEEX_TAB_DEFAULTS = new InjectionToken<GeexTabDefaults>("GEEX_TAB_DEFAULTS", {
  providedIn: "root", factory: () => ({ menu: "path", other: "full" }),
});

const serializer = new DefaultUrlSerializer();

export function geexTabPath(url: string): string {
  const tree = serializer.parse(url);
  return serializer.serialize(new UrlTree(tree.root));
}

export function geexTabKey(url: string, policy: GeexTabOptions): string {
  const tree = serializer.parse(url);
  const query: Params = {};
  if (policy.reuse !== "path") {
    const keys = policy.reuse === "query" && policy.queryKeys ? policy.queryKeys : Object.keys(tree.queryParams);
    for (const key of [...new Set(keys)].sort()) {
      if (Object.prototype.hasOwnProperty.call(tree.queryParams, key)) query[key] = tree.queryParams[key];
    }
  }
  return serializer.serialize(new UrlTree(tree.root, query,
    policy.reuse === "full" || policy.reuse === "none" ? tree.fragment : null));
}

@Injectable({ providedIn: "root" })
export class GeexTabPolicy {
  private readonly menus = inject(MenuService);
  private readonly defaults = inject(GEEX_TAB_DEFAULTS);

  resolve(url: string, snapshot?: ActivatedRouteSnapshot): GeexTabOptions {
    while (snapshot?.firstChild) snapshot = snapshot.firstChild;
    const data = snapshot?.routeConfig?.data;
    if (data?.["tab"]) return data["tab"] as GeexTabOptions;
    if (data?.["reuse"] === false) return { reuse: "none" };
    const path = geexTabPath(url);
    const menu = this.menus.getPathByUrl(path, false)?.some(item => item.link && geexTabPath(item.link) === path);
    return { reuse: menu ? this.defaults.menu : this.defaults.other };
  }

  key(url: string, snapshot?: ActivatedRouteSnapshot): string {
    return geexTabKey(url, this.resolve(url, snapshot));
  }
}

declare module "@angular/router" {
  interface RouteData { tab?: GeexTabOptions; }
}
