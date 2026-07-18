import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from "@angular/core";
import type { DocumentNode } from "graphql";
import {
  GEEX_BLOB_CREATE_DOCUMENT,
  GEEX_BLOB_DEFAULT_STORAGE_TYPE,
  GEEX_BLOB_DELETE_DOCUMENT,
  GEEX_BLOB_LIST_DOCUMENT,
} from "./blob.tokens";

export interface GeexBlobStorageOptions {
  readonly defaultStorageType?: string;
  readonly createDocument?: DocumentNode;
  readonly listDocument?: DocumentNode;
  readonly deleteDocument?: DocumentNode;
}

export const GEEX_BLOB_STORAGE_OPTIONS = new InjectionToken<Readonly<GeexBlobStorageOptions>>(
  "GEEX_BLOB_STORAGE_OPTIONS",
);

export function provideGeexBlobStorage(
  options: Readonly<GeexBlobStorageOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_BLOB_STORAGE_OPTIONS, useValue: options },
    ...(options.defaultStorageType
      ? [{ provide: GEEX_BLOB_DEFAULT_STORAGE_TYPE, useValue: options.defaultStorageType }]
      : []),
    ...(options.createDocument
      ? [{ provide: GEEX_BLOB_CREATE_DOCUMENT, useValue: options.createDocument }]
      : []),
    ...(options.listDocument
      ? [{ provide: GEEX_BLOB_LIST_DOCUMENT, useValue: options.listDocument }]
      : []),
    ...(options.deleteDocument
      ? [{ provide: GEEX_BLOB_DELETE_DOCUMENT, useValue: options.deleteDocument }]
      : []),
  ]);
}
