import { Apollo } from "apollo-angular";
import { Injector, Signal, WritableSignal, runInInjectionContext, signal } from "@angular/core";
import { OAuthService } from "angular-oauth2-oidc";
import { CookieService, deepCopy } from "@delon/util";
import gql from "graphql-tag";
import { geex } from "./geex";
import { firstValueFrom, map, fromEvent } from "rxjs";
import { debounceTime, switchMap } from "rxjs/operators";
import { toSignal } from "@angular/core/rxjs-interop";

/* =============================================================
 * Domain shared types — keep minimal yet concrete enough to
 * eliminate any in public API while staying framework-agnostic.
 * =========================================================== */

export interface Tenant {
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

export interface SettingItem {
  name: string;
  value?: any;
}

export interface TenantModule extends GeexModule {
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

export interface AuthModule extends GeexModule {
  /** Current authenticated user */
  user: WritableSignal<User | null>;
  /**
   * Load current user information. Returns undefined when unauthenticated.
   */
  loadUserData(): Promise<User | undefined>;
}

export interface IdentityModule extends GeexModule {
  /** All organizations */
  orgs: WritableSignal<Org[]>;
  /** Organizations owned by current user */
  userOwnedOrgs: WritableSignal<Org[]>;
}

export interface MessagingModule extends GeexModule {
  /**
   * Subscribe to server broadcast events.
   */
  onPublicNotify(notify: any): void;
}

export interface SettingsModule extends GeexModule {
  settings: WritableSignal<SettingItem[]>;
}

// -------------------------------------------------------------
// UI module – state helpers shared across frontend projects
export interface UiModule extends GeexModule {
  /** Whether the app is currently in fullscreen mode */
  fullScreen: WritableSignal<boolean>;
  /** Responsive helper stream notifying if device is mobile-like */
  isMobile: Signal<boolean | undefined>;
  /** Currently active routed component (framework dependent) */
  activeRoutedComponent?: any;
}
export const ExtensionModule: Record<string, any> & {
} = {
};

export type ExtensionModule = typeof ExtensionModule;



export type GeexModules<TExtensionModules extends ExtensionModule = ExtensionModule> = {
  init(): Promise<{ [K in keyof GeexModules<TExtensionModules>]: any }>;
  tenant: TenantModule;
  auth: AuthModule;
  identity: IdentityModule;
  messaging: MessagingModule;
  settings: SettingsModule;
  ui: UiModule;
} & TExtensionModules;


export type GeexModule<TExtension = ExtensionModule> = {
  /**
   * A module combines both reactive state (signals) and business logic methods.
   * Concrete modules can extend this interface to expose their own signals & methods.
   * This empty base exists mainly for typing convenience and future extension.
   */
  init: () => Promise<any>;
} & TExtension;

// -------------------------------------------------------------
// GraphQL documents used by default module implementations (private to this file)
const GQL_CHECK_TENANT = gql`mutation checkTenant($code: String!) { checkTenant(code: $code) { id code name isEnabled createdOn } }`;
const GQL_FEDERATE_AUTH = gql`mutation federateAuthenticate(
  $code: String!
  $loginProvider: LoginProviderEnum!
) {
  federateAuthenticate(
    request: { code: $code, loginProvider: $loginProvider }
  ) {
    token
    loginProvider
    userId
    name
    user {
      id
      username
      nickname
      phoneNumber
      email
      isEnable
      openId
      loginProvider
      createdOn
      ... on IUser {
        roleNames
        roleIds
        permissions
        orgCodes
        avatarFileId
        orgs {
          allParentOrgs {
            code
            name
          }
          name
          code
        }
        claims {
          claimType
          claimValue
        }
        avatarFile {
          id
          createdOn
          fileSize
          mimeType
          storageType
          fileName
          md5
          url
        }
      }
    }
  }
}
`;
const GQL_ON_PUBLIC_NOTIFY = gql`subscription onPublicNotify { onPublicNotify { __typename ... on DataChangeClientNotify { dataChangeType } } }`;
const GQL_ORGS_CACHE = gql`query orgsCache { orgs(take: 999) { items { id orgType code name parentOrgCode } } }`;
const GQL_INIT_SETTINGS = gql`query initSettings { initSettings { id name value } }`;

// Default GraphQL documents are defined inside each module. Consumers can override by providing
// custom module implementations via the `overrides` parameter in `initGeex`.

// ----- default implementations -----

export function createTenantModule(injector: Injector): TenantModule {
  const current = signal<Tenant | null>(null);
  const module: TenantModule = {
    init: async () => {
      try {
        const tenantCode = injector.get(CookieService).get("__tenant");
        if (tenantCode) {
          const tenantData = await module.loadTenantData(tenantCode);
          current.set(tenantData ?? null);
        }
      } catch (err) {
        console.error(err);
      }
    },
    current,
    async loadTenantData(code: string): Promise<Tenant> {
      type CheckTenantResponse = { data: { checkTenant: Tenant } };
      const res = (await firstValueFrom(
        injector.get(Apollo).mutate<CheckTenantResponse>({ mutation: GQL_CHECK_TENANT, variables: { code } })
      )) as unknown as CheckTenantResponse;
      return res.data.checkTenant;
    },
    switchTenant(targetTenantCode: string) {
      const domainParts = location.hostname.split(".");
      if (domainParts.length > 2) {
        domainParts.shift();
      }
      const rootDomain = domainParts.join(".");
      injector.get(CookieService).put("__tenant", targetTenantCode, {
        secure: false,
        SameSite: "lax",
        HttpOnly: false,
        domain: rootDomain,
      });
    },
  } as TenantModule;
  return module;
};

export function createAuthModule(injector: Injector): AuthModule {
  const user = signal<User | null>(null);
  const module: AuthModule = {
    init: async () => {
      try {
        const userData = await module.loadUserData();
        user.set(userData ?? null);
      }
      catch (err) {
        console.error(err);
      }
    },
    user,
    async loadUserData(): Promise<User | undefined> {
      const oAuthService = injector.get(OAuthService);
      try {
        await oAuthService.loadDiscoveryDocument();
        if (!oAuthService.hasValidAccessToken()) {
          return undefined;
        }
        await oAuthService.loadUserProfile();
        const profile = oAuthService.getIdentityClaims() as { login_provider: string } | null;
        if (profile) {
          type FederateAuthResponse = { federateAuthenticate: { user: User } };
          const result = await firstValueFrom(injector.get(Apollo)
            .mutate<FederateAuthResponse>({
              mutation: GQL_FEDERATE_AUTH,
              variables: {
                code: oAuthService.getAccessToken(),
                loginProvider: profile.login_provider,
              },
            })
            .pipe(map(res => res?.data?.federateAuthenticate.user)));
          return result;
        }
      } catch (err) {
        console.error(err);
      }
      return undefined;
    },
  } as AuthModule;
  return module;
};

export function createIdentityModule(injector: Injector): IdentityModule {
  const orgsSignal = signal<Org[]>([]);
  const userOwnedOrgsSignal = signal<Org[]>([]);
  const module: IdentityModule = {
    orgs: orgsSignal,
    userOwnedOrgs: userOwnedOrgsSignal,
    async init() {
      try {
        const orgs$ = injector.get(Apollo)
          .watchQuery<{ orgs: { items: Org[] } }>({ query: GQL_ORGS_CACHE })
          .valueChanges.pipe(map(res => deepCopy(res.data.orgs.items) as Org[]));
        orgs$.subscribe((orgs: Org[]) => {
          runInInjectionContext(injector, () => {
            orgsSignal.set(orgs);
            const userData = geex.auth.user();
            let allOwned: Org[] = [];
            if (orgs?.length && userData) {
              if (userData.id === "000000000000000000000001") {
                allOwned = deepCopy(orgs);
              } else {
                const ownedCodes = userData.orgs.map((x: any) => x.code);
                allOwned = orgs.filter(o => ownedCodes.some(code => o.code.startsWith(code)));
              }
            }
            userOwnedOrgsSignal.set(allOwned);
          });
        });
      } catch (error) {
        console.error(error);
      }
    },
  };
  return module;
}

export function createMessagingModule(injector: Injector): MessagingModule {
  const module: MessagingModule = {
    async init() {
      try {
        await geex.auth.init();
        if (injector.get(OAuthService).hasValidAccessToken()) {
          const subClient = injector.get(Apollo).use("subscription");
          subClient
            .subscribe<{ onPublicNotify: any }>({ query: GQL_ON_PUBLIC_NOTIFY })
            .pipe(map(res => res?.data?.onPublicNotify))
            .subscribe(notify => {
              module.onPublicNotify(notify);
            });
        }
      } catch (err) {
        console.error(err);
      }
    },
    onPublicNotify(notify: any) {
      console.log("Public notify", notify);
    },
  };
  return module;
}

export function createSettingsModule(injector: Injector): SettingsModule {
  const settingsSignal = signal<SettingItem[]>([]);
  const module: SettingsModule = {
    settings: settingsSignal,
    async init() {
      type InitSettingsResponse = { data: { initSettings: SettingItem[] } };
      const res = (await firstValueFrom(
        injector.get(Apollo).query<InitSettingsResponse>({ query: GQL_INIT_SETTINGS })
      )) as unknown as InitSettingsResponse;
      const settings = res.data.initSettings;
      settingsSignal.set(settings);
    },
  };
  return module;
}

export function createUiModule(injector: Injector): UiModule {
  const fullScreenSignal = signal<boolean>(false);
  const isMobile = toSignal(fromEvent(window, "resize").pipe(
    debounceTime(200),
    switchMap(async () => window.innerHeight / window.innerWidth >= 1.5)
  ));
  const module: UiModule = {
    fullScreen: fullScreenSignal,
    isMobile,
    activeRoutedComponent: undefined,
    async init() {
      return;
    },
  };
  return module;
}
