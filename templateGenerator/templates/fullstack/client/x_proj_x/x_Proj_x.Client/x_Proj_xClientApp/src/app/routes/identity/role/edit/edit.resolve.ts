import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { RoleEditComponentParams } from "./edit.component";

@Injectable({
  providedIn: "root",
})
export class RoleEditComponentResolve implements Resolve<RoleEditComponentParams> {
  constructor(private router: Router) {}
  resolve(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot,
  ): Observable<RoleEditComponentParams> | Promise<RoleEditComponentParams> | RoleEditComponentParams {
    let params = { ...route.params, ...route.queryParams } as { [x: string]: string } as any;
    let resolvedParams: RoleEditComponentParams = {
      id: params.id ?? undefined,
      roleName: params.roleName ?? undefined,
    };
    return resolvedParams;
  }
}
