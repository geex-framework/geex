import { Provider } from "@angular/core";
import { provideGeex } from "./provide-geex";
import { GeexModule, GeexModules } from "./modules";

/**
 * Core meta-provide aligned with backend Geex.Common.
 * Installs geex signal modules. Delon page bases are opt-in via `provideGeexDelonBase()`.
 * Does not install admin business UI pages; use `geex add <name>` for source modules.
 */
export function provideGeexCommon<TExtensionModules extends Record<string, GeexModule> = {}>(
  overrides: Partial<GeexModules> = {} as Partial<GeexModules>,
  extensions: TExtensionModules = {} as TExtensionModules,
): Provider[] {
  return provideGeex(overrides, extensions);
}
