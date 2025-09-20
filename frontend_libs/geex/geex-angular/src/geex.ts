import { InjectionToken, Injector, runInInjectionContext, signal } from "@angular/core";
import type { GeexModule, GeexModules } from "./modules";
import {
  createTenantModule,
  createAuthModule,
  createIdentityModule,
  createMessagingModule,
  createSettingsModule,
  createUiModule,
} from "./modules";

export type GeexOverrides<TExtensionModules extends Record<string, GeexModule> = {}> = Partial<GeexModules<TExtensionModules>>;

export let geex: GeexModules;
export let Geex = new InjectionToken<GeexModules>("Geex");

/**
 * Initialize geex singleton with concrete dependencies
 * @param deps   Required runtime services
 * @param overrides  Custom module overrides – properties will be merged into generated modules
 */
export function configGeex<TExtensionModules extends Record<string, GeexModule> = {}>(injector: Injector, overrides?: GeexOverrides<TExtensionModules>) {
  runInInjectionContext(injector, () => {
    const modules: GeexModules = {
      init: async () => {
        const entries = Object.entries(modules).filter(([key]) => key !== "init");
        await Promise.all(
          entries.map(async ([, mod]) => {
            const maybeInit = (mod as GeexModule).init;
            if (typeof maybeInit === "function") {
              try {
                await maybeInit();
              } catch (err) {
                console.error(err);
              }
            }
          })
        );
      },
      tenant: createTenantModule(injector),
      auth: createAuthModule(injector),
      identity: createIdentityModule(injector),
      messaging: createMessagingModule(injector),
      settings: createSettingsModule(injector),
      ui: createUiModule(injector),
    };
    if (overrides) {
      Object.assign(modules, overrides);
    }
    geex = modules;
  });
}
