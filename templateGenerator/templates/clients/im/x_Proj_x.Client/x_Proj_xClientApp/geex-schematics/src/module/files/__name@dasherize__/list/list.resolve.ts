import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { <%= classify(name) %>ListPageParam } from "./list.page";

import { RoutedComponentResolveBase } from "@/app/shared/resolvers/route-component.resolver.base";

@Injectable({
  providedIn: "root",
})
export class <%= classify(name) %>ListPageResolve extends RoutedComponentResolveBase<<%= classify(name) %>ListPageParam> {
  constructor(injector: Injector) {
    super(injector);
  }
  override normalizeParams(params: <%= classify(name) %>ListPageParam): <%= classify(name) %>ListPageParam {
    params.pi ??= 1;
    params.ps ??= 10;
    params.filterText ??= "";
    params.sort ??= undefined;
    return params;
  }
}
