import { InjectionToken, Injector, runInInjectionContext, signal } from "@angular/core";
import type { GeexModule, GeexModules, ExtensionModule } from "./modules";
import {
  createTenantModule,
  createAuthModule,
  createIdentityModule,
  createMessagingModule,
  createSettingsModule,
  createUiModule,
} from "./modules";

export type GeexOverrides<TExtensionModules extends Record<string, GeexModule> = {}> = Partial<Omit<GeexModules<TExtensionModules>, "init">>;

export type GeexExtensions<TExtensionModules extends Record<string, GeexModule> = {}> = Partial<TExtensionModules>;

export let geex: GeexModules;
export let Geex = new InjectionToken<GeexModules>("Geex");

/**
 * Initialize geex singleton with concrete dependencies
 * @param deps   Required runtime services
 * @param overrides  Custom module overrides – properties will be merged into generated modules
 */
export function configGeex<TExtensionModules extends Record<string, GeexModule> = ExtensionModule>(
  injector: Injector,
  overrides: GeexOverrides<TExtensionModules> = {} as GeexOverrides<TExtensionModules>,
) {
  // Start with an empty collection and merge in overrides later (overrides have higher priority)
  const modules: GeexModules<TExtensionModules> = {
    ...(overrides as Partial<GeexModules<TExtensionModules>>) as any,
  } as GeexModules<TExtensionModules>;
  runInInjectionContext(injector, () => {
    modules.init ??= async () => {
      const entries = Object.entries(modules).filter(([key]) => key !== "init");
      return Object.fromEntries(await Promise.all(
        entries.map(async ([key, mod]) => {
          const maybeInit = (mod as GeexModule).init;
          try {
            return [key, await maybeInit()]
          } catch (err) {
            console.error(err);
            return [key, null];
          }
        })
      ));
    };
    modules.tenant ??= createTenantModule(injector);
    modules.auth ??= createAuthModule(injector);
    modules.identity ??= createIdentityModule(injector);
    modules.messaging ??= createMessagingModule(injector);
    modules.settings ??= createSettingsModule(injector);
    modules.ui ??= createUiModule(injector);

    geex = modules;
  });
}
