import { Provider } from "@angular/core";

import { GEEX_I18N, GEEX_I18N_SERVICE } from "../delon/tokens";
import { createGeexI18nDictionaryProxy } from "./geex-i18n-dictionary";
import { GeexI18nService } from "./geex-i18n.service";
import { GEEX_I18N_PACKS } from "./tokens";

/**
 * Register kiwi packs + GeexI18nService.
 * Host should also: `{ provide: ALAIN_I18N_TOKEN, useExisting: GeexI18nService }` (or useClass).
 */
export function provideGeexI18n(packs: Record<string, Record<string, unknown>>): Provider[] {
  return [
    { provide: GEEX_I18N_PACKS, useValue: packs },
    GeexI18nService,
    { provide: GEEX_I18N_SERVICE, useExisting: GeexI18nService },
    {
      provide: GEEX_I18N,
      useFactory: () => createGeexI18nDictionaryProxy(),
    },
  ];
}
