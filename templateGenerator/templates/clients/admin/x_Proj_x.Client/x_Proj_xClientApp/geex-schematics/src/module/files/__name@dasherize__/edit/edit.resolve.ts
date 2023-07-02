import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { RoutedComponentResolveBase } from "../../../shared/resolvers/route-component.resolver.base";
import { <%= classify(name) %>EditPageParams } from "./edit.page";

@Injectable({
  providedIn: "root",
})
export class <%= classify(name) %>EditPageResolve extends RoutedComponentResolveBase<<%= classify(name) %>EditPageParams> {
  override normalizeParams(queryParams: <%= classify(name) %>EditPageParams): <%= classify(name) %>EditPageParams {
    let resolvedParams: <%= classify(name) %>EditPageParams = {
      id: queryParams.id ?? undefined,
      name: queryParams.name ?? undefined,
    };
    return resolvedParams;
  }
  constructor(injector: Injector) {
    super(injector);
  }
}
