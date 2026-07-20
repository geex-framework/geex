import { InjectionToken } from "@angular/core";

import type { GeexSessionTerminatedCopy, GeexStartupOptions } from "./types";

export const GEEX_STARTUP_OPTIONS = new InjectionToken<GeexStartupOptions>("GEEX_STARTUP_OPTIONS");

export const GEEX_EXCEPTION_500_PATH = new InjectionToken<string>("GEEX_EXCEPTION_500_PATH", {
  providedIn: "root",
  factory: () => "/exception/500",
});

export const GEEX_SESSION_TERMINATED_COPY = new InjectionToken<GeexSessionTerminatedCopy>(
  "GEEX_SESSION_TERMINATED_COPY",
  {
    providedIn: "root",
    factory: () => ({}),
  },
);
