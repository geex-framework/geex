import { InjectionToken } from "@angular/core";
import { geex } from "@geexcode/geex-angular";

export type GeexPermissionFilterFn = (permissions: string[]) => string[];

export const defaultGeexPermissionFilter: GeexPermissionFilterFn = permissions => {
  const tenant = geex.tenant.current?.();
  if (tenant?.code != undefined) {
    return permissions.filter(x => !x.toString().startsWith("multiTenant_"));
  }
  return permissions;
};

export const GEEX_PERMISSION_FILTER = new InjectionToken<GeexPermissionFilterFn>("GEEX_PERMISSION_FILTER", {
  providedIn: "root",
  factory: () => defaultGeexPermissionFilter,
});
