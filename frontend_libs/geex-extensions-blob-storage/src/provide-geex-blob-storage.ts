import { EnvironmentProviders, InjectionToken, Injector, makeEnvironmentProviders } from "@angular/core";
import { provideGeexModuleContribution } from "@geexcode/geex-angular";
import type { DocumentNode } from "graphql";
import { createBlobStorageModule, type BlobStorageModuleConfig } from "./blob-storage.module";
import type { BlobStorageModule } from "./blob-storage.types";

export interface GeexBlobStorageOptions {
  readonly defaultStorageType?: string;
  readonly createDocument: DocumentNode;
  readonly listDocument: DocumentNode;
  readonly deleteDocument: DocumentNode;
  readonly createBlobStorageModule?: (
    injector: Injector,
    config: BlobStorageModuleConfig,
  ) => BlobStorageModule;
}

export const GEEX_BLOB_STORAGE_OPTIONS = new InjectionToken<Readonly<GeexBlobStorageOptions>>(
  "GEEX_BLOB_STORAGE_OPTIONS",
);

export function provideGeexBlobStorage(
  options: Readonly<GeexBlobStorageOptions>,
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_BLOB_STORAGE_OPTIONS, useValue: options },
    provideGeexModuleContribution({
      createModules: ({ injector }) => {
        const config: BlobStorageModuleConfig = {
          defaultStorageType: options.defaultStorageType ?? "Db",
          createDocument: options.createDocument,
          listDocument: options.listDocument,
          deleteDocument: options.deleteDocument,
        };
        return {
          blobStorage: (options.createBlobStorageModule ?? createBlobStorageModule)(injector, config),
        };
      },
    }),
  ]);
}
