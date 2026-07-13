import "./chunk-EBO3CZXG.mjs";

// src/provide-geex.ts
import { Injector as Injector3 } from "@angular/core";

// src/geex.ts
import { InjectionToken, runInInjectionContext as runInInjectionContext2 } from "@angular/core";

// src/modules.ts
import { Apollo } from "apollo-angular";
import { runInInjectionContext, signal } from "@angular/core";
import { OAuthService } from "angular-oauth2-oidc";
import { CookieService, deepCopy } from "@delon/util";
import gql from "graphql-tag";
import { firstValueFrom, map, fromEvent } from "rxjs";
import { debounceTime, switchMap } from "rxjs/operators";
import { toSignal } from "@angular/core/rxjs-interop";
var LoginProviderEnum = {
  Local: "Local"
};
var OrgTypeEnum = {
  Default: "Default"
};
var ExtensionModule = {};
var GQL_CHECK_TENANT = gql`mutation checkTenant($code: String!) { checkTenant(code: $code) { id code name isEnabled createdOn } }`;
var GQL_FEDERATE_AUTH = gql`mutation federateAuthenticate(
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
var GQL_ON_PUBLIC_NOTIFY = gql`subscription onPublicNotify { onPublicNotify { __typename ... on DataChangeClientNotify { dataChangeType } } }`;
var GQL_ORGS_CACHE = gql`query orgsCache { orgs(take: 999) { items { id orgType code name parentOrgCode } } }`;
var GQL_INIT_SETTINGS = gql`query initSettings { initSettings { id name value } }`;
function guardedSignal(innerSignal, isInitialized) {
  const guard = (() => {
    if (!isInitialized()) {
      throw new Error(`GuardedSignal not initialized. 
        isInitialized: ${isInitialized.toString()}
        innerSignal: ${innerSignal.toString()}
        `);
    }
    return innerSignal();
  });
  if ("set" in innerSignal) {
    guard.set = innerSignal.set.bind(innerSignal);
    guard.update = innerSignal.update.bind(innerSignal);
  }
  if ("asReadonly" in innerSignal) {
    guard.asReadonly = innerSignal.asReadonly.bind(innerSignal);
  }
  return guard;
}
function createTenantModule(injector) {
  const _current = signal(null);
  let _initialized = false;
  let _initPromise = null;
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
    async loadTenantData(code) {
      const res = await firstValueFrom(
        injector.get(Apollo).mutate({ mutation: GQL_CHECK_TENANT, variables: { code } })
      );
      return res.data.checkTenant;
    },
    switchTenant(targetTenantCode) {
      const domainParts = location.hostname.split(".");
      if (domainParts.length > 2) {
        domainParts.shift();
      }
      const rootDomain = domainParts.join(".");
      injector.get(CookieService).put("__tenant", targetTenantCode, {
        secure: false,
        SameSite: "lax",
        HttpOnly: false,
        domain: rootDomain
      });
    }
  };
  return module;
}
function createAuthModule(injector) {
  const _user = signal(null);
  let _initialized = false;
  let _initPromise = null;
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
    async loadUserData() {
      const oAuthService = injector.get(OAuthService);
      try {
        await oAuthService.loadDiscoveryDocument();
        if (!oAuthService.hasValidAccessToken()) {
          return void 0;
        }
        await oAuthService.loadUserProfile();
        const profile = oAuthService.getIdentityClaims();
        if (profile) {
          const result = await firstValueFrom(injector.get(Apollo).mutate({
            mutation: GQL_FEDERATE_AUTH,
            variables: {
              code: oAuthService.getAccessToken(),
              loginProvider: profile.login_provider
            }
          }).pipe(map((res) => res?.data?.federateAuthenticate.user)));
          return result;
        }
      } catch (err) {
        console.error(err);
      }
      return void 0;
    }
  };
  return module;
}
function createIdentityModule(injector) {
  const _orgsSignal = signal([]);
  const _userOwnedOrgsSignal = signal([]);
  let _initialized = false;
  let _initPromise = null;
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
            await new Promise((resolve, reject) => {
              const orgs$ = injector.get(Apollo).watchQuery({ query: GQL_ORGS_CACHE }).valueChanges.pipe(map((res) => deepCopy(res.data.orgs.items)));
              orgs$.subscribe({
                next: (orgs) => {
                  runInInjectionContext(injector, () => {
                    _orgsSignal.set(orgs);
                    const userData = geex.auth.user();
                    let allOwned = [];
                    if (orgs?.length && userData) {
                      if (userData.id === "000000000000000000000001") {
                        allOwned = deepCopy(orgs);
                      } else {
                        const ownedCodes = userData.orgs.map((x) => x.code);
                        allOwned = orgs.filter((o) => ownedCodes.some((code) => o.code.startsWith(code)));
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
    }
  };
  return module;
}
function createMessagingModule(injector) {
  let _initialized = false;
  let _initPromise = null;
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
              subClient.subscribe({ query: GQL_ON_PUBLIC_NOTIFY }).pipe(map((res) => res?.data?.onPublicNotify)).subscribe((notify) => {
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
    onPublicNotify(notify) {
      console.log("Public notify", notify);
    }
  };
  return module;
}
function createSettingsModule(injector) {
  const _settingsSignal = signal([]);
  let _initialized = false;
  let _initPromise = null;
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
            const res = await firstValueFrom(
              injector.get(Apollo).query({ query: GQL_INIT_SETTINGS })
            );
            const settings = res.data.initSettings;
            _settingsSignal.set(settings);
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    }
  };
  return module;
}
function createUiModule(injector) {
  const _fullScreenSignal = signal(false);
  const _isMobile = toSignal(fromEvent(window, "resize").pipe(
    debounceTime(200),
    switchMap(async () => window.innerHeight / window.innerWidth >= 1.5)
  ));
  let _initialized = false;
  let _initPromise = null;
  const module = {
    fullScreen: guardedSignal(_fullScreenSignal, () => _initialized),
    isMobile: guardedSignal(_isMobile, () => _initialized),
    activeRoutedComponent: void 0,
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
    }
  };
  return module;
}

// src/geex.ts
var geex;
var Geex = new InjectionToken("Geex");
function configGeex(injector, overrides = {}) {
  runInInjectionContext2(injector, () => {
    const modules = {
      ...overrides
    };
    let _initPromise = null;
    modules.init ?? (modules.init = (force = false) => {
      if (force) {
        _initPromise = null;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          const entries = Object.entries(modules).filter(([key]) => key !== "init");
          return Object.fromEntries(await Promise.all(
            entries.map(async ([key, mod]) => {
              const maybeInit = mod.init;
              try {
                return [key, await maybeInit(force)];
              } catch (err) {
                console.error(err);
                return [key, null];
              }
            })
          ));
        })();
      }
      return _initPromise;
    });
    modules.tenant ?? (modules.tenant = createTenantModule(injector));
    modules.auth ?? (modules.auth = createAuthModule(injector));
    modules.identity ?? (modules.identity = createIdentityModule(injector));
    modules.messaging ?? (modules.messaging = createMessagingModule(injector));
    modules.settings ?? (modules.settings = createSettingsModule(injector));
    modules.ui ?? (modules.ui = createUiModule(injector));
    geex = modules;
  });
}

// src/provide-geex.ts
function provideGeex(overrides = {}, extensions = {}) {
  return [
    {
      provide: Geex,
      useFactory: (injector) => {
        const mergedModules = {
          ...extensions,
          ...overrides
        };
        configGeex(injector, mergedModules);
        return geex;
      },
      deps: [Injector3]
    }
  ];
}
export {
  ExtensionModule,
  Geex,
  LoginProviderEnum,
  OrgTypeEnum,
  configGeex,
  createAuthModule,
  createIdentityModule,
  createMessagingModule,
  createSettingsModule,
  createTenantModule,
  createUiModule,
  geex,
  provideGeex
};
