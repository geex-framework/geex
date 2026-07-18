import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import {
  geex,
  provideGeexApolloTypePolicies,
  provideGeexModuleContribution,
  type GeexTypePolicies,
} from "@geexcode/geex-angular";
import { createAuthModule } from "./auth.module";
import { createIdentityModule } from "./identity.module";
import { createTenantModule } from "./tenant.module";
import type { AuthModule, IdentityModule, IdentityModuleDepsFactory, TenantModule } from "./types";

export interface GeexIdentityOptions {
  readonly createTenantModule?: (injector: Injector) => TenantModule;
  readonly createAuthModule?: (injector: Injector) => AuthModule;
  readonly createIdentityModule?: (
    injector: Injector,
    dependencies: IdentityModuleDepsFactory,
  ) => IdentityModule;
}

export const GEEX_IDENTITY_OPTIONS = new InjectionToken<Readonly<GeexIdentityOptions>>(
  "GEEX_IDENTITY_OPTIONS",
);

type GeexIdentityFieldReadContext = {
  readField(name: string): unknown;
};

export function geexIdentityTypePolicies(): GeexTypePolicies {
  return {
    User: {
      keyFields: ["id"],
      fields: {
        orgs: {
          read(_existing: unknown, { readField }: GeexIdentityFieldReadContext) {
            const orgCodes = readField("orgCodes") as string[] | undefined;
            if (!orgCodes) {
              return null;
            }
            const hit = (geex.identity.orgs() ?? []).find(org => orgCodes.includes(org.code));
            return hit ? { ...hit } : null;
          },
        },
      },
    },
    Org: {
      keyFields: ["code"],
      fields: {
        parentOrg: {
          read(_existing: unknown, { readField }: GeexIdentityFieldReadContext) {
            const parentOrgCode = readField("parentOrgCode");
            if (!parentOrgCode) {
              return null;
            }
            const hit = (geex.identity.orgs() ?? []).find(org => org.code === parentOrgCode);
            return hit ? { ...hit } : null;
          },
        },
      },
    },
  };
}

export function provideGeexIdentity(
  options: Readonly<GeexIdentityOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_IDENTITY_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => {
        const tenant = (options.createTenantModule ?? createTenantModule)(injector);
        const auth = (options.createAuthModule ?? createAuthModule)(injector);
        const identity = (options.createIdentityModule ?? createIdentityModule)(
          injector,
          () => ({ tenant, auth }),
        );
        return { tenant, auth, identity };
      },
    }),
    provideGeexApolloTypePolicies(geexIdentityTypePolicies),
  ]);
}
