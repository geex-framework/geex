import { Injectable, Type } from "@angular/core";
import { State } from "@ngxs/store";
import { StateClass } from "@ngxs/store/internals";
import { Observable } from "rxjs";

export const StateClassDefine = function <TModel>(modelDefaults: TModel) {
  return (target: StateClass<StateBase<TModel>>) => {
    let classDefineFactory = function () {
      Object.defineProperty(target.prototype, "snapshot", {
        get() {
          return this.store.selectSnapshot(target);
        },
      });
      Object.defineProperty(target.prototype, "next", {
        get() {
          return this.store.selectOnce(target);
        },
      });
      Object.defineProperty(target.prototype, "observable", {
        get() {
          return this.store.select(target);
        },
      });
      return target;
    };
    let classDefine = classDefineFactory();
    State({ name: classDefine.name, defaults: modelDefaults })(classDefine);
    return classDefine as any;
  };
};

declare module "@ngxs/store/src/public_api" {
  export interface Store {
    dispatch(a: AppActions | AppActions[]): Observable<any>;
    select<TStateDefine extends abstract new (...args: any) => any>(
      stateDefine: TStateDefine,
    ): Observable<StateModelInfer<InstanceType<TStateDefine>>>;
    /**
     * Select one slice of data from the store.
     */
    selectOnce<TStateDefine extends abstract new (...args: any) => any>(
      stateDefine: TStateDefine,
    ): Observable<StateModelInfer<InstanceType<TStateDefine>>>;
    /**
     * Select a snapshot from the state.
     */
    selectSnapshot<TStateDefine extends abstract new (...args: any[]) => any>(
      stateDefine: TStateDefine,
    ): StateModelInfer<InstanceType<TStateDefine>>;
  }
}

type StateModelInfer<Type> = Type extends StateBase<infer X> ? X : never;

// type extracted = StateModelInfer<TenantStateDefine>;

import { AppActions } from "./AppActions";
import { StateBase } from "./StateBase";
import { TenantState as TenantStateDefine } from "./tenant.state";
