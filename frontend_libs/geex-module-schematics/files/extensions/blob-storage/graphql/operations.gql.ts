import type { TypedDocumentNode as DocumentNode } from "@graphql-typed-document-node/core";
import gql from "graphql-tag";
import type * as Types from "../../../shared/graphql/schema.gql";

export type BlobObjectBrief = {
  id: string;
  fileName?: string | null;
  fileSize: number;
  md5?: string | null;
  mimeType?: string | null;
  storageType: Types.BlobStorageType;
  url?: string | null;
  expireAt?: unknown;
  createdOn: unknown;
};
export type BlobObjectsVariables = Types.Exact<{
  filter?: Types.InputMaybe<Types.IBlobObjectFilterInput>;
  skip?: Types.InputMaybe<Types.Scalars["Int"]["input"]>;
  take?: Types.InputMaybe<Types.Scalars["Int"]["input"]>;
}>;
export type BlobObjectsResult = { blobObjects?: { items?: Array<BlobObjectBrief | null> | null; totalCount: number } | null };
export type CreateBlobObjectVariables = Types.Exact<{ request: Types.CreateBlobObjectRequest }>;
export type CreateBlobObjectResult = { createBlobObject: BlobObjectBrief };
export type DeleteBlobObjectVariables = Types.Exact<{ request: Types.DeleteBlobObjectRequest }>;
export type DeleteBlobObjectResult = { deleteBlobObject: boolean };

export const blobObjects = gql`
  query blobObjects($filter: IBlobObjectFilterInput, $skip: Int = 0, $take: Int = 10) {
    blobObjects(filter: $filter, skip: $skip, take: $take) {
      items { id fileName fileSize md5 mimeType storageType url expireAt createdOn }
      pageInfo { hasPreviousPage hasNextPage }
      totalCount
    }
  }
` as unknown as DocumentNode<BlobObjectsResult, BlobObjectsVariables>;

export const createBlobObject = gql`
  mutation createBlobObject($request: CreateBlobObjectRequest!) {
    createBlobObject(request: $request) { id fileName fileSize md5 mimeType storageType url expireAt createdOn }
  }
` as unknown as DocumentNode<CreateBlobObjectResult, CreateBlobObjectVariables>;

export const deleteBlobObject = gql`
  mutation deleteBlobObject($request: DeleteBlobObjectRequest!) {
    deleteBlobObject(request: $request)
  }
` as unknown as DocumentNode<DeleteBlobObjectResult, DeleteBlobObjectVariables>;
