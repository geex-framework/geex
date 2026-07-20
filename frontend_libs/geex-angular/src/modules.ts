import { Injector, Signal, WritableSignal, signal } from "@angular/core";
import { fromEvent } from "rxjs";
import { debounceTime, switchMap } from "rxjs/operators";
import { toSignal } from "@angular/core/rxjs-interop";
import { guardedSignal } from "./guarded-signal";

export const ExtensionModule: Record<string, unknown> = {};

export type ExtensionModule = typeof ExtensionModule;

export type GeexModule<TExtension = any> = {
  init: (force?: boolean) => Promise<unknown>;
} & TExtension;

export interface UiModule extends GeexModule<{
  fullScreen: WritableSignal<boolean>;
  isMobile: Signal<boolean | undefined>;
  activeRoutedComponent?: unknown;
}> {}

export interface GeexModuleMap {
  ui: UiModule;
  [name: string]: GeexModule<any>;
}

export type GeexModules<TExtensionModules extends Record<string, GeexModule> = {}> = {
  init: (force?: boolean) => Promise<{ [K in keyof (GeexModuleMap & TExtensionModules)]: unknown }>;
} & GeexModuleMap & TExtensionModules;

export function createUiModule(_injector: Injector): UiModule {
  const _fullScreenSignal = signal<boolean>(false);
  const _isMobile = toSignal(
    fromEvent(window, "resize").pipe(
      debounceTime(200),
      switchMap(async () => window.innerHeight / window.innerWidth >= 1.5),
    ),
  );
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    fullScreen: guardedSignal(_fullScreenSignal, () => _initialized),
    isMobile: guardedSignal(_isMobile as Signal<boolean | undefined>, () => _initialized),
    activeRoutedComponent: undefined,
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          _initialized = true;
        })();
      }
      return _initPromise;
    },
  };
  return module as unknown as UiModule;
}
