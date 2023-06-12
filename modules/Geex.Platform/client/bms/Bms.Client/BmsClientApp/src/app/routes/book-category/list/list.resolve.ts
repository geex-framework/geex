import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { BookCategoryListPageParam } from "./list.page";

import { RoutedComponentResolveBase } from "@/app/shared/resolvers/route-component.resolver.base";

@Injectable({
  providedIn: "root",
})
export class BookCategoryListPageResolve extends RoutedComponentResolveBase<BookCategoryListPageParam> {
  constructor(injector: Injector) {
    super(injector);
  }
  override normalizeParams(params: BookCategoryListPageParam): BookCategoryListPageParam {
    params.pi ??= 1;
    params.ps ??= 10;
    params.filterText ??= "";
    params.sort ??= undefined;
    return params;
  }
}
