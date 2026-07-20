import { importProvidersFrom, type EnvironmentProviders, type Provider } from "@angular/core";
import { ALAIN_I18N_TOKEN } from "@delon/theme";
import { TranslateLoader, TranslateModule } from "@ngx-translate/core";

import { GEEX_I18N, GEEX_I18N_SERVICE } from "../delon/tokens";
import { createGeexI18nDictionaryProxy } from "./geex-i18n-dictionary";
import { GeexI18nService } from "./geex-i18n.service";
import { GeexTranslateLoader } from "./geex-translate-loader";
import { GEEX_I18N_PACKS } from "./tokens";

export interface GeexI18nProvideOptions {
  fallbackLang?: string;
}

/**
 * Register kiwi packs + GeexI18nService + Alain/ngx-translate wiring.
 */
export function provideGeexI18n(
  packs: Record<string, Record<string, unknown>>,
  options: GeexI18nProvideOptions = {},
): Array<Provider | EnvironmentProviders> {
  return [
    { provide: GEEX_I18N_PACKS, useValue: packs },
    GeexI18nService,
    { provide: GEEX_I18N_SERVICE, useExisting: GeexI18nService },
    {
      provide: GEEX_I18N,
      useFactory: () => createGeexI18nDictionaryProxy(),
    },
    { provide: ALAIN_I18N_TOKEN, useExisting: GeexI18nService },
    importProvidersFrom(
      TranslateModule.forRoot({
        loader: {
          provide: TranslateLoader,
          useClass: GeexTranslateLoader,
        },
        fallbackLang: options.fallbackLang ?? "en",
      }),
    ),
  ];
}
