import { Provider } from "@angular/core";

import { GeexStartupService } from "./geex-startup.service";
import { GEEX_STARTUP_OPTIONS } from "./tokens";
import type { GeexStartupOptions } from "./types";

export function provideGeexStartup(options: GeexStartupOptions): Provider[] {
  return [
    { provide: GEEX_STARTUP_OPTIONS, useValue: options },
    GeexStartupService,
  ];
}
