import { Injectable, Injector } from "@angular/core";
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { merge } from "lodash";
import { Observable } from "rxjs";

import { RoutedComponentResolveBase } from "../../../shared/resolvers/route-component.resolver.base";
import { BorrowViewPageParams } from "./view.page";

@Injectable({
  providedIn: "root",
})
export class BorrowViewPageResolve extends RoutedComponentResolveBase<BorrowViewPageParams> {
  override normalizeParams(queryParams: BorrowViewPageParams): BorrowViewPageParams {
    let resolvedParams: BorrowViewPageParams = {
      bookISBN: queryParams.bookISBN ?? undefined,
      userPhone: queryParams.userPhone ?? undefined,
    };
    return resolvedParams;
  }
  constructor(injector: Injector) {
    super(injector);
  }
}
