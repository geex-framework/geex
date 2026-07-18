import { InjectionToken } from "@angular/core";
import type { DocumentNode } from "graphql";

export const GEEX_CANCEL_AUTHENTICATION_DOCUMENT = new InjectionToken<DocumentNode>("GEEX_CANCEL_AUTHENTICATION_DOCUMENT");

/** Header profile route (default `/identity/me`). */
export const GEEX_PROFILE_PATH = new InjectionToken<string>("GEEX_PROFILE_PATH", {
  providedIn: "root",
  factory: () => "/identity/me",
});

/** Header profile menu label (default 个人中心). */
export const GEEX_PROFILE_LABEL = new InjectionToken<string>("GEEX_PROFILE_LABEL", {
  providedIn: "root",
  factory: () => "个人中心",
});
