import { Injector } from "@angular/core";
import { Apollo, gql } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import type { BlobStorageModule } from "./blob-storage.types";

const GQL_BLOB_OBJECTS = gql`
  query blobObjects($filter: IBlobObjectFilterInput, $skip: Int = 0, $take: Int = 10) {
    blobObjects(filter: $filter, skip: $skip, take: $take) {
      items { id fileName fileSize md5 mimeType storageType url expireAt createdOn }
      pageInfo { hasPreviousPage hasNextPage }
      totalCount
    }
  }
`;

const GQL_CREATE_BLOB_OBJECT = gql`
  mutation createBlobObject($request: CreateBlobObjectRequest!) {
    createBlobObject(request: $request) { id fileName fileSize md5 mimeType storageType url expireAt createdOn }
  }
`;

const GQL_DELETE_BLOB_OBJECT = gql`
  mutation deleteBlobObject($request: DeleteBlobObjectRequest!) {
    deleteBlobObject(request: $request)
  }
`;

export type BlobStorageModuleConfig = {
  readonly defaultStorageType: string;
};

export function createBlobStorageModule(
  injector: Injector,
  config: BlobStorageModuleConfig,
): BlobStorageModule {
  const apollo = () => injector.get(Apollo);
  return {
    defaultStorageType: config.defaultStorageType,
    createDocument: GQL_CREATE_BLOB_OBJECT,
    listDocument: GQL_BLOB_OBJECTS,
    deleteDocument: GQL_DELETE_BLOB_OBJECT,
    create: async (variables, context) => {
      const result = await firstValueFrom(
        apollo().mutate({ mutation: GQL_CREATE_BLOB_OBJECT, variables, context }),
      );
      return result.data;
    },
    list: async variables => {
      const result = await firstValueFrom(
        apollo().query({ query: GQL_BLOB_OBJECTS, variables }),
      );
      return result.data;
    },
    delete: async (variables, context) => {
      const result = await firstValueFrom(
        apollo().mutate({ mutation: GQL_DELETE_BLOB_OBJECT, variables, context }),
      );
      return result.data;
    },
    init: async () => undefined,
  };
}
