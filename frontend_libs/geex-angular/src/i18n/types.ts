/**
 * Nested i18n dictionary typing.
 * Leaf values keep the source type (so Go-to-Definition can reach pack literals).
 * Nested objects also expose runtime `get(key)` from kiwi attachGetter.
 */
export type LangObject<O = Record<string, any>> = O extends object
  ? O extends (...args: never[]) => unknown
    ? O
    : {
        get(x: string, notFoundValue?: string): string;
      } & {
        [K in keyof O]: LangObject<O[K]>;
      }
  : O;
