import type { DocumentNode } from "graphql";
import type { GeexModule } from "@geexcode/geex-angular";

export interface BlobStorageModule extends GeexModule<{
  defaultStorageType: string;
  createDocument: DocumentNode;
  listDocument: DocumentNode;
  deleteDocument: DocumentNode;
  create(variables: Record<string, unknown>, context?: Record<string, unknown>): Promise<unknown>;
  list(variables: Record<string, unknown>): Promise<unknown>;
  delete(variables: Record<string, unknown>, context?: Record<string, unknown>): Promise<unknown>;
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    blobStorage: BlobStorageModule;
  }
}
