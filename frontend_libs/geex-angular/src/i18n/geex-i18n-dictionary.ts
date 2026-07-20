import type { GeexI18n } from "../delon/tokens";
import { I18N } from "./geex-i18n.service";

/**
 * Stable DI surface for `GEEX_I18N` that tracks module `I18N` reassignments
 * without depending on `GeexI18nService` (avoids TranslateService cycle).
 */
export function createGeexI18nDictionaryProxy(): GeexI18n {
  const proxy = new Proxy({} as Record<string, unknown>, {
    get(_target, prop, receiver) {
      const dict = I18N;
      if (dict == null) {
        return undefined;
      }
      const value = Reflect.get(dict as object, prop, receiver);
      if (typeof value === "function") {
        return (value as (...args: unknown[]) => unknown).bind(dict);
      }
      return value;
    },
  });
  return proxy as GeexI18n;
}
