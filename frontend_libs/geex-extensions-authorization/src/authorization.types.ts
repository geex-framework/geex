import type { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from "@angular/router";
import type { GeexModule } from "@geexcode/geex-angular";

export interface AuthorizationAclData {
  roles: string[];
  abilities: Array<string | number>;
  full: boolean;
}

export interface AuthorizationModule extends GeexModule<{
  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean | UrlTree>;
  checkTenant(pathTenant: string): Promise<unknown>;
  syncAclFromAuth(): Promise<AuthorizationAclData | null>;
  loadAcl(): AuthorizationAclData;
  persistAcl(data: AuthorizationAclData): void;
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    authorization: AuthorizationModule;
  }
}
