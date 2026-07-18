import { computed, CreateSignalOptions, effect, ExtendedSignal, isSignal, Signal, signal, untracked, WritableSignal } from "@angular/core";
import { firstValueFrom, isObservable, Observable, of } from "rxjs";

declare module "@angular/core" {
  interface SignalExtensions<T> {
    toDebounced(debounceTimeInMs: number): ExtendedSignal<T>;
    toObservable(): Observable<T>;
  }

  export type ExtendedSignal<T> = Signal<T> &
    SignalExtensions<T> & {
      toDeepSignal(): DeepSignal<T>;
    };
  export type ExtendedWritableSignal<T> = WritableSignal<T> &
    SignalExtensions<T> & {
      toDeepSignal(): WritableDeepSignal<T>;
    };

  function signal<T>(initialValue: T, options?: CreateSignalOptions<T>): ExtendedWritableSignal<T>;
}

type IsRecord<T> = T extends object
  ? T extends unknown[]
    ? false
    : T extends Set<unknown>
      ? false
      : T extends Map<unknown, unknown>
        ? false
        : T extends Function
          ? false
          : true
  : false;

type IsUnknownRecord<T> = string extends keyof T ? true : number extends keyof T ? true : false;

type IsKnownRecord<T> = IsRecord<T> extends true ? (IsUnknownRecord<T> extends true ? false : true) : false;

export type DeepSignal<T> = ExtendedSignal<T> &
  (IsKnownRecord<T> extends true
    ? Readonly<{
        [K in keyof T]: IsKnownRecord<T[K]> extends true ? DeepSignal<T[K]> : ExtendedSignal<T[K]>;
      }>
    : unknown);

export type WritableDeepSignal<T> = Signal<T> &
  WritableSignal<T> &
  (IsKnownRecord<T> extends true
    ? Readonly<{
        [K in keyof T]: IsKnownRecord<T[K]> extends true ? WritableDeepSignal<T[K]> : WritableSignal<T[K]>;
      }>
    : unknown);

export function deepSignal<T>(initialValue: T, options?: CreateSignalOptions<T>): WritableDeepSignal<T> {
  const result = toDeepSignal(signal(initialValue, options));
  return result;
}

function toDeepSignal<T>(source: ExtendedSignal<T>): DeepSignal<T>;
function toDeepSignal<T>(source: WritableSignal<T>): WritableDeepSignal<T>;
function toDeepSignal<T>(source: ExtendedSignal<T> | WritableSignal<T>): DeepSignal<T> | WritableDeepSignal<T> {
  const value = untracked(() => source());
  if (!isRecord(value)) {
    return source as DeepSignal<T>;
  }
  if ("set" in source && typeof (source as WritableSignal<T>).set === "function") {
    return new Proxy(source as WritableSignal<T>, {
      get(target: any, prop) {
        if (!(prop in value) && prop in target) {
          return target[prop];
        }

        if (isSignal(target[prop])) {
          Object.defineProperty(target, prop, {
            value: computed(() => target()[prop]),
            configurable: true,
          });
        }

        if (target[prop] == undefined) {
          return signal(target[prop]);
        }

        return toDeepSignal(target[prop]);
      },
      set(target: any, prop: PropertyKey, value: any) {
        if (isSignal(target[prop])) {
          (target[prop] as WritableSignal<any>).set(value);
        } else {
          target[prop] = value;
        }
        return true;
      },
    }) as WritableDeepSignal<T>;
  }
  return new Proxy(signal, {
    get(target: any, prop) {
      if (!(prop in value) && prop in target) {
        return target[prop];
      }

      if (!isSignal(target[prop])) {
        Object.defineProperty(target, prop, {
          value: computed(() => target()[prop]),
          configurable: true,
        });
      }

      return toDeepSignal(target[prop]);
    },
  });
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return value?.constructor === Object;
}

function toDebounced<T>(sourceSignal: ExtendedSignal<T>, debounceTimeInMs = 0): ExtendedSignal<T> {
  const debounceSignal = signal(sourceSignal());
  effect(onCleanup => {
    const value = sourceSignal();
    const timeout = setTimeout(() => debounceSignal.set(value), debounceTimeInMs);
    onCleanup(() => clearTimeout(timeout));
  }, {});
  return debounceSignal as unknown as ExtendedSignal<T>;
}
const signal1 = signal(null);
const signalProto = Object.getPrototypeOf(signal1) as Record<string, unknown>;
signalProto["toDebounced"] = function (this: ExtendedSignal<any>, debounceTimeInMs = 0) {
  return toDebounced(this, debounceTimeInMs);
};

signalProto["toDeepSignal"] = function (this: ExtendedSignal<any>) {
  return toDeepSignal(this);
};

function toObservable<T>(sourceSignal: ExtendedSignal<T>): ExtendedSignal<T> {
  return of(sourceSignal()) as unknown as ExtendedSignal<T>;
}
signalProto["toObservable"] = function (this: ExtendedSignal<any>) {
  return toObservable(this);
};

export function computedAsync<T>(computation: () => Observable<T> | Promise<T> | T | undefined | null): Signal<T | null> {
  const resultSignal = signal<T | null>(null);

  effect(async () => {
    const result = computation();
    const unwrappedResult = await (isObservable(result) ? firstValueFrom(result as Observable<T>, { defaultValue: null }) : result);

    resultSignal.set(unwrappedResult as T | null);
  }, {});

  return resultSignal.asReadonly();
}
