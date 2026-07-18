import { Location } from "@angular/common";
import { ChangeDetectorRef, Injectable, WritableSignal, effect, inject, signal } from "@angular/core";
import { FormBuilder, FormControl } from "@angular/forms";
import { ActivatedRoute, Params } from "@angular/router";
import { ReuseTabService } from "@delon/abc/reuse-tab";
import { LoadingService } from "@delon/abc/loading";
import { TitleService } from "@delon/theme";
import { match } from "ts-pattern";
import rison from "../../rison";

import { BusinessComponentBase } from "./business.component.base";
import { GeexTypedFormGroup } from "../types";

function geexIsEqual(a: unknown, b: unknown): boolean {
  if (Object.is(a, b)) {
    return true;
  }
  try {
    return JSON.stringify(a) === JSON.stringify(b);
  } catch {
    return false;
  }
}

type ParamMappingValue<TValue> = {
  position: "pathParams" | "queryParams" | "fragment";
  default?: TValue;
};

export type RouteParamsMappings<TParams extends {}> = {
  [Key in keyof TParams]: ParamMappingValue<TParams[Key]>;
};

export type RouteParams = {
  pathParams?: Params;
  queryParams?: Params;
  fragment?: string;
};

@Injectable()
export abstract class RoutedComponent<TParams extends {}> extends BusinessComponentBase<TParams> {
  abstract routeParamsMappings: RouteParamsMappings<TParams>;
  defaultParams!: TParams;
  paramsForm!: GeexTypedFormGroup<TParams>;
  declare params: WritableSignal<TParams>;
  title = signal<string | undefined>(undefined);
  protected cdr = inject(ChangeDetectorRef);
  protected fb = inject(FormBuilder);
  protected loading = signal(false);
  protected loadingSrv = inject(LoadingService);
  protected location = inject(Location);
  protected reuseTabSrv = inject(ReuseTabService);
  protected route = inject(ActivatedRoute);
  protected titleSrv = inject(TitleService);

  async ngOnInit(): Promise<void> {
    this.defaultParams = Object.fromEntries(
      Object.entries(this.routeParamsMappings).map(([key, mapping]) => [key, (mapping as ParamMappingValue<unknown>).default]),
    ) as TParams;
    this.params ??= signal(this.defaultParams);
    this.paramsForm ??= this.buildParamsForm(this.defaultParams);
  }

  constructor() {
    super();

    effect(async () => {
      await this.handleRouteReload();
    }, {});
  }

  /** Full route-reload pipeline; override to replace navigation side effects. */
  protected async handleRouteReload(): Promise<void> {
    (this.router as any).navigationReload();
    this.loading.set(true);
    const routeParams = {
      pathParams: await (this.route.params as any).firstValuePromise(),
      queryParams: await (this.route.queryParams as any).firstValuePromise(),
      fragment: await (this.route.fragment as any).firstValuePromise(),
    };
    const params = await this.resolve(routeParams);
    this.paramsForm.reset(params, { emitEvent: false });
    this.params.set(params);
    await this.beforeOnRouted(params);
    await this.onRouted(params);
    await this.afterOnRouted(params);
    this.loading.set(false);
    const title = this.title();
    if (title) this.reuseTabSrv.title = title;
    this.cdr.detectChanges();
  }

  protected beforeOnRouted(_params: TParams): void | Promise<void> {}

  protected afterOnRouted(_params: TParams): void | Promise<void> {}

  protected buildParamsForm(defaults: TParams): GeexTypedFormGroup<TParams> {
    return this.fb.group(
      Object.fromEntries(Object.entries(defaults).map(x => [x[0], new FormControl(x[1])])),
    ) as GeexTypedFormGroup<TParams>;
  }

  protected decodeQueryParam(raw: string): unknown {
    return rison.decode(raw);
  }

  async resolve({ pathParams, queryParams, fragment }: RouteParams): Promise<TParams> {
    const params = {} as TParams;

    if (this.routeParamsMappings) {
      const mappings = Object.entries<ParamMappingValue<any>>(this.routeParamsMappings);

      mappings.forEach(([key, mappingValue]) => {
        const value = match(mappingValue.position)
          .with("pathParams", () => pathParams?.[key])
          .with("queryParams", () => (queryParams?.[key] ? this.decodeQueryParam(queryParams[key]) : undefined))
          .with("fragment", () => fragment)
          .exhaustive();

        (params as any)[key] = value ?? mappingValue.default;
      });
    }

    return params;
  }

  refresh() {
    const { pathParams, queryParams, fragment } = this.paramsToRouteParams(this.paramsForm.value as TParams);
    this.router.navigate([".", pathParams], {
      relativeTo: this.route,
      queryParams,
      fragment,
      forceReload: true,
      replaceUrl: true,
    } as any);
  }

  reset() {
    this.paramsForm.reset(this.defaultParams, { emitEvent: false });
    this.refresh();
  }

  abstract onRouted(params: TParams): void | Promise<void>;

  paramsToRouteParams(params: TParams): RouteParams {
    const routeParams: RouteParams = { pathParams: {}, queryParams: {}, fragment: undefined };
    const mappings = Object.entries<ParamMappingValue<any>>(this.routeParamsMappings);

    mappings.forEach(([key, mappingValue]) => {
      const paramValue = (params as any)[key] ?? mappingValue.default;
      const defaultParamValue = (this.defaultParams as any)[key];

      if (paramValue == undefined || this.isEqualToDefault(paramValue, defaultParamValue)) {
        return;
      }

      match(mappingValue.position)
        .with("pathParams", () => ((routeParams.pathParams as any)[key] = paramValue))
        .with("queryParams", () => ((routeParams.queryParams as any)[key] = paramValue))
        .with("fragment", () => (routeParams.fragment = paramValue))
        .exhaustive();
    });

    return routeParams;
  }

  protected isEqualToDefault(value: any, defaultValue: any): boolean {
    return geexIsEqual(value, defaultValue);
  }
}
