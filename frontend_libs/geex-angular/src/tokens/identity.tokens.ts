import { InjectionToken } from "@angular/core";

export const GEEX_SUPER_ADMIN_USER_ID = new InjectionToken<string>("GEEX_SUPER_ADMIN_USER_ID", {
  providedIn: "root",
  factory: () => "000000000000000000000001",
});
