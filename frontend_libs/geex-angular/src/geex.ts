import { InjectionToken, Injector, runInInjectionContext } from "@angular/core";
import type { GeexModuleContribution } from "./module-contribution";
import type { GeexModule } from "./modules";
import type { GeexModules } from "./modules";
import { createMessagingModule, createSettingsModule, createUiModule } from "./modules";

export type GeexOverrides<TExtensionModules extends Record<string, GeexModule> = Record<string, GeexModule>> = Partial<
  Omit<GeexModules<TExtensionModules>, "init">
>;

export type GeexExtensions<TExtensionModules extends Record<string, GeexModule> = Record<string, GeexModule>> =
  Partial<TExtensionModules>;

export let geex: GeexModules;
export let Geex = new InjectionToken<GeexModules>("Geex");

export function configGeex<TExtensionModules extends Record<string, GeexModule> = Record<string, never>>(
  injector: Injector,
  overrides: GeexOverrides<TExtensionModules> = {} as GeexOverrides<TExtensionModules>,
  contributions: readonly GeexModuleContribution[] = [],
) {
  runInInjectionContext(injector, () => {
    const modules = {
      settings: createSettingsModule(injector),
      ui: createUiModule(injector),
    } as GeexModules<TExtensionModules>;
    const moduleRecord = modules as unknown as Record<string, GeexModule>;
    modules.messaging = createMessagingModule(injector, () => modules["auth"]);

    for (const contribution of contributions) {
      const contributedModules = contribution.createModules({
        injector,
        modules,
      });
      for (const [name, module] of Object.entries(contributedModules)) {
        if (name === "init" || name in modules) {
          throw new Error(`Geex module "${name}" is already registered.`);
        }
        moduleRecord[name] = module;
      }
    }

    Object.assign(modules, overrides);

    let _initPromise: Promise<{ [K in keyof GeexModules<TExtensionModules>]: unknown }> | null = null;
    modules.init ??= (force = false) => {
      if (force) {
        _initPromise = null;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          const entries = Object.entries(modules).filter(([key]) => key !== "init");
          return Object.fromEntries(
            await Promise.all(
              entries.map(async ([key, mod]) => {
                const maybeInit = (mod as GeexModule).init;
                try {
                  return [key, await maybeInit(force)];
                } catch (err) {
                  console.error(err);
                  return [key, null];
                }
              }),
            ),
          ) as { [K in keyof GeexModules<TExtensionModules>]: unknown };
        })();
      }
      return _initPromise;
    };

    geex = modules;
  });
}
