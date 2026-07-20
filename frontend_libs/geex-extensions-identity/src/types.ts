import type { WritableSignal } from "@angular/core";
import type { GeexModule } from "@geexcode/geex-angular";
export type { UiModule } from "@geexcode/geex-angular";
export type { SettingItem, SettingsModule } from "@geexcode/geex-extensions-settings";
export type { Tenant, ITenant, MultiTenantModule } from "@geexcode/geex-extensions-multi-tenant";
export type { MessagingModule } from "@geexcode/geex-extensions-messaging";

export const GEEX_DEFAULT_SUPER_ADMIN_USER_ID = "000000000000000000000001";

export const LoginProviderEnum: Record<string, string> & {
  Local: "Local";
} = {
  Local: "Local",
};

export type LoginProviderEnum = (typeof LoginProviderEnum)[keyof typeof LoginProviderEnum];

export const OrgTypeEnum: Record<string, string> & {
  Default: "Default";
} = {
  Default: "Default",
};

export type OrgTypeEnum = (typeof OrgTypeEnum)[keyof typeof OrgTypeEnum];

export interface Org {
  id: string;
  orgType: OrgTypeEnum;
  code: string;
  name: string;
  parentOrgCode: string;
  [key: string]: any;
}

export interface UserOrgMembership {
  code: string;
  name: string;
  allParentOrgs: Org[];
}

export interface User {
  id: string;
  orgs: UserOrgMembership[];
  isEnable: boolean;
  permissions: string[];
  orgCodes: string[];
  username: string;
  claims: { claimType: string; claimValue: string }[];
  roleIds: string[];
  loginProvider: LoginProviderEnum;
  roleNames: string[];
  [key: string]: any;
}

export interface IdentityModule extends GeexModule<{
  orgs: WritableSignal<Org[]>;
  userOwnedOrgs: WritableSignal<Org[]>;
}> {}

/** Authentication module dependency shape; kept structural to avoid depending on `@geexcode/geex-extensions-authentication`. */
export interface IdentityAuthenticationDeps {
  init: (force?: boolean) => Promise<unknown>;
  user: WritableSignal<User | null>;
}

export interface IdentityModuleDeps {
  multiTenant: Pick<GeexModule, "init">;
  authentication: IdentityAuthenticationDeps;
}

export type IdentityModuleDepsFactory = () => IdentityModuleDeps;

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    identity: IdentityModule;
  }
}
