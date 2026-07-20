import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { ACLService } from "@delon/acl";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { AuthorizeGuard } from "./authorize.guard";
import { createAuthorizationModule } from "./authorization.module";
import type { AuthorizationModule } from "./authorization.types";
import { LocalStorageACLService } from "./local-storage-acl.service";

export interface GeexAuthorizationOptions {
  readonly createAuthorizationModule?: (injector: Injector) => AuthorizationModule;
  readonly createAclService?: (injector: Injector) => ACLService;
}

export const GEEX_AUTHORIZATION_OPTIONS = new InjectionToken<Readonly<GeexAuthorizationOptions>>(
  "GEEX_AUTHORIZATION_OPTIONS",
);

export function provideGeexAuthorization(
  options: Readonly<GeexAuthorizationOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_AUTHORIZATION_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ modules, injector }) => {
        const multiTenant = modules["multiTenant"];
        const authentication = modules["authentication"];
        if (!multiTenant || !authentication) {
          throw new Error(
            "provideGeexAuthorization() requires provideGeexMultiTenant() and provideGeexAuthentication() to be registered first",
          );
        }
        return {
          authorization: (options.createAuthorizationModule ?? createAuthorizationModule)(injector),
        };
      },
    }),
    AuthorizeGuard,
    {
      provide: ACLService,
      useFactory: (injector: Injector) =>
        options.createAclService?.(injector) ?? LocalStorageACLService.new(injector),
      deps: [Injector],
    },
  ]);
}
