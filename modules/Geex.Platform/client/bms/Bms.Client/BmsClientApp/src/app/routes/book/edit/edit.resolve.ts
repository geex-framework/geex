import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { RoutedComponentResolveBase } from "../../../shared/resolvers/route-component.resolver.base";
import { BookEditPageParams } from "./edit.page";

@Injectable({
  providedIn: "root",
})
export class BookEditPageResolve extends RoutedComponentResolveBase<BookEditPageParams> {
  override normalizeParams(queryParams: BookEditPageParams): BookEditPageParams {
    let resolvedParams: BookEditPageParams = {
      id: queryParams.id ?? undefined,
      name: queryParams.name ?? undefined,
    };
    return resolvedParams;
  }
  constructor(injector: Injector) {
    super(injector);
  }
}
