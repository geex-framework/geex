import { InjectionToken } from "@angular/core";

/** When true, DebuggerBlockerService activates anti-devtools measures. */
export const GEEX_BLOCK_DEBUGGER = new InjectionToken<boolean>("GEEX_BLOCK_DEBUGGER", {
  providedIn: "root",
  factory: () => false,
});
