import { InjectionToken } from "@angular/core";

export const GEEX_EXCEPTION_403_PROFILE_PATH = new InjectionToken<string>("GEEX_EXCEPTION_403_PROFILE_PATH", {
  providedIn: "root",
  factory: () => "/identity/me",
});

export const GEEX_EXCEPTION_403_PROFILE_LABEL = new InjectionToken<string>("GEEX_EXCEPTION_403_PROFILE_LABEL", {
  providedIn: "root",
  factory: () => "个人中心",
});

export const GEEX_EXCEPTION_LOGIN_PATH = new InjectionToken<string>("GEEX_EXCEPTION_LOGIN_PATH", {
  providedIn: "root",
  factory: () => "/authentication/login",
});
