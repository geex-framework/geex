import { Component, Injector, OnInit, ViewChild } from "@angular/core";
import { AbstractControl, FormBuilder, FormControl, FormGroup } from "@angular/forms";
import { gql } from "apollo-angular";
import { isDate, isEqual } from "lodash-es";
import { combineLatest, forkJoin, from, zip } from "rxjs";
import { filter, map } from "rxjs/operators";

import { EditMode } from "../../types/common";
import { MyFormGroup, RoutedComponent } from "./routed.component.base";

type PropValueType<T, K extends keyof T> = T[K];

type FormControlPropMap<X, T extends keyof X> = {
  [K in T]?: FormControl<PropValueType<{ [P in K]?: X[T] }, K>> | PropValueType<{ [P in K]?: X[T] }, K>;
};

// type test = FormControlPropMap<{ id: string; name: string }, "id" | "name">;
// type test = { [key in keyof ["id","name"]]: never };

/**数据上下文 */
export class EditDataContext<T extends { id?: string }, TEditable extends keyof T> {
  id?: string;
  entity?: Partial<T>;
  entityForm?: MyFormGroup<FormControlPropMap<T, TEditable>>;
  originalValue?: { [key in TEditable]?: T[key] };
}

/**
 *
 * @class 路由组件基类
 * @extends {BusinessComponentBase}
 * @template TParams 参数对应的表单类型
 * @template TContext 绑定数据上下文类型
 */
@Component({ template: "" })
export abstract class RoutedEditComponent<
  TParams extends {},
  TDto extends { id?: string },
  TEditable extends keyof TDto, // 设置默认类型参数
> extends RoutedComponent<TParams, EditDataContext<TDto, TEditable>> {
  mode: EditMode;

  constructor(injector: Injector) {
    super(injector);
  }

  async close() {
    if (await this.closableCheck()) {
      await this.back();
    }
  }

  closableCheck() {
    if (isEqual(this.context.entityForm.value, this.context.originalValue)) {
      return Promise.resolve(true);
    }
    let promise = new Promise<boolean>((resolve, reject) => {
      this.nzModalSrv.confirm({
        nzTitle: "当前页面内容未保存，确定离开？",
        nzOnOk: async () => {
          this.context.entityForm.reset(this.context.originalValue);
          this.context.entityForm.markAsPristine();
          resolve(true);
        },
        nzOnCancel: () => {
          resolve(false);
        },
      });
    });
    return promise;
  }

  async back(reload: boolean = false) {
    await this.router.navigate(["../"], { relativeTo: this.route, replaceUrl: true, forceReload: reload });
  }
}
