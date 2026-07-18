import { addDays, addHours, addMilliseconds, addMinutes, addMonths, addSeconds, addWeeks, addYears } from "date-fns";
import { Maybe } from "graphql/jsutils/Maybe";
import * as _ from "lodash-es";

if (!Object.hasOwnProperty("fromEntries")) {
  Object.fromEntries = function fromEntries(iterable: Iterable<readonly [PropertyKey, unknown]>) {
    return [...iterable].reduce((obj: Record<string, unknown>, [key, val]) => {
      obj[String(key)] = val;
      return obj;
    }, {});
  };
}

Date.prototype.add = function (value: {
  years?: number;
  months?: number;
  weeks?: number;
  days?: number;
  hours?: number;
  minutes?: number;
  seconds?: number;
  milliseconds?: number;
}) {
  let result = this;
  if (value.years) {
    result = addYears(result, value.years);
  }
  if (value.months) {
    result = addMonths(result, value.months);
  }
  if (value.weeks) {
    result = addWeeks(result, value.weeks);
  }
  if (value.days) {
    result = addDays(result, value.days);
  }
  if (value.hours) {
    result = addHours(result, value.hours);
  }
  if (value.minutes) {
    result = addMinutes(result, value.minutes);
  }
  if (value.seconds) {
    result = addSeconds(result, value.seconds);
  }
  if (value.milliseconds) {
    result = addMilliseconds(result, value.milliseconds);
  }
  return result;
};

Date.prototype.format = function (this: Date, format: string) {
  const yyyy = this.getFullYear().toString();
  format = format.replace(/yyyy/g, yyyy);
  const MM = (this.getMonth() + 1).toString();
  format = format.replace(/MM/g, MM[1] ? MM : `0${MM[0]}`);
  const dd = this.getDate().toString();
  format = format.replace(/dd/g, dd[1] ? dd : `0${dd[0]}`);
  const HH = this.getHours().toString();
  format = format.replace(/HH/g, HH[1] ? HH : `0${HH[0]}`);
  const mm = this.getMinutes().toString();
  format = format.replace(/mm/g, mm[1] ? mm : `0${mm[0]}`);
  const ss = this.getSeconds().toString();
  format = format.replace(/ss/g, ss[1] ? ss : `0${ss[0]}`);
  return format;
};

Date.prototype.getTotalMonth = function (this: Date) {
  return this.getFullYear() * 12 + this.getMonth() + 1;
};

export function extract<TActual = any, T = any>(object: TActual, properties: Partial<Record<keyof (T | TActual), true>>) {
  const result = {} as Partial<T>;
  for (const property of Object.keys(properties) as Array<keyof (T | TActual)>) {
    result[property] = object[property] as any;
  }
  return result;
}
window["extract"] = extract;

declare global {
  type Hint<T> = Maybe<T>;

  function extract<TActual = any, T = any>(object: TActual, properties: Partial<Record<keyof (T | TActual), true>>): Partial<T>;
  interface Date {
    add: (value: {
      years?: number;
      months?: number;
      weeks?: number;
      days?: number;
      hours?: number;
      minutes?: number;
      seconds?: number;
      milliseconds?: number;
    }) => Date;
    format(format: string): string;
    getTotalMonth(): number;
  }
  interface Array<T> {
    flatMapDeep<U>(this: this, iteratee: (value: T, index: number, array: T[]) => U[], thisArg?: any): U[];
  }
  interface ArrayConstructor {
    range(start: number, end: number): number[];
  }
  interface Number {
    hasFlag(...flags: number[]): boolean;
    hasNoFlag(...flags: number[]): boolean;
  }
  interface String {
    trimEnd(strToTrim: string): string;
  }
}

const legacyTrimEnd = String.prototype.trimEnd;
String.prototype.trimEnd = function trimEnd(this: string, strToTrim?: string) {
  if (strToTrim == undefined) {
    return legacyTrimEnd.bind(this)();
  }
  return this.replace(new RegExp(`${strToTrim}$`), "");
};

Number.prototype.hasFlag = function hasFlag(this: number, ...flags: number[]) {
  return flags.all(flag => flag !== undefined && (this.valueOf() & flag) == flag);
};

Number.prototype.hasNoFlag = function hasNoFlag(this: number, ...flags: number[]) {
  return flags.all(flag => flag !== undefined && (this.valueOf() & flag) == 0);
};

const flatMapDeep = function <T>(this: T[], iteratee: (value: T, index: number, array: T[]) => T[]) {
  if (!this || this.length == 0) {
    return [];
  }
  return this.concat(_.flatten(this.map(iteratee).filter(x => x != undefined)).flatMapDeep(iteratee));
};

Array.prototype.flatMapDeep = flatMapDeep;

const range = (start: number, end: number) => Array.from({ length: end - start }, (v, k) => k + start);
Array.range = range;

export function assertIsDefined<T>(val: T): asserts val is NonNullable<T> {
  if (val === undefined || val === null) {
    throw new Error(`Expected 'val' to be defined, but received ${val}`);
  }
}

export function assertIsArray<T>(val: T | T[]): asserts val is T[] {
  if (!Array.isArray(val)) {
    throw new Error(`Expected 'val' to be defined, but received ${val}`);
  }
}

export function assert<T>(_val: any): asserts _val is T {}

export function assertIsNotArray<T>(val: T | T[]): asserts val is T {
  if (Array.isArray(val)) {
    throw new Error(`Expected 'val' to be defined, but received ${val}`);
  }
}

export type PropertyAccessType = "create" | "modify" | "delete" | "assignment";

export function deepProxy<T>(
  obj: T,
  callback: (type: PropertyAccessType, param: { target: any; key: string | number | symbol; value: any }) => void,
) {
  if (
    obj === null ||
    obj === undefined ||
    obj instanceof String ||
    obj instanceof Date ||
    obj instanceof Number ||
    obj instanceof Function
  ) {
    return obj;
  }
  if (typeof obj === "object") {
    for (const key in obj) {
      if (typeof obj[key] === "object") {
        obj[key] = deepProxy(obj[key], callback);
      }
    }
  }

  return new Proxy(obj, {
    set: (target: any, key: any, value: any, receiver: any) => {
      if (typeof value === "object") {
        value = deepProxy(value, callback);
      }
      let cbType: PropertyAccessType = target[key] == undefined ? "create" : "modify";

      if (target[key] === value) {
        cbType = "assignment";
      }
      if (!(Array.isArray(target) && key === "length")) {
        callback(cbType, { target, key, value });
      }
      return Reflect.set(target, key, value, receiver);
    },
    deleteProperty: (target, key) => {
      callback("delete", { target, key, value: undefined });
      return Reflect.deleteProperty(target, key);
    },
  });
}

export function isRecord<TValue = any>(value: unknown): value is Record<string, TValue> {
  return typeof value === "object" && value !== null && !Array.isArray(value) && Object.keys(value).every(key => typeof key === "string");
}
