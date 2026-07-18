import { InjectionToken } from "@angular/core";

export const GEEX_MOBILE_PATH_SUFFIX = new InjectionToken<string>("GEEX_MOBILE_PATH_SUFFIX", {
  providedIn: "root",
  factory: () => "/query",
});
