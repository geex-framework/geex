export type GeexEnvironmentOverridesOptions = {
  url?: string;
  onInvalid?: (override: unknown, url: string) => void;
  onLoadError?: (error: unknown, url: string) => void;
};

function setByPath(target: Record<string, unknown>, path: string, value: unknown): void {
  const parts = path.split(".");
  let cur: Record<string, unknown> = target;
  for (let i = 0; i < parts.length - 1; i++) {
    const key = parts[i];
    const next = cur[key];
    if (next == null || typeof next !== "object") {
      cur[key] = {};
    }
    cur = cur[key] as Record<string, unknown>;
  }
  cur[parts[parts.length - 1]] = value;
}

/** Deep-set env from flat / dotted override keys. Host may pass typed `environment` objects. */
export function applyEnvironmentOverrides(
  env: object,
  override: Record<string, unknown>,
): void {
  const target = env as Record<string, unknown>;
  for (const [key, value] of Object.entries(override)) {
    if (key.includes(".")) {
      setByPath(target, key, value);
    } else {
      target[key] = value;
    }
  }
}

/**
 * Loads `/assets/environment.override.js` (or options.url) and merges into env.
 * Missing / invalid override is non-fatal.
 */
export async function loadEnvironmentOverrides(
  env: object,
  options: GeexEnvironmentOverridesOptions = {},
): Promise<void> {
  const url = options.url ?? "/assets/environment.override.js";
  try {
    const mod = await import(/* @vite-ignore */ /* webpackIgnore: true */ url);
    const override = (mod.default ?? mod) as Record<string, unknown>;
    if (override && typeof override === "object") {
      applyEnvironmentOverrides(env, override);
      return;
    }
    if (options.onInvalid) {
      options.onInvalid(override, url);
    } else {
      console.warn(`[environment] Invalid override export from ${url}, expected object. Using defaults.`, override);
    }
  } catch (error) {
    if (options.onLoadError) {
      options.onLoadError(error, url);
    } else {
      console.warn(`[environment] Failed to load ${url}, using defaults.`, error);
    }
  }
}
