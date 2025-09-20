import { Injector, Provider } from "@angular/core";
import { configGeex, GeexOverrides, Geex, geex } from "./geex";

export function provideGeex(overrides?: GeexOverrides): Provider[] {
  return [
    {
      provide: Geex, useFactory: (injector: Injector) => {
        configGeex(injector, overrides);
        return geex;
      },
      deps: [Injector],
    },
  ];
}
