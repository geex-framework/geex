import { InjectionToken } from "@angular/core";
import type { DocumentNode } from "graphql";

export const GEEX_BLOB_CREATE_DOCUMENT = new InjectionToken<DocumentNode>("GEEX_BLOB_CREATE_DOCUMENT");

export const GEEX_BLOB_LIST_DOCUMENT = new InjectionToken<DocumentNode>("GEEX_BLOB_LIST_DOCUMENT");

export const GEEX_BLOB_DELETE_DOCUMENT = new InjectionToken<DocumentNode>("GEEX_BLOB_DELETE_DOCUMENT");

export const GEEX_BLOB_DEFAULT_STORAGE_TYPE = new InjectionToken<string>("GEEX_BLOB_DEFAULT_STORAGE_TYPE", {
  providedIn: "root",
  factory: () => "Db",
});
