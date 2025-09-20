"use strict";
var __create = Object.create;
var __defProp = Object.defineProperty;
var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
var __getOwnPropNames = Object.getOwnPropertyNames;
var __getProtoOf = Object.getPrototypeOf;
var __hasOwnProp = Object.prototype.hasOwnProperty;
var __export = (target, all) => {
  for (var name in all)
    __defProp(target, name, { get: all[name], enumerable: true });
};
var __copyProps = (to, from, except, desc) => {
  if (from && typeof from === "object" || typeof from === "function") {
    for (let key of __getOwnPropNames(from))
      if (!__hasOwnProp.call(to, key) && key !== except)
        __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
  }
  return to;
};
var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(
  // If the importer is in node compatibility mode or this is not an ESM
  // file that has been converted to a CommonJS file using a Babel-
  // compatible transform (i.e. "__esModule" has not been set), then set
  // "default" to the CommonJS "module.exports" for node compatibility.
  isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", { value: mod, enumerable: true }) : target,
  mod
));
var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

// src/index.ts
var index_exports = {};
__export(index_exports, {
  Geex: () => Geex,
  LoginProviderEnum: () => LoginProviderEnum,
  OrgTypeEnum: () => OrgTypeEnum,
  configGeex: () => configGeex,
  createAuthModule: () => createAuthModule,
  createIdentityModule: () => createIdentityModule,
  createMessagingModule: () => createMessagingModule,
  createSettingsModule: () => createSettingsModule,
  createTenantModule: () => createTenantModule,
  createUiModule: () => createUiModule,
  geex: () => geex,
  provideGeex: () => provideGeex
});
module.exports = __toCommonJS(index_exports);

// src/provide-geex.ts
var import_core3 = require("@angular/core");

// src/geex.ts
var import_core2 = require("@angular/core");

// src/modules.ts
var import_apollo_angular = require("apollo-angular");
var import_core = require("@angular/core");
var import_angular_oauth2_oidc = require("angular-oauth2-oidc");
var import_util = require("@delon/util");
var import_graphql_tag = __toESM(require("graphql-tag"));
var import_rxjs = require("rxjs");
var import_operators = require("rxjs/operators");
var import_rxjs_interop = require("@angular/core/rxjs-interop");
var LoginProviderEnum = {
  Local: "Local"
};
var OrgTypeEnum = {
  Default: "Default"
};
var GQL_CHECK_TENANT = import_graphql_tag.default`mutation checkTenant($code: String!) { checkTenant(code: $code) { id code name isEnabled createdOn } }`;
var GQL_FEDERATE_AUTH = import_graphql_tag.default`mutation federateAuthenticate(
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
var GQL_ON_PUBLIC_NOTIFY = import_graphql_tag.default`subscription onPublicNotify { onPublicNotify { __typename ... on DataChangeClientNotify { dataChangeType } } }`;
var GQL_ORGS_CACHE = import_graphql_tag.default`query orgsCache { orgs(take: 999) { items { id orgType code name parentOrgCode } } }`;
var GQL_INIT_SETTINGS = import_graphql_tag.default`query initSettings { initSettings { id name value } }`;
function createTenantModule(injector) {
  const current = (0, import_core.signal)(null);
  const module2 = {
    init: async () => {
      try {
        const tenantCode = injector.get(import_util.CookieService).get("__tenant");
        if (tenantCode) {
          const tenantData = await module2.loadTenantData(tenantCode);
          current.set(tenantData ?? null);
        }
      } catch (err) {
        console.error(err);
      }
    },
    current,
    async loadTenantData(code) {
      const res = await (0, import_rxjs.firstValueFrom)(
        injector.get(import_apollo_angular.Apollo).mutate({ mutation: GQL_CHECK_TENANT, variables: { code } })
      );
      return res.data.checkTenant;
    },
    switchTenant(targetTenantCode) {
      const domainParts = location.hostname.split(".");
      if (domainParts.length > 2) {
        domainParts.shift();
      }
      const rootDomain = domainParts.join(".");
      injector.get(import_util.CookieService).put("__tenant", targetTenantCode, {
        secure: false,
        SameSite: "lax",
        HttpOnly: false,
        domain: rootDomain
      });
    }
  };
  return module2;
}
function createAuthModule(injector) {
  const user = (0, import_core.signal)(null);
  const module2 = {
    init: async () => {
      try {
        let userData = await module2.loadUserData();
        user.set(userData ?? null);
      } catch (err) {
        console.error(err);
      }
    },
    user,
    async loadUserData() {
      const oAuthService = injector.get(import_angular_oauth2_oidc.OAuthService);
      try {
        await oAuthService.loadDiscoveryDocument();
        if (!oAuthService.hasValidAccessToken()) {
          return void 0;
        }
        await oAuthService.loadUserProfile();
        const profile = oAuthService.getIdentityClaims();
        if (profile) {
          const result = await (0, import_rxjs.firstValueFrom)(injector.get(import_apollo_angular.Apollo).mutate({
            mutation: GQL_FEDERATE_AUTH,
            variables: {
              code: oAuthService.getAccessToken(),
              loginProvider: profile.login_provider
            }
          }).pipe((0, import_rxjs.map)((res) => res?.data?.federateAuthenticate.user)));
          return result;
        }
      } catch (err) {
        console.error(err);
      }
      return void 0;
    }
  };
  return module2;
}
function createIdentityModule(injector) {
  const orgsSignal = (0, import_core.signal)([]);
  const userOwnedOrgsSignal = (0, import_core.signal)([]);
  return {
    orgs: orgsSignal,
    userOwnedOrgs: userOwnedOrgsSignal,
    init() {
      const orgs$ = injector.get(import_apollo_angular.Apollo).watchQuery({ query: GQL_ORGS_CACHE }).valueChanges.pipe((0, import_rxjs.map)((res) => (0, import_util.deepCopy)(res.data.orgs.items)));
      orgs$.subscribe((orgs) => {
        (0, import_core.runInInjectionContext)(injector, () => {
          orgsSignal.set(orgs);
          const userData = geex.auth.user();
          let allOwned = [];
          if (orgs?.length && userData) {
            if (userData.id === "000000000000000000000001") {
              allOwned = (0, import_util.deepCopy)(orgs);
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
      const subClient = injector.get(import_apollo_angular.Apollo).use("subscription");
      subClient.subscribe({ query: GQL_ON_PUBLIC_NOTIFY }).pipe((0, import_rxjs.map)((res) => res?.data?.onPublicNotify)).subscribe((notify) => {
        console.log("Public notify", notify);
      });
    }
  };
}
function createSettingsModule(injector) {
  const settingsSignal = (0, import_core.signal)([]);
  return {
    settings: settingsSignal,
    async init() {
      const res = await (0, import_rxjs.firstValueFrom)(
        injector.get(import_apollo_angular.Apollo).query({ query: GQL_INIT_SETTINGS })
      );
      const settings = res.data.initSettings;
      settingsSignal.set(settings);
    }
  };
}
function createUiModule(injector) {
  const fullScreenSignal = (0, import_core.signal)(false);
  const isMobile = (0, import_rxjs_interop.toSignal)((0, import_rxjs.fromEvent)(window, "resize").pipe(
    (0, import_operators.debounceTime)(200),
    (0, import_operators.switchMap)(async () => window.innerHeight / window.innerWidth >= 1.5)
  ));
  return {
    fullScreen: fullScreenSignal,
    isMobile,
    activeRoutedComponent: void 0
  };
}

// src/geex.ts
var geex;
var Geex = new import_core2.InjectionToken("Geex");
function configGeex(injector, overrides) {
  (0, import_core2.runInInjectionContext)(injector, () => {
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
      deps: [import_core3.Injector]
    }
  ];
}
// Annotate the CommonJS export names for ESM import in node:
0 && (module.exports = {
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
});
