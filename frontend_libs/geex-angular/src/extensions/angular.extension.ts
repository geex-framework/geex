import { signal, Signal, WritableSignal } from "@angular/core";

import {
  AbstractControl,
  AbstractControlOptions,
  FormBuilder,
  FormControl,
  FormGroup,
  FormArray,
  TypedFormGroup,
  ValueOrTypedAbstractControl,
} from "@angular/forms";
import {
  ActivatedRouteSnapshot,
  GeexRoute,
  NavigationEnd,
  Navigation,
  Route,
  Router,
  Data,
  UrlCreationOptions,
  NavigationBehaviorOptions,
} from "@angular/router";
import * as _ from "lodash-es";
import { Observable } from "rxjs";

function getResolvedUrl(this: ActivatedRouteSnapshot): string {
  return (this as any)["_routerState"].url;
}

function getConfiguredUrl(this: ActivatedRouteSnapshot): string {
  return `/${this.pathFromRoot
    .filter(v => v.routeConfig)
    .map(v => v.routeConfig!.path)
    .where(x => x != "")
    .join("/")}`;
}

function getDeepestRouteConfig(this: ActivatedRouteSnapshot): GeexRoute | null {
  let currentRoute = this;

  while (currentRoute.firstChild) {
    currentRoute = currentRoute.firstChild;
  }

  return currentRoute.routeConfig;
}

ActivatedRouteSnapshot.prototype.getResolvedUrl = getResolvedUrl;
ActivatedRouteSnapshot.prototype.getConfiguredUrl = getConfiguredUrl;
ActivatedRouteSnapshot.prototype.getDeepestRouteConfig = getDeepestRouteConfig;

declare module "@angular/router" {
  interface ActivatedRouteSnapshot {
    getResolvedUrl(): string;
    getConfiguredUrl(): string;
    getDeepestRouteConfig(): GeexRoute | null;
  }
  interface RouteData extends Data {
    singleton?: boolean;
    reuse?: boolean;
    title?: string;
  }

  interface NavigationBehaviorOptions {
    forceReload?: boolean;
  }

  type GeexRoutes = GeexRoute[];
  interface GeexRoute extends Route {
    data?: RouteData;
  }

  interface Router {
    navigationReload: WritableSignal<NavigationEnd | undefined>;
  }
}

Object.defineProperty(Router.prototype, "navigationReload", {
  value: signal<NavigationEnd | undefined>(undefined as unknown as NavigationEnd),
  writable: false,
});

declare module "@angular/forms" {
  type IsArray<TValue> = TValue extends Array<any> ? true : false;

  type IsObject<TValue> = TValue extends object ? (TValue extends Array<any> ? false : TValue extends Date ? false : true) : false;

  export type TypedAbstractControl<TValue> =
    IsArray<TValue> extends true
      ? TypedFormArray<TValue extends Array<infer U> ? U : never> & FormControl<TValue>
      : IsObject<TValue> extends true
        ? TypedFormGroup<TValue> & FormControl<TValue>
        : FormControl<TValue>;

  export interface TypedFormGroup<TValue> extends FormGroup {
    controls: {
      [K in keyof TValue]: TypedAbstractControl<TValue[K]>;
    };
    value: TValue;
  }

  export interface TypedFormArray<TValue> extends FormArray {
    controls: Array<TypedAbstractControl<TValue>>;
    value: Array<TValue>;
  }

  export type ValueOrTypedAbstractControl<T> = {
    [key in keyof T]: FormControl<T[key]> | ValueOrTypedAbstractControl<T[key]>;
  };
  export interface FormBuilder {
    build<T>(controls: ValueOrTypedAbstractControl<T>, options?: AbstractControlOptions | null): TypedFormGroup<T>;
  }
}

function transformObjectToForm(fb: FormBuilder, obj: any, options: AbstractControlOptions): any {
  if (obj instanceof Object && !(obj instanceof Date)) {
    const result: any = {};
    for (const key in obj) {
      if (obj.hasOwnProperty(key)) {
        if (obj[key] instanceof AbstractControl) {
          result[key] = obj[key];
          continue;
        }
        if (obj[key] instanceof Object && !(obj[key] instanceof Array)) {
          result[key] = transformObjectToForm(fb, obj[key], options);
        } else if (obj[key] instanceof Array) {
          result[key] = fb.array(
            obj[key].map(item => transformObjectToForm(fb, item, options)),
            options,
          );
        } else {
          result[key] = new FormControl(obj[key], options);
        }
      }
    }
    return fb.group(result, options);
  } else {
    return obj;
  }
}

FormBuilder.prototype.build = function <T>(
  this: FormBuilder,
  controls: ValueOrTypedAbstractControl<T>,
  options?: AbstractControlOptions | null,
): TypedFormGroup<T> {
  let updated = transformObjectToForm(this, controls, options ?? {});
  return updated as TypedFormGroup<T>;
};
