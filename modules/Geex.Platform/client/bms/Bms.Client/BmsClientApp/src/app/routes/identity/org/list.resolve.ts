import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

@Injectable({
  providedIn: "root",
})
export class OrgListComponentResolve implements Resolve<{}> {
  constructor(private router: Router) {}
  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<{}> | Promise<{}> | {} {
    let params = { ...route.params, ...route.queryParams } as { [x: string]: string } as any;
    let resolvedParams: {} = {
      id: params.id ?? undefined,
      ListName: params.ListName ?? undefined,
    };
    return resolvedParams;
  }
}
