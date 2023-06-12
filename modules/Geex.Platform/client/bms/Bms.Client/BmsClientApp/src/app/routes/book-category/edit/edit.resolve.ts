import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { RoutedComponentResolveBase } from "../../../shared/resolvers/route-component.resolver.base";
import { BookCategoryEditPageParams } from "./edit.page";

@Injectable({
  providedIn: "root",
})
export class BookCategoryEditPageResolve extends RoutedComponentResolveBase<BookCategoryEditPageParams> {
  override normalizeParams(queryParams: BookCategoryEditPageParams): BookCategoryEditPageParams {
    let resolvedParams: BookCategoryEditPageParams = {
      id: queryParams.id ?? undefined,
      name: queryParams.name ?? undefined,
    };
    return resolvedParams;
  }
  constructor(injector: Injector) {
    super(injector);
  }
}
