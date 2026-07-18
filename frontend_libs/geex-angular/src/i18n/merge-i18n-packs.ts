import { merge } from "lodash-es";

export function mergeGeexI18nPacks<T extends Record<string, unknown>>(base: T, ...overlays: Array<Partial<T> | Record<string, unknown>>): T {
  return merge({}, base, ...overlays) as T;
}
