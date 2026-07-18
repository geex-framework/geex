import type { AuthConfig } from "angular-oauth2-oidc";
import type { Menu } from "@delon/theme";

export interface GeexStartupSettingKeys {
  appName: string;
  appMenu: string;
  localizationData: string;
  localizationLanguage: string;
}

export interface GeexStartupModalCopy {
  sessionTerminatedTitle?: string;
  sessionTerminatedOkText?: string;
}

export interface GeexStartupI18nAdapter {
  merge(translations: object): void;
  use(lang: string): void;
}

export interface GeexStartupOptions {
  getOAuthConfig: () => AuthConfig;
  defaultMenus: Menu[];
  settingKeys: GeexStartupSettingKeys;
  loginUrl?: string;
  exception500Url?: string;
  superAdminUserId?: string;
  onDebuggerInit?: () => void;
  modalCopy?: GeexStartupModalCopy;
  i18n?: GeexStartupI18nAdapter;
}
