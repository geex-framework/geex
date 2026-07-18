import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { ACLService } from "@delon/acl";
import { AuthorizeGuard } from "./authorize.guard";
import { LocalStorageACLService } from "./local-storage-acl.service";

export interface GeexAuthorizationOptions {
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
    AuthorizeGuard,
    {
      provide: ACLService,
      useFactory: (injector: Injector) =>
        options.createAclService?.(injector) ?? LocalStorageACLService.new(injector),
      deps: [Injector],
    },
  ]);
}
