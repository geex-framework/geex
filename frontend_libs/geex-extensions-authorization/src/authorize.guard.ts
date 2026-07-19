import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from "@angular/router";
import { geex } from "@geexcode/geex-angular";

@Injectable()
export class AuthorizeGuard {
  async canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean | UrlTree> {
    return geex.authorization.canActivate(route, state);
  }

  async checkTenant(pathTenant: string) {
    return geex.authorization.checkTenant(pathTenant);
  }
}
