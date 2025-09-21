import { Injector, Provider } from "@angular/core";
import { configGeex, GeexOverrides, Geex, geex } from "./geex";
import { GeexModule, GeexModules } from "./modules";

export function provideGeex<TExtensionModules extends Record<string, GeexModule> = {}>(
  overrides: Partial<GeexModules> = {} as Partial<GeexModules>,
  extensions: TExtensionModules = {} as TExtensionModules,
): Provider[] {
  return [
    {
      provide: Geex,
      useFactory: (injector: Injector) => {
        // Merge extensions first so that overrides take precedence when there are conflicts
        const mergedModules = {
          ...(extensions as Record<string, GeexModule>),
          ...(overrides as Record<string, GeexModule>),
        } as GeexOverrides<TExtensionModules>;

        configGeex<TExtensionModules>(injector, mergedModules);
        return geex;
      },
      deps: [Injector],
    },
  ];
}
