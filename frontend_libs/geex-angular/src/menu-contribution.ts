import { InjectionToken, type Provider } from "@angular/core";

/** Minimal menu shape shared by host Delon menus and extension plugins. */
export interface GeexMenuItem {
  text?: string;
  link?: string;
  icon?: string | object;
  group?: boolean;
  hideInBreadcrumb?: boolean;
  open?: boolean;
  children?: GeexMenuItem[];
  [key: string]: unknown;
}

export interface GeexMenuContributionContext {
  id: string;
}

/**
 * Multi-provider token for additive menu contributions.
 * Host should merge contributions into app menus with a single MenuService.add([...app, ...extras]).
 */
export interface GeexMenuContribution {
  resolve(user: GeexMenuContributionContext): Promise<GeexMenuItem[]>;
}

export const GEEX_MENU_CONTRIBUTIONS = new InjectionToken<GeexMenuContribution[]>("GEEX_MENU_CONTRIBUTIONS");

/** Host-composed default menus (e.g. from module-registry). */
export const GEEX_DEFAULT_MENUS = new InjectionToken<GeexMenuItem[]>("GEEX_DEFAULT_MENUS", {
  providedIn: "root",
  factory: () => [],
});

export function provideGeexMenus(menus: GeexMenuItem[]): Provider[] {
  return [{ provide: GEEX_DEFAULT_MENUS, useValue: menus }];
}