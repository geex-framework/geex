import { inject, Injector, Provider } from "@angular/core";
import { configGeex, GeexOverrides, Geex, geex } from "./geex";
import { GEEX_MODULE_CONTRIBUTIONS } from "./module-contribution";
import { GeexModule, GeexModules } from "./modules";

export function provideGeex<TExtensionModules extends Record<string, GeexModule> = {}>(
  overrides: Partial<GeexModules> = {} as Partial<GeexModules>,
  extensions: TExtensionModules = {} as TExtensionModules,
): Provider[] {
  return [
    {
      provide: Geex,
      useFactory: (injector: Injector) => {
        const contributions = inject(GEEX_MODULE_CONTRIBUTIONS, { optional: true }) ?? [];
        const mergedModules = {
          ...(extensions as Record<string, GeexModule>),
          ...(overrides as unknown as Record<string, GeexModule>),
        } as GeexOverrides<TExtensionModules>;

        configGeex<TExtensionModules>(injector, mergedModules, contributions);
        return geex;
      },
      deps: [Injector],
    },
  ];
}
