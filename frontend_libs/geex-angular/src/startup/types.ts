import type { AuthConfig } from "angular-oauth2-oidc";

/** Startup-only orchestration. Module-owned keys live on their provideGeex* entrypoints. */
export interface GeexStartupOptions {
  oauth: {
    getConfig: () => AuthConfig;
  };
  /** Forward host `environment.blockDebugger`. */
  blockDebugger?: boolean;
}

export interface GeexSessionTerminatedCopy {
  title?: string;
  okText?: string;
}

export interface GeexStartupI18nAdapter {
  merge(translations: object): void;
  use(lang: string): void;
}
