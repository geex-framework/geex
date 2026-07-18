import type { Signal, WritableSignal } from "@angular/core";

export function guardedSignal<T>(
  innerSignal: WritableSignal<T>,
  isInitialized: () => boolean,
): WritableSignal<T>;
export function guardedSignal<T>(
  innerSignal: Signal<T>,
  isInitialized: () => boolean,
): Signal<T>;
export function guardedSignal<T>(
  innerSignal: Signal<T> | WritableSignal<T>,
  isInitialized: () => boolean,
): Signal<T> | WritableSignal<T> {
  const guard = (() => {
    if (!isInitialized()) {
      throw new Error(`GuardedSignal not initialized. isInitialized: ${isInitialized.toString()}`);
    }
    return innerSignal();
  }) as WritableSignal<T>;

  if ("set" in innerSignal) {
    guard.set = innerSignal.set.bind(innerSignal);
    guard.update = innerSignal.update.bind(innerSignal);
  }
  if ("asReadonly" in innerSignal) {
    guard.asReadonly = innerSignal.asReadonly.bind(innerSignal);
  }

  return guard;
}
