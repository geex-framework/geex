import { ExtendedSignal } from "@angular/core";
import { ToSignalOptions, toSignal } from "@angular/core/rxjs-interop";
import { filter, firstValueFrom, lastValueFrom, map, Observable, ObservableInput, ObservedValueOf, switchMap } from "rxjs";
import { DeepSignal } from "./signal.extension";

declare module "rxjs" {
  interface Observable<T> {
    lastValuePromise(this: this): Promise<T | undefined>;
    firstValuePromise(this: this): Promise<T | undefined>;
    toSignal(
      this: this,
      options?: ToSignalOptions<T> & {
        deep?: false;
        initialValue?: T;
        requireSync?: false;
      },
    ): ExtendedSignal<T>;
    toSignal(
      this: this,
      options?: ToSignalOptions<T> & {
        deep?: true;
        initialValue?: T;
        requireSync?: false;
      },
    ): DeepSignal<T>;
    pipeMap<R>(this: this, project: (value: T, index: number) => R | undefined): Observable<R | undefined>;
    pipeSwitchMap<R extends ObservableInput<any>>(this: this, project: (value: T, index: number) => R): Observable<ObservedValueOf<R>>;
    pipeFilter(this: this, predicate: (value: T, index: number) => boolean): Observable<T | undefined>;
  }
}

Observable.prototype.lastValuePromise = function () {
  return lastValueFrom(this);
};

Observable.prototype.firstValuePromise = function () {
  return firstValueFrom(this);
};
Observable.prototype.toSignal = function (options) {
  if (options?.deep) {
    return (toSignal(this, options) as ExtendedSignal<any>).toDeepSignal();
  }
  return toSignal(this, options!) as ExtendedSignal<any>;
};
Observable.prototype.pipeMap = function (project) {
  return this.pipe(map(project));
};
Observable.prototype.pipeSwitchMap = function (project) {
  return this.pipe(switchMap(project));
};

Observable.prototype.pipeFilter = function (predicate) {
  return this.pipe(filter(predicate));
};
