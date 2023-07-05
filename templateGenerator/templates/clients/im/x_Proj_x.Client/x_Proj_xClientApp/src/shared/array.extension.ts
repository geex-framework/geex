import { List } from "linqts-camelcase";
declare global {
  interface ReadonlyArray<T> {
    any(): boolean;
    any(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): boolean;
    any(predicate?: any);
    first(): T;
    first(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T;
    first(predicate?: any);
    firstOrDefault(defaultValue?: Partial<T>): T;
    firstOrDefault(
      predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
      defaultValue?: Partial<T>,
    ): T;
    firstOrDefault(predicate?: any, defaultValue?: Partial<T>);
  }
  interface Array<T> {
    add(element: T): void;
    addRange(elements: T[]): void;
    aggregate<U>(
      accumulator: (accum: U, value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => any,
      initialValue?: U | undefined,
    );
    all(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): boolean;
    any(): boolean;
    clear(): void;
    any(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): boolean;
    any(predicate?: any);
    average(): number;
    average(transform: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => any): number;
    average(transform?: any);
    contains(element: T): boolean;
    count(): number;
    count(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): number;
    count(predicate?: any);
    defaultIfEmpty(defaultValue?: T | undefined): T[];
    distinct(): T[];
    distinctBy(keySelector: (key: T) => string | number): T[];
    elementAt(index: number): T;
    toArray(): T[];
    elementAtOrDefault(index: number): T;
    except(source: T[]): T[];
    first(): T;
    first(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T;
    first(predicate?: any);
    firstOrDefault(defaultValue?: Partial<T>): T;
    firstOrDefault(
      predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
      defaultValue?: Partial<T>,
    ): T;
    firstOrDefault(predicate?: any, defaultValue?: Partial<T>);
    groupBy<TResult = T>(
      grouper: (key: T) => string | number,
      mapper?: ((element: T) => TResult) | undefined,
    ): { [key: string]: TResult[] };
    groupJoin<U, R>(list: List<U>, key1: (k: T) => any, key2: (k: U) => any, result: (first: T, second: List<U>) => any): R[];
    // indexOf(element: T): number;
    insert(index: number, element: T): void | Error;
    intersect(source: T[]): T[];
    linqJoin<U, R>(list: List<U>, key1: (key: T) => any, key2: (key: U) => any, result: (first: T, second: U) => R): R[];
    last(): T;
    last(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T;
    last(predicate?: any);
    lastOrDefault(): T;
    lastOrDefault(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T;
    lastOrDefault(predicate?: any);
    max(): number;
    max(selector: (value: T, index: number, array: T[]) => number): number;
    max(selector?: any);
    min(): number;
    min(selector: (value: T, index: number, array: T[]) => number): number;
    min(selector?: any);
    ofType<U>($type: any): List<U>;
    orderBy(keySelector: (key: T) => any, comparer?: ((a: T, b: T) => number) | undefined): T[];
    orderByDescending(keySelector: (key: T) => any, comparer?: ((a: T, b: T) => number) | undefined): T[];
    thenBy(keySelector: (key: T) => any): T[];
    thenByDescending(keySelector: (key: T) => any): T[];
    remove(element: T): boolean;
    removeAll(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T[];
    removeAt(index: number): void;
    // select<TOut>(selector: (element: T, index: number) => TOut): List<TOut>;
    selectMany<TOut extends any[]>(selector: (element: T, index: number) => TOut): TOut;
    sequenceEqual(list: T[]): boolean;
    single(predicate?: ((value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean) | undefined): T;
    singleOrDefault(predicate?: ((value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean) | undefined): T;
    skip(amount: number): T[];
    skipWhile(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T[];
    sum(): number;
    sum(transform: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => number): number;
    sum(transform?: any);
    take(amount: number): T[];
    takeWhile(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T[];
    // toArray(): T[];
    toDictionary<TKey>(key: (key: T) => TKey): List<{ Key: TKey; Value: T }>;
    toDictionary<TKey, TValue>(key: (key: T) => TKey, value: (value: T) => TValue): List<{ Key: TKey; Value: T | TValue }>;
    toDictionary(key: any, value?: any);
    // toList(): T[];
    toLookup<TResult>(keySelector: (key: T) => string | number, elementSelector: (element: T) => TResult): { [key: string]: TResult[] };
    union(list: T[]): T[];
    where(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T[];
    zip<U, TOut>(list: U[], result: (first: T, second: U) => TOut): TOut[];
  }
}
// Array.prototype._elements = Array.prototype;
Array.prototype.add = function add<T>(this: T[], element: T): void {
  return List.prototype.add.bind(new List(this), element)();
};
Array.prototype.clear = function clear<T>(this: T[]): void {
  while (this.pop()) {}
};
Array.prototype.addRange = function addRange<T>(this: T[], elements: T[]): void {
  return List.prototype.addRange.bind(new List(this), elements)();
};
Array.prototype.aggregate = function aggregate<T, U>(
  this: T[],
  accumulator: (accum: U, value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => any,
  initialValue?: U | undefined,
) {
  return List.prototype.aggregate.bind(new List(this), accumulator, initialValue)();
};
Array.prototype.all = function all<T>(
  this: T[],
  predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): boolean {
  return List.prototype.all.bind(new List(this), predicate)();
};
Array.prototype.any = function any<T>(
  this: T[],
  predicate?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): boolean {
  if (this === undefined) {
    return false;
  }
  return List.prototype.any.bind(new List(this), predicate!)();
};
Array.prototype.average = function average<T>(
  this: T[],
  transform?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => any,
): number {
  return List.prototype.average.bind(new List(this))(transform).toArray();
};
// Array.prototype.cast = function cast<U>(): List<U> {
//     return List.prototype.cast.bind(this)();
// }

Array.prototype.contains = function contains<T>(element: T): boolean {
  this._elements = this;
  return List.prototype.contains.bind(this, element)();
};
Array.prototype.count = function count<T>(
  this: T[],
  predicate?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): number {
  return List.prototype.count.bind(new List(this))(predicate);
};
Array.prototype.defaultIfEmpty = function defaultIfEmpty<T>(this: T[], defaultValue?: T | undefined): T[] {
  return List.prototype.defaultIfEmpty.bind(new List(this))(defaultValue).toArray();
};
Array.prototype.distinct = function distinct<T>(this: T[]): T[] {
  return List.prototype.distinct.bind(new List(this))().toArray();
};
Array.prototype.distinctBy = function distinctBy<T>(this: T[], keySelector: (key: T) => string | number): T[] {
  return List.prototype.distinctBy.bind(new List(this))(keySelector).toArray();
};
Array.prototype.elementAt = function elementAt<T>(this: T[], index: number): T {
  return List.prototype.elementAt.bind(new List(this))(index);
};
Array.prototype.elementAtOrDefault = function elementAtOrDefault<T>(this: T[], index: number): T {
  if (this?.length) {
    if (this.length <= index) {
      return undefined;
    }
    return List.prototype.elementAtOrDefault.bind(new List(this))(index);
  }
  return undefined;
};
Array.prototype.except = function except<T>(this: T[], source: T[]): T[] {
  return List.prototype.except.bind(new List(this))(source).toArray();
};
Array.prototype.first = function first<T>(
  this: T[],
  predicate?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): T {
  return List.prototype.first.bind(new List(this), predicate)();
};
Array.prototype.firstOrDefault = function firstOrDefault<T>(
  this: T[],
  predicate?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
  defaultValue?: T,
): T {
  let result = List.prototype.firstOrDefault.bind(new List(this), predicate)();
  return result === undefined ? defaultValue : result;
};
Array.prototype.first = function first<T>(
  this: T[],
  predicate?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): T {
  return List.prototype.first.bind(new List(this), predicate)();
};
Array.prototype.firstOrDefault = function firstOrDefault<T>(
  this: T[],
  predicate?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
  defaultValue?: T,
): T {
  let result = List.prototype.firstOrDefault.bind(new List(this), predicate)();
  return result === undefined ? defaultValue : result;
};
Array.prototype.groupBy = function groupBy<T, TResult = T>(
  grouper: (key: T) => string | number,
  mapper?: ((element: T) => TResult) | undefined,
): { [key: string]: TResult[] } {
  return List.prototype.groupBy.bind(new List(this))(grouper, mapper);
};
Array.prototype.groupJoin = function groupJoin<T, U, R>(
  list: List<U>,
  key1: (k: T) => any,
  key2: (k: U) => any,
  result: (first: T, second: List<U>) => any,
): R[] {
  return List.prototype.groupJoin.bind(new List(this), key1, key2, result)().toArray();
};
// Array.prototype.indexOf = function indexOf<T>(this: Array<T>, element: T): number {
//     return List.prototype.indexOf.bind(this, element)();
// }
Array.prototype.insert = function insert<T>(this: T[], index: number, element: T): void | Error {
  return List.prototype.insert.bind(new List(this))(index, element);
};
Array.prototype.intersect = function intersect<T>(this: T[], source: T[]): T[] {
  return List.prototype.intersect.bind(new List(this), new List(source))().toArray();
};
Array.prototype.linqJoin = function linqJoin<T, U, R>(
  list: List<U>,
  key1: (key: T) => any,
  key2: (key: U) => any,
  result: (first: T, second: U) => R,
): R[] {
  return List.prototype.join.bind(new List(this), key1, key2, result)().toArray();
};
Array.prototype.last = function last<T>(
  this: T[],
  predicate?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): T {
  return List.prototype.last.bind(new List(this))(predicate);
};
Array.prototype.lastOrDefault = function lastOrDefault<T>(
  this: T[],
  predicate?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): T {
  return List.prototype.lastOrDefault.bind(new List(this))(predicate);
};
Array.prototype.max = function max<T>(this: T[], selector?: (value: T, index: number, array: T[]) => number): number {
  return List.prototype.max.bind(new List(this))(selector);
};
Array.prototype.min = function min<T>(this: T[], selector?: (value: T, index: number, array: T[]) => number): number {
  return List.prototype.min.bind(new List(this))(selector);
};
Array.prototype.ofType = function ofType<T, U>($type: any): List<U> {
  return List.prototype.ofType.bind(new List(this), $type)();
};
Array.prototype.orderBy = function orderBy<T>(
  this: T[],
  keySelector: (key: T) => any,
  comparer?: ((a: T, b: T) => number) | undefined,
): T[] {
  return List.prototype.orderBy.bind(new List(this), keySelector, comparer)().toArray();
};
Array.prototype.orderByDescending = function orderByDescending<T>(
  this: T[],
  keySelector: (key: T) => any,
  comparer?: ((a: T, b: T) => number) | undefined,
): T[] {
  return List.prototype.orderByDescending.bind(new List(this), keySelector, comparer)().toArray();
};
Array.prototype.thenBy = function thenBy<T>(this: T[], keySelector: (key: T) => any): T[] {
  return List.prototype.thenBy.bind(new List(this), keySelector)().toArray();
};
Array.prototype.thenByDescending = function thenByDescending<T>(this: T[], keySelector: (key: T) => any): T[] {
  return List.prototype.thenByDescending.bind(new List(this), keySelector)().toArray();
};
Array.prototype.remove = function remove<T>(this: T[], element: T): boolean {
  return List.prototype.remove.bind(new List(this), element)();
};
Array.prototype.removeAll = function removeAll<T>(
  this: T[],
  predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): T[] {
  return List.prototype.removeAll.bind(new List(this))(predicate).toArray();
};
Array.prototype.removeAt = function removeAt<T>(this: T[], index: number): void {
  return List.prototype.removeAt.bind(new List(this), index)();
};

// Array.prototype.select = function select<T, TOut>(selector: (element: T, index: number) => TOut): List<TOut> {
//     return List.prototype.add.bind(this, element)();
// }
Array.prototype.selectMany = function selectMany<T, TOut extends T[]>(this: T[], selector: (element: T, index: number) => TOut): TOut {
  return List.prototype.selectMany.bind(new List(this))(selector).toArray();
};
Array.prototype.sequenceEqual = function sequenceEqual<T>(this: T[], list: T[]): boolean {
  if (this?.length !== list?.length) {
    return false;
  }
  if (this?.length === 0 && this?.length === list?.length) {
    return true;
  }
  for (let i = 0; i < this.length; i++) {
    const element = this[i];
    if (this[i] === list[i]) {
      continue;
    } else {
      return false;
    }
  }
  return true;
};
Array.prototype.single = function single<T>(
  predicate?: ((value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean) | undefined,
): T {
  return List.prototype.single.bind(new List(this), predicate)();
};
Array.prototype.singleOrDefault = function singleOrDefault<T>(
  predicate?: ((value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean) | undefined,
): T {
  return List.prototype.singleOrDefault.bind(new List(this), predicate)();
};
Array.prototype.skip = function skip<T>(amount: number): T[] {
  return List.prototype.skip.bind(new List(this), amount)().toArray();
};
Array.prototype.skipWhile = function skipWhile<T>(
  predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): T[] {
  return List.prototype.skipWhile.bind(new List(this), predicate)().toArray();
};
Array.prototype.sum = function sum<T>(
  this: T[],
  transform?: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => number,
): number {
  return List.prototype.sum.bind(new List(this))(transform);
};
Array.prototype.take = function take<T>(this: T[], amount: number): T[] {
  return List.prototype.take.bind(new List(this), amount)().toArray();
};
Array.prototype.takeWhile = function takeWhile<T>(
  this: T[],
  predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): T[] {
  return List.prototype.takeWhile.bind(this, predicate)().toArray();
};
// Array.prototype.toArray = function toArray<T>(this: Array<T>, ): T[] {
//     return List.prototype.add.bind(this, element)();
// }
// Array.prototype.toDictionary = function toDictionary<T, TKey, TValue>(this: Array<T>, key: TKey, value?: TValue) {
//     return List.prototype.add.bind(this, element)();
// }
// Array.prototype.toList = function toT[](this: Array<T>, ): T[] {
//     return List.prototype.add.bind(this, element)();
// }
Array.prototype.toLookup = function toLookup<T, TResult>(
  keySelector: (key: T) => string | number,
  elementSelector: (element: T) => TResult,
): { [key: string]: TResult[] } {
  return List.prototype.toLookup.bind(new List(this), keySelector, elementSelector)();
};
Array.prototype.union = function union<T>(this: T[], list: T[]): T[] {
  return List.prototype.union.bind(new List(this), list)().toArray();
};
Array.prototype.where = function where<T>(
  this: T[],
  predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean,
): T[] {
  return List.prototype.where.bind(new List(this), predicate)().toArray();
};
Array.prototype.toArray = function where<T>(this: T[]): T[] {
  return this;
};
Array.prototype.zip = function zip<T, U, TOut>(list: U[], result: (first: T, second: U) => TOut): TOut[] {
  return List.prototype.zip.bind(new List(this))(list, result).toArray();
};
