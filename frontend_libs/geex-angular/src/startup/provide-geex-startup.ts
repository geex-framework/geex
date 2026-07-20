import { inject, provideAppInitializer, type EnvironmentProviders, type Provider } from "@angular/core";

import { GEEX_BLOCK_DEBUGGER } from "../debugger-blocker.tokens";
import { DebuggerBlockerService } from "../debugger-blocker.service";
import { GeexStartupService } from "./geex-startup.service";
import { GEEX_STARTUP_OPTIONS } from "./tokens";
import type { GeexStartupOptions } from "./types";

export function provideGeexStartup(options: GeexStartupOptions): Array<Provider | EnvironmentProviders> {
  return [
    { provide: GEEX_STARTUP_OPTIONS, useValue: options },
    { provide: GEEX_BLOCK_DEBUGGER, useValue: options.blockDebugger ?? false },
    DebuggerBlockerService,
    GeexStartupService,
    provideAppInitializer(() => inject(GeexStartupService).load()),
  ];
}
