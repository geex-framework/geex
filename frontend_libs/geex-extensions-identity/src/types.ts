import type { WritableSignal } from "@angular/core";
import type { GeexModule } from "@geexcode/geex-angular";
export type { MessagingModule, SettingItem, SettingsModule, UiModule } from "@geexcode/geex-angular";

export interface Tenant {
  id: string;
  code: string;
  name: string;
  isEnabled: boolean;
  createdOn: Date;
  [key: string]: any;
}

/** @deprecated Use `Tenant`; alias for host `ITenant` compatibility. */
export type ITenant = Tenant;

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

export interface TenantModule extends GeexModule<{
  current: WritableSignal<Tenant | null>;
  loadTenantData(code: string): Promise<Tenant>;
  switchTenant(targetTenantCode: string): void;
}> {}

export interface AuthModule extends GeexModule<{
  user: WritableSignal<User | null>;
  loadUserData(): Promise<User | undefined>;
}> {}

export interface IdentityModule extends GeexModule<{
  orgs: WritableSignal<Org[]>;
  userOwnedOrgs: WritableSignal<Org[]>;
}> {}

export interface IdentityModuleDeps {
  tenant: Pick<TenantModule, "init">;
  auth: Pick<AuthModule, "init" | "user">;
}

export type IdentityModuleDepsFactory = () => IdentityModuleDeps;

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    tenant: TenantModule;
    auth: AuthModule;
    identity: IdentityModule;
  }
}
