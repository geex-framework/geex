import { makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { FavoritesService } from "./favorites.service";
import type { FavoritesModule } from "./favorites.types";

export function provideGeexFavorites() {
  return makeEnvironmentProviders([
    FavoritesService,
    provideGeexModuleContribution({
      createModules: ({ injector, modules }): Record<string, FavoritesModule> => {
        if (modules["favorites"]) return {};
        const service = injector.get(FavoritesService);
        return { favorites: {
          types: service.types,
          refresh: () => service.refresh(),
          init: async (force = false) => { await modules["authentication"]?.init(); await service.init(force); },
        } };
      },
    }),
  ]);
}
