import { filter, firstValueFrom, lastValueFrom, map, Observable } from "rxjs";

declare module "rxjs" {
  interface Observable<T> {
    lastValuePromise(this: this): Promise<T | undefined>;
    firstValuePromise(this: this): Promise<T | undefined>;
    map<R>(this: this, project: (value: T, index: number) => R): Observable<R | undefined>;
    filter<T>(this: this, predicate: (value: T, index: number) => boolean): Observable<T | undefined>;
  }
}

Observable.prototype.lastValuePromise = function () {
  return lastValueFrom(this);
};

Observable.prototype.firstValuePromise = function () {
  return firstValueFrom(this);
};
Observable.prototype.map = function (project) {
  return this.pipe(map(project));
};

Observable.prototype.filter = function (predicate) {
  return this.pipe(filter(predicate));
};
