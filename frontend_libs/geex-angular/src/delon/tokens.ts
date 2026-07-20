import { InjectionToken } from "@angular/core";

/**
 * Host-augmentable i18n dictionary shape.
 * Apps should `declare module "@geexcode/geex-angular" { interface GeexI18n extends ... {} }`
 * (typically in module-registry) so `inject(GEEX_I18N)` / `BusinessComponentBase.I18N` stay typed.
 */
export interface GeexI18n {}

/**
 * Host provides AlainI18NService-compatible instance (e.g. `GeexI18nService`).
 */
export const GEEX_I18N_SERVICE = new InjectionToken<unknown>("GEEX_I18N_SERVICE");

/**
 * Host-augmentable AppPermission map / enum object.
 * Apps should augment `GeexAppPermission` from generated `AppPermission`.
 */
export interface GeexAppPermission {}

/**
 * Typed kiwi/i18n dictionary (augment `GeexI18n` in the host app).
 */
export const GEEX_I18N = new InjectionToken<GeexI18n>("GEEX_I18N");

/**
 * Typed AppPermission enum/map (augment `GeexAppPermission` in the host app).
 */
export const GEEX_APP_PERMISSION = new InjectionToken<GeexAppPermission>("GEEX_APP_PERMISSION");
