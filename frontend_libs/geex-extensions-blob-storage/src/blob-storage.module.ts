import { Injector } from "@angular/core";
import { Apollo } from "apollo-angular";
import type { DocumentNode } from "graphql";
import { firstValueFrom } from "rxjs";
import type { BlobStorageModule } from "./blob-storage.types";

export type BlobStorageModuleConfig = {
  readonly defaultStorageType: string;
  readonly createDocument: DocumentNode;
  readonly listDocument: DocumentNode;
  readonly deleteDocument: DocumentNode;
};

export function createBlobStorageModule(
  injector: Injector,
  config: BlobStorageModuleConfig,
): BlobStorageModule {
  const apollo = () => injector.get(Apollo);
  return {
    defaultStorageType: config.defaultStorageType,
    createDocument: config.createDocument,
    listDocument: config.listDocument,
    deleteDocument: config.deleteDocument,
    create: async (variables, context) => {
      const result = await firstValueFrom(
        apollo().mutate({ mutation: config.createDocument, variables, context }),
      );
      return result.data;
    },
    list: async variables => {
      const result = await firstValueFrom(
        apollo().query({ query: config.listDocument, variables }),
      );
      return result.data;
    },
    delete: async (variables, context) => {
      const result = await firstValueFrom(
        apollo().mutate({ mutation: config.deleteDocument, variables, context }),
      );
      return result.data;
    },
    init: async () => undefined,
  };
}
