import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { RoutedComponentResolveBase } from "../../../shared/resolvers/route-component.resolver.base";
import { ReaderEditPageParams } from "./edit.page";

@Injectable({
  providedIn: "root",
})
export class ReaderEditPageResolve extends RoutedComponentResolveBase<ReaderEditPageParams> {
  override normalizeParams(queryParams: ReaderEditPageParams): ReaderEditPageParams {
    let resolvedParams: ReaderEditPageParams = {
      id: queryParams.id ?? undefined,
      name: queryParams.name ?? undefined,
    };
    return resolvedParams;
  }
  constructor(injector: Injector) {
    super(injector);
  }
}
