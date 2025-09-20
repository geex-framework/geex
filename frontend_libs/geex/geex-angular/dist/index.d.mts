import { WritableSignal, Signal, Injector, InjectionToken, Provider } from '@angular/core';

interface Tenant {
    /** Unique identifier */
    id: string;
    /** Tenant code */
    code: string;
    /** Tenant display name */
    name: string;
    /** Whether the tenant is enabled */
    isEnabled: boolean;
    /** Creation timestamp */
    createdOn: Date;
    [key: string]: any;
}
declare const LoginProviderEnum: Record<string, string> & {
    Local: "Local";
};
type LoginProviderEnum = (typeof LoginProviderEnum)[keyof typeof LoginProviderEnum];
declare const OrgTypeEnum: Record<string, string> & {
    Default: "Default";
};
type OrgTypeEnum = (typeof OrgTypeEnum)[keyof typeof OrgTypeEnum];
interface Org {
    /** Unique identifier */
    id: string;
    /** Organization type */
    orgType: OrgTypeEnum;
    /** Hierarchical code */
    code: string;
    /** Organization name */
    name: string;
    /** Parent organization code */
    parentOrgCode: string;
    [key: string]: any;
}
interface UserOrgMembership {
    code: string;
    name: string;
    allParentOrgs: Org[];
}
interface User {
    id: string;
    orgs: UserOrgMembership[];
    isEnable: boolean;
    permissions: string[];
    orgCodes: string[];
    username: string;
    claims: {
        claimType: string;
        claimValue: string;
    }[];
    roleIds: string[];
    loginProvider: LoginProviderEnum;
    roleNames: string[];
    [key: string]: any;
}
interface SettingItem {
    name: string;
    value?: any;
}
interface TenantModule extends GeexModule {
    /** Current tenant reactive state */
    current: WritableSignal<Tenant | null>;
    /**
     * Load tenant information by code.
     */
    loadTenantData(code: string): Promise<Tenant>;
    /**
     * Switch current tenant.
     */
    switchTenant(targetTenantCode: string): void;
}
interface AuthModule extends GeexModule {
    /** Current authenticated user */
    user: WritableSignal<User | null>;
    /**
     * Load current user information. Returns undefined when unauthenticated.
     */
    loadUserData(): Promise<User | undefined>;
}
interface IdentityModule extends GeexModule {
    /** All organizations */
    orgs: WritableSignal<Org[]>;
    /** Organizations owned by current user */
    userOwnedOrgs: WritableSignal<Org[]>;
}
interface MessagingModule extends GeexModule {
    /**
     * Subscribe to server broadcast events.
     */
    onPublicNotify(): void;
}
interface SettingsModule extends GeexModule {
    settings: WritableSignal<SettingItem[]>;
}
interface UiModule extends GeexModule {
    /** Whether the app is currently in fullscreen mode */
    fullScreen: WritableSignal<boolean>;
    /** Responsive helper stream notifying if device is mobile-like */
    isMobile: Signal<boolean | undefined>;
    /** Currently active routed component (framework dependent) */
    activeRoutedComponent?: any;
}
type GeexModules<TExtensionModules extends Record<string, GeexModule> = {}> = {
    init: () => Promise<any>;
    tenant: TenantModule;
    auth: AuthModule;
    identity: IdentityModule;
    messaging: MessagingModule;
    settings: SettingsModule;
    ui: UiModule;
} & TExtensionModules;
type GeexModule<TExtension = any> = {
    /**
     * A module combines both reactive state (signals) and business logic methods.
     * Concrete modules can extend this interface to expose their own signals & methods.
     * This empty base exists mainly for typing convenience and future extension.
     */
    init?: () => Promise<any>;
} & TExtension;
declare function createTenantModule(injector: Injector): TenantModule;
declare function createAuthModule(injector: Injector): AuthModule;
declare function createIdentityModule(injector: Injector): IdentityModule;
declare function createMessagingModule(injector: Injector): MessagingModule;
declare function createSettingsModule(injector: Injector): SettingsModule;
declare function createUiModule(injector: Injector): UiModule;

type GeexOverrides<TExtensionModules extends Record<string, GeexModule> = {}> = Partial<GeexModules<TExtensionModules>>;
declare let geex: GeexModules;
declare let Geex: InjectionToken<{
    init: () => Promise<any>;
    tenant: TenantModule;
    auth: AuthModule;
    identity: IdentityModule;
    messaging: MessagingModule;
    settings: SettingsModule;
    ui: UiModule;
}>;
/**
 * Initialize geex singleton with concrete dependencies
 * @param deps   Required runtime services
 * @param overrides  Custom module overrides – properties will be merged into generated modules
 */
declare function configGeex<TExtensionModules extends Record<string, GeexModule> = {}>(injector: Injector, overrides?: GeexOverrides<TExtensionModules>): void;

declare function provideGeex(overrides?: GeexOverrides): Provider[];

export { type AuthModule, Geex, type GeexModule, type GeexModules, type GeexOverrides, type IdentityModule, LoginProviderEnum, type MessagingModule, type Org, OrgTypeEnum, type SettingItem, type SettingsModule, type Tenant, type TenantModule, type UiModule, type User, type UserOrgMembership, configGeex, createAuthModule, createIdentityModule, createMessagingModule, createSettingsModule, createTenantModule, createUiModule, geex, provideGeex };
