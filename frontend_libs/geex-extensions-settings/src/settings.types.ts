import type { WritableSignal } from "@angular/core";
import type { GeexModule } from "@geexcode/geex-angular";

export interface SettingItem {
  name: string;
  value?: any;
}

export interface SettingsModule extends GeexModule<{
  settings: WritableSignal<SettingItem[]>;
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    settings: SettingsModule;
  }
}
