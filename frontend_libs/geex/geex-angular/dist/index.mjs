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
function createTenantModule(injector) {
  const current = signal(null);
  const module = {
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
  const user = signal(null);
  const module = {
    init: async () => {
      try {
        let userData = await module.loadUserData();
        user.set(userData ?? null);
      } catch (err) {
        console.error(err);
      }
    },
    user,
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
  const orgsSignal = signal([]);
  const userOwnedOrgsSignal = signal([]);
  return {
    orgs: orgsSignal,
    userOwnedOrgs: userOwnedOrgsSignal,
    init() {
      const orgs$ = injector.get(Apollo).watchQuery({ query: GQL_ORGS_CACHE }).valueChanges.pipe(map((res) => deepCopy(res.data.orgs.items)));
      orgs$.subscribe((orgs) => {
        runInInjectionContext(injector, () => {
          orgsSignal.set(orgs);
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
          userOwnedOrgsSignal.set(allOwned);
        });
      });
    }
  };
}
function createMessagingModule(injector) {
  return {
    init: async () => {
      return;
    },
    onPublicNotify() {
      const subClient = injector.get(Apollo).use("subscription");
      subClient.subscribe({ query: GQL_ON_PUBLIC_NOTIFY }).pipe(map((res) => res?.data?.onPublicNotify)).subscribe((notify) => {
        console.log("Public notify", notify);
      });
    }
  };
}
function createSettingsModule(injector) {
  const settingsSignal = signal([]);
  return {
    settings: settingsSignal,
    async init() {
      const res = await firstValueFrom(
        injector.get(Apollo).query({ query: GQL_INIT_SETTINGS })
      );
      const settings = res.data.initSettings;
      settingsSignal.set(settings);
    }
  };
}
function createUiModule(injector) {
  const fullScreenSignal = signal(false);
  const isMobile = toSignal(fromEvent(window, "resize").pipe(
    debounceTime(200),
    switchMap(async () => window.innerHeight / window.innerWidth >= 1.5)
  ));
  return {
    fullScreen: fullScreenSignal,
    isMobile,
    activeRoutedComponent: void 0
  };
}

// src/geex.ts
var geex;
var Geex = new InjectionToken("Geex");
function configGeex(injector, overrides) {
  runInInjectionContext2(injector, () => {
    const modules = {
      init: async () => {
        const entries = Object.entries(modules).filter(([key]) => key !== "init");
        await Promise.all(
          entries.map(async ([, mod]) => {
            const maybeInit = mod.init;
            if (typeof maybeInit === "function") {
              try {
                await maybeInit();
              } catch (err) {
                console.error(err);
              }
            }
          })
        );
      },
      tenant: createTenantModule(injector),
      auth: createAuthModule(injector),
      identity: createIdentityModule(injector),
      messaging: createMessagingModule(injector),
      settings: createSettingsModule(injector),
      ui: createUiModule(injector)
    };
    if (overrides) {
      Object.assign(modules, overrides);
    }
    geex = modules;
  });
}

// src/provide-geex.ts
function provideGeex(overrides) {
  return [
    {
      provide: Geex,
      useFactory: (injector) => {
        configGeex(injector, overrides);
        return geex;
      },
      deps: [Injector3]
    }
  ];
}
export {
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
