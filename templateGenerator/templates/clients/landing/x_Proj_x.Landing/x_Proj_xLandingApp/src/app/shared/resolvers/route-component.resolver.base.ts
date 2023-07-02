import { Injector } from "@angular/core";
import { Resolve, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import * as rison from "rison";
import { Observable } from "rxjs";

export abstract class RoutedComponentResolveBase<TParams> implements Resolve<TParams> {
  protected router: Router;
  /**
   *
   */
  constructor(protected injector: Injector) {
    this.router = injector.get(Router);
  }
  async resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<TParams> {
    var queryParams = { ...route.queryParams };
    let params = {} as TParams;
    // 路由参数转换为组件参数
    Object.entries(queryParams).forEach(([key, value]) => {
      if (typeof value == "string" && value != "") {
        if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/.test(value)) {
          params[key] = Date.parse(value);
          return;
        }
        params[key] = rison.decode(value);
      }
    });
    // let a = rison.decode(queryParams["a"]);
    params = this.normalizeParams(params);
    this.router.virtualNavigate([], { queryParams: params, relativeTo: this.router.routerState.root });
    return params;
  }

  // 路由参数转换为组件参数
  abstract normalizeParams(queryParams: TParams): TParams;
  parseNestedObject(parts: string[], value: any): Record<string, any> {
    return parts.reduceRight((acc, cur) => ({ [cur]: acc }), value);
  }
}
