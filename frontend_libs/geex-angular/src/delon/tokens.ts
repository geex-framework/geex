import { InjectionToken } from "@angular/core";

/**
 * Host provides the kiwi/i18n dictionary object (e.g. `I18N` from admin i18n.service).
 */
export const GEEX_I18N = new InjectionToken<Record<string, any>>("GEEX_I18N");

/**
 * Host provides AlainI18NService-compatible instance (e.g. `I18NService`).
 */
export const GEEX_I18N_SERVICE = new InjectionToken<unknown>("GEEX_I18N_SERVICE");

/**
 * Host provides AppPermission enum / map for ACL templates.
 */
export const GEEX_APP_PERMISSION = new InjectionToken<Record<string, string>>("GEEX_APP_PERMISSION");
