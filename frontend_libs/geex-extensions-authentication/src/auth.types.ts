import type { WritableSignal } from "@angular/core";
import type { GeexModule } from "@geexcode/geex-angular";
import type { User } from "@geexcode/geex-extensions-identity";

export interface AuthModule extends GeexModule<{
  user: WritableSignal<User | null>;
  loadUserData(): Promise<User | undefined>;
  /** Drop cached session and reload user from the current OAuth access token. */
  reload(): Promise<void>;
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    auth: AuthModule;
  }
}
