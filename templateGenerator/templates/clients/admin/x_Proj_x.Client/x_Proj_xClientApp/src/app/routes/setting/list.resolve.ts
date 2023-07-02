import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { SettingListParams } from "./list.component";

@Injectable({
  providedIn: "root",
})
export class SettingListResolve implements Resolve<SettingListParams> {
  key = "role";
  constructor(private router: Router) {}
  resolve(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot,
  ): Observable<SettingListParams> | Promise<SettingListParams> | SettingListParams {
    let params = { ...route.params, ...route.queryParams } as { [x: string]: string } as any;
    let resolvedParams: SettingListParams = {
      pi: params.pi ?? 1,
      ps: params.ps ?? 10,
      name: params.name ?? null,
    };
    return resolvedParams;
  }
}
