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
  init: (force?: boolean) => Promise<{ [K in keyof GeexModules<TExtensionModules>]: any }>;
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
   * @param force - If true, forces re-initialization. Defaults to false.
   * When force is false, multiple calls will share the same initialization Promise.
   */
  init: (force?: boolean) => Promise<any>;
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

// ----- Helper function to guard signal access -----

/**
 * Wraps a signal to throw an error if accessed before module initialization.
 * Supports both Signal and WritableSignal.
 */
function guardedSignal<T>(
  innerSignal: WritableSignal<T>,
  isInitialized: () => boolean
): WritableSignal<T>;
function guardedSignal<T>(
  innerSignal: Signal<T>,
  isInitialized: () => boolean,
): Signal<T>;
function guardedSignal<T>(
  innerSignal: Signal<T> | WritableSignal<T>,
  isInitialized: () => boolean
): Signal<T> | WritableSignal<T> {
  const guard = (() => {
    if (!isInitialized()) {
      throw new Error(`GuardedSignal not initialized. 
        isInitialized: ${isInitialized.toString()}
        innerSignal: ${innerSignal.toString()}
        `);
    }
    return innerSignal();
  }) as any;

  // Preserve WritableSignal methods if present
  if ('set' in innerSignal) {
    guard.set = innerSignal.set.bind(innerSignal);
    guard.update = innerSignal.update.bind(innerSignal);
  }
  if ('asReadonly' in innerSignal) {
    guard.asReadonly = innerSignal.asReadonly.bind(innerSignal);
  }

  return guard;
}

// ----- default implementations -----

export function createTenantModule(injector: Injector): TenantModule {
  const _current = signal<Tenant | null>(null);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            const tenantCode = injector.get(CookieService).get("__tenant");
            if (tenantCode) {
              const tenantData = await module.loadTenantData(tenantCode);
              _current.set(tenantData ?? null);
            }
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
    current: guardedSignal(_current, () => _initialized),
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
  };
  return module as TenantModule;
};

export function createAuthModule(injector: Injector): AuthModule {
  const _user = signal<User | null>(null);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            const userData = await module.loadUserData();
            _user.set(userData ?? null);
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
    user: guardedSignal(_user, () => _initialized),
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
  };
  return module as AuthModule;
};

export function createIdentityModule(injector: Injector): IdentityModule {
  const _orgsSignal = signal<Org[]>([]);
  const _userOwnedOrgsSignal = signal<Org[]>([]);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    orgs: guardedSignal(_orgsSignal, () => _initialized),
    userOwnedOrgs: guardedSignal(_userOwnedOrgsSignal, () => _initialized),
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            await geex.tenant.init();
            await geex.auth.init();
            // 创建一个 Promise 用于等待第一次订阅回调完成
            await new Promise<void>((resolve, reject) => {
              const orgs$ = injector.get(Apollo)
                .watchQuery<{ orgs: { items: Org[] } }>({ query: GQL_ORGS_CACHE })
                .valueChanges.pipe(map(res => deepCopy(res.data.orgs.items) as Org[]));
              
              orgs$.subscribe({
                next: (orgs: Org[]) => {
                  runInInjectionContext(injector, () => {
                    _orgsSignal.set(orgs);
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
                    _userOwnedOrgsSignal.set(allOwned);
                    resolve();
                  });
                },
                error: (err) => {
                  console.error(err);
                  reject(err);
                }
              });
            });
            _initialized = true;
          } catch (error) {
            console.error(error);
          }
        })();
      }
      return _initPromise;
    },
  };
  return module as IdentityModule;
}

export function createMessagingModule(injector: Injector): MessagingModule {
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
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
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
    onPublicNotify(notify: any) {
      console.log("Public notify", notify);
    },
  };
  return module as MessagingModule;
}

export function createSettingsModule(injector: Injector): SettingsModule {
  const _settingsSignal = signal<SettingItem[]>([]);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    settings: guardedSignal(_settingsSignal, () => _initialized),
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            type InitSettingsResponse = { data: { initSettings: SettingItem[] } };
            const res = (await firstValueFrom(
              injector.get(Apollo).query<InitSettingsResponse>({ query: GQL_INIT_SETTINGS })
            )) as unknown as InitSettingsResponse;
            const settings = res.data.initSettings;
            _settingsSignal.set(settings);
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
  };
  return module as SettingsModule;
}

export function createUiModule(injector: Injector): UiModule {
  const _fullScreenSignal = signal<boolean>(false);
  const _isMobile = toSignal(fromEvent(window, "resize").pipe(
    debounceTime(200),
    switchMap(async () => window.innerHeight / window.innerWidth >= 1.5)
  ));
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    fullScreen: guardedSignal(_fullScreenSignal, () => _initialized),
    isMobile: guardedSignal(_isMobile, () => _initialized),
    activeRoutedComponent: undefined,
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          _initialized = true;
        })();
      }
      return _initPromise;
    },
  };
  return module as UiModule;
}
