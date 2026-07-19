import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import {
  geex,
  provideGeexApolloTypePolicies,
  provideGeexModuleContribution,
  type GeexTypePolicies,
} from "@geexcode/geex-angular";
import { createIdentityModule } from "./identity.module";
import type { IdentityModule, IdentityModuleDeps, IdentityModuleDepsFactory } from "./types";

export interface GeexIdentityOptions {
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
      createModules: ({ injector, modules }) => {
        const tenant = modules["tenant"];
        const auth = modules["auth"];
        if (!tenant || !auth) {
          throw new Error(
            "provideGeexMultiTenant() and provideGeexAuthentication() must be registered before provideGeexIdentity()",
          );
        }
        const identity = (options.createIdentityModule ?? createIdentityModule)(
          injector,
          () => ({ tenant, auth } as unknown as IdentityModuleDeps),
        );
        return { identity };
      },
    }),
    provideGeexApolloTypePolicies(geexIdentityTypePolicies),
  ]);
}
