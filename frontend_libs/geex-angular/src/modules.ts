import { Apollo } from "apollo-angular";
import { Injector, Signal, WritableSignal, signal } from "@angular/core";
import { OAuthService } from "angular-oauth2-oidc";
import gql from "graphql-tag";
import { firstValueFrom, fromEvent } from "rxjs";
import { debounceTime, switchMap, map } from "rxjs/operators";
import { toSignal } from "@angular/core/rxjs-interop";

export const ExtensionModule: Record<string, unknown> = {};

export type ExtensionModule = typeof ExtensionModule;

export type GeexModule<TExtension = any> = {
  init: (force?: boolean) => Promise<unknown>;
} & TExtension;

export interface SettingItem {
  name: string;
  value?: any;
}

export interface MessagingModule extends GeexModule<{
  onPublicNotify(notify: unknown): void;
}> {}

export interface SettingsModule extends GeexModule<{
  settings: WritableSignal<SettingItem[]>;
}> {}

export interface UiModule extends GeexModule<{
  fullScreen: WritableSignal<boolean>;
  isMobile: Signal<boolean | undefined>;
  activeRoutedComponent?: unknown;
}> {}

export interface GeexModuleMap {
  messaging: MessagingModule;
  settings: SettingsModule;
  ui: UiModule;
  [name: string]: GeexModule<any>;
}

export type GeexModules<TExtensionModules extends Record<string, GeexModule> = {}> = {
  init: (force?: boolean) => Promise<{ [K in keyof (GeexModuleMap & TExtensionModules)]: unknown }>;
} & GeexModuleMap & TExtensionModules;

const GQL_ON_PUBLIC_NOTIFY = gql`subscription onPublicNotify { onPublicNotify { __typename ... on DataChangeClientNotify { dataChangeType } } }`;
const GQL_ACTIVE_SETTINGS = gql`query activeSettings { activeSettings { id name value } }`;

function guardedSignal<T>(
  innerSignal: WritableSignal<T>,
  isInitialized: () => boolean,
): WritableSignal<T>;
function guardedSignal<T>(innerSignal: Signal<T>, isInitialized: () => boolean): Signal<T>;
function guardedSignal<T>(
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

export function createMessagingModule(
  injector: Injector,
  deps?: () => Pick<GeexModule, "init"> | undefined,
): MessagingModule {
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            await deps?.()?.init();
            if (injector.get(OAuthService).hasValidAccessToken()) {
              const subClient = injector.get(Apollo).use("subscription");
              subClient
                .subscribe<{ onPublicNotify: unknown }>({ query: GQL_ON_PUBLIC_NOTIFY })
                .pipe(map(res => res?.data?.onPublicNotify))
                .subscribe(notify => {
                  module.onPublicNotify(notify);
                });
            }
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
    onPublicNotify(notify: unknown) {
      console.log("Public notify", notify);
    },
  };
  return module as MessagingModule;
}

export function createSettingsModule(injector: Injector): SettingsModule {
  const _settingsSignal = signal<SettingItem[]>([]);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    settings: guardedSignal(_settingsSignal, () => _initialized),
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            type ActiveSettingsResponse = { data: { activeSettings: SettingItem[] } };
            const res = (await firstValueFrom(
              injector.get(Apollo).query<ActiveSettingsResponse>({ query: GQL_ACTIVE_SETTINGS }),
            )) as unknown as ActiveSettingsResponse;
            _settingsSignal.set(res.data.activeSettings);
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
  };
  return module as unknown as SettingsModule;
}

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
