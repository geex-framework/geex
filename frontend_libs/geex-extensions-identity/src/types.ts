import type { WritableSignal } from "@angular/core";
import type { GeexModule } from "@geexcode/geex-angular";
export type { MessagingModule, SettingItem, SettingsModule, UiModule } from "@geexcode/geex-angular";
export type { Tenant, ITenant, TenantModule } from "@geexcode/geex-extensions-multi-tenant";

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

/** Auth module dependency shape; kept structural to avoid depending on `@geexcode/geex-extensions-authentication`. */
export interface IdentityAuthDeps {
  init: (force?: boolean) => Promise<unknown>;
  user: WritableSignal<User | null>;
}

export interface IdentityModuleDeps {
  tenant: Pick<GeexModule, "init">;
  auth: IdentityAuthDeps;
}

export type IdentityModuleDepsFactory = () => IdentityModuleDeps;

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    identity: IdentityModule;
  }
}
