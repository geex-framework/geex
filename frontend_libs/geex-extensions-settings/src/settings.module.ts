import { Injector, signal } from "@angular/core";
import { Apollo, gql } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import { guardedSignal } from "@geexcode/geex-angular";
import type { SettingItem, SettingsModule } from "./settings.types";

const GQL_ACTIVE_SETTINGS = gql`query activeSettings { activeSettings { id name value } }`;

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
