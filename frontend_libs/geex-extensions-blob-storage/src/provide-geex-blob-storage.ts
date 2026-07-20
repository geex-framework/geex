import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import { createBlobStorageModule, type BlobStorageModuleConfig } from "./blob-storage.module";
import type { BlobStorageModule } from "./blob-storage.types";

export interface GeexBlobStorageOptions {
  readonly defaultStorageType?: string;
  readonly createBlobStorageModule?: (
    injector: Injector,
    config: BlobStorageModuleConfig,
  ) => BlobStorageModule;
}

export const GEEX_BLOB_STORAGE_OPTIONS = new InjectionToken<Readonly<GeexBlobStorageOptions>>(
  "GEEX_BLOB_STORAGE_OPTIONS",
);

export function provideGeexBlobStorage(
  options: Readonly<GeexBlobStorageOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_BLOB_STORAGE_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => {
        const config: BlobStorageModuleConfig = {
          defaultStorageType: options.defaultStorageType ?? "Db",
        };
        return {
          blobStorage: (options.createBlobStorageModule ?? createBlobStorageModule)(injector, config),
        };
      },
    }),
  ]);
}
