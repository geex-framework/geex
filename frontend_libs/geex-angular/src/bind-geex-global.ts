import { geex } from "./geex";

declare global {
  // Live binding to package singleton (assigned in configGeex).
  var geex: typeof import("./geex").geex;
  interface Window {
    geex: typeof import("./geex").geex;
  }
}

/**
 * Expose package `geex` on globalThis/window via a live getter.
 * Must not copy the value at bind time — `geex` is only assigned inside `configGeex`.
 */
export function bindGeexGlobal(): void {
  const bind = (target: object) => {
    Reflect.deleteProperty(target, "geex");
    Object.defineProperty(target, "geex", {
      configurable: true,
      enumerable: true,
      get: () => geex,
    });
  };

  if (typeof globalThis !== "undefined") {
    bind(globalThis);
  }
  if (typeof window !== "undefined") {
    bind(window);
  }
}
