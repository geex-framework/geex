import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { UserParams } from "./list.component";

@Injectable({
  providedIn: "root",
})
export class UserResolveGuard implements Resolve<UserParams> {
  key = "role";
  constructor(private router: Router) {}
  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<UserParams> | Promise<UserParams> | UserParams {
    let params = { ...route.params, ...route.queryParams } as { [x: string]: string } as any;
    let resolvedParams: UserParams = {
      pi: params.pi ?? 1,
      ps: params.ps ?? 10,
      username: params.username ?? null,
    };
    return resolvedParams;
  }
}
