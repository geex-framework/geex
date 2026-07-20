import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import {
  GEEX_SUPER_ADMIN_USER_ID,
  geex,
  provideGeexApolloTypePolicies,
  provideGeexModuleContribution,
  type GeexTypePolicies,
} from "@geexcode/geex-angular";
import { createIdentityModule } from "./identity.module";
import {
  GEEX_DEFAULT_SUPER_ADMIN_USER_ID,
  type IdentityModule,
  type IdentityModuleDeps,
  type IdentityModuleDepsFactory,
} from "./types";

export interface GeexIdentityOptions {
  readonly createIdentityModule?: (
    injector: Injector,
    dependencies: IdentityModuleDepsFactory,
  ) => IdentityModule;
  readonly superAdminUserId?: string;
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
    {
      provide: GEEX_SUPER_ADMIN_USER_ID,
      useValue: options.superAdminUserId ?? GEEX_DEFAULT_SUPER_ADMIN_USER_ID,
    },
    provideGeexModuleContribution({
      createModules: ({ injector, modules }) => {
        const multiTenant = modules["multiTenant"];
        const authentication = modules["authentication"];
        if (!multiTenant || !authentication) {
          throw new Error(
            "provideGeexMultiTenant() and provideGeexAuthentication() must be registered before provideGeexIdentity()",
          );
        }
        const identity = (options.createIdentityModule ?? createIdentityModule)(
          injector,
          () => ({ multiTenant, authentication } as unknown as IdentityModuleDeps),
        );
        return { identity };
      },
    }),
    provideGeexApolloTypePolicies(geexIdentityTypePolicies),
  ]);
}
