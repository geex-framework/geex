import { InjectionToken } from "@angular/core";

/** Per-language ngx-translate dictionaries keyed by locale code (e.g. `zh-cn`). */
export const GEEX_I18N_PACKS = new InjectionToken<Record<string, Record<string, unknown>>>("GEEX_I18N_PACKS");

/** Well-known setting names for post-login localization (aligned with SettingDefinition). */
export const GEEX_LOCALIZATION_DATA_SETTING = "LocalizationData";
export const GEEX_LOCALIZATION_LANGUAGE_SETTING = "LocalizationLanguage";
