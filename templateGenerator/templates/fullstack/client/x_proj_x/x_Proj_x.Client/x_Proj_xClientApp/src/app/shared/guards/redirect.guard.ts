import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree } from "@angular/router";
import { Observable } from "rxjs";
@Injectable({ providedIn: "root" })
export class CurrentPlatformRedirectGuard implements CanActivate {
  constructor(private router: Router) {}
  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot,
  ): boolean | UrlTree | Observable<boolean | UrlTree> | Promise<boolean | UrlTree> {
    let pathname = state.url.split("?")[0];
    if (window.isMobile) {
      if (!pathname.endsWith("/query")) {
        this.router.navigate([`${pathname}/query`]);
      }
    } else {
      if (pathname.endsWith("/query")) {
        this.router.navigate([`${pathname.replace("/query", "")}`]);
      }
    }

    return true;
  }
}
