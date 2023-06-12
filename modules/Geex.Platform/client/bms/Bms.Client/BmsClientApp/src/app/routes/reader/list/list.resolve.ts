import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { ReaderListPageParam } from "./list.page";

import { RoutedComponentResolveBase } from "@/app/shared/resolvers/route-component.resolver.base";

@Injectable({
  providedIn: "root",
})
export class ReaderListPageResolve extends RoutedComponentResolveBase<ReaderListPageParam> {
  constructor(injector: Injector) {
    super(injector);
  }
  override normalizeParams(params: ReaderListPageParam): ReaderListPageParam {
    params.pi ??= 1;
    params.ps ??= 10;
    params.filterText ??= "";
    params.sort ??= undefined;
    return params;
  }
}
