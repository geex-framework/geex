import type { TypedDocumentNode as DocumentNode } from "@graphql-typed-document-node/core";
import gql from "graphql-tag";
import type * as Types from "../../../shared/graphql/schema.gql";

export type ApprovalFlowTemplateNode = { id: string; name: string; index: number; auditRole: string; carbonCopyUserIds: string[] };
export type ApprovalFlowTemplateDetail = {
  id: string;
  name: string;
  description: string;
  orgCode: string;
  createdOn: unknown;
  modifiedOn: unknown;
  nodes: ApprovalFlowTemplateNode[];
};
export type ApprovalFlowBrief = {
  id: string;
  name: string;
  description?: string | null;
  status: Types.ApprovalFlowStatus;
  orgCode: string;
  activeIndex: number;
  createdOn: unknown;
  modifiedOn: unknown;
};
export type ApprovalFlowsVariables = Types.Exact<{
  filter?: Types.InputMaybe<Types.ApprovalFlowFilterInput>;
  request?: Types.InputMaybe<Types.QueryApprovalFlowRequest>;
  skip?: Types.InputMaybe<Types.Scalars["Int"]["input"]>;
  take?: Types.InputMaybe<Types.Scalars["Int"]["input"]>;
  sort?: Types.InputMaybe<Array<Types.ApprovalFlowSortInput> | Types.ApprovalFlowSortInput>;
}>;
export type ApprovalFlowsResult = { approvalFlow?: { items?: Array<ApprovalFlowBrief | null> | null; totalCount: number } | null };
export type ApprovalFlowTemplatesVariables = Types.Exact<{
  filter?: Types.InputMaybe<Types.ApprovalFlowTemplateFilterInput>;
  request?: Types.InputMaybe<Types.QueryApprovalFlowTemplateRequest>;
  skip?: Types.InputMaybe<Types.Scalars["Int"]["input"]>;
  take?: Types.InputMaybe<Types.Scalars["Int"]["input"]>;
  sort?: Types.InputMaybe<Array<Types.ApprovalFlowTemplateSortInput> | Types.ApprovalFlowTemplateSortInput>;
}>;
export type ApprovalFlowTemplatesResult = { approvalFlowTemplate?: { items?: Array<ApprovalFlowTemplateDetail | null> | null; totalCount: number } | null };
export type ApprovalFlowTemplateByIdVariables = Types.Exact<{ id: Types.Scalars["String"]["input"] }>;
export type ApprovalFlowTemplateByIdResult = { approvalFlowTemplateById?: ApprovalFlowTemplateDetail | null };
export type CreateApprovalFlowTemplateVariables = Types.Exact<{ request: Types.CreateApprovalFlowTemplateRequest }>;
export type EditApprovalFlowTemplateVariables = Types.Exact<{ request: Types.EditApprovalFlowTemplateRequest }>;
export type DeleteApprovalFlowTemplateVariables = Types.Exact<{ ids: Array<string> | string }>;

export const approvalFlows = gql`
  query approvalFlows($filter: ApprovalFlowFilterInput, $request: QueryApprovalFlowRequest, $skip: Int = 0, $take: Int = 10, $sort: [ApprovalFlowSortInput!]) {
    approvalFlow(filter: $filter, request: $request, skip: $skip, take: $take, sort: $sort) {
      items { id name description status orgCode activeIndex createdOn modifiedOn }
      pageInfo { hasPreviousPage hasNextPage }
      totalCount
    }
  }
` as unknown as DocumentNode<ApprovalFlowsResult, ApprovalFlowsVariables>;
export const approvalFlowTemplates = gql`
  query approvalFlowTemplates($filter: ApprovalFlowTemplateFilterInput, $request: QueryApprovalFlowTemplateRequest, $skip: Int = 0, $take: Int = 10, $sort: [ApprovalFlowTemplateSortInput!]) {
    approvalFlowTemplate(filter: $filter, request: $request, skip: $skip, take: $take, sort: $sort) {
      items { id name description orgCode createdOn modifiedOn nodes { id name index auditRole carbonCopyUserIds } }
      pageInfo { hasPreviousPage hasNextPage }
      totalCount
    }
  }
` as unknown as DocumentNode<ApprovalFlowTemplatesResult, ApprovalFlowTemplatesVariables>;
export const approvalFlowTemplateById = gql`
  query approvalFlowTemplateById($id: String!) {
    approvalFlowTemplateById(id: $id) { id name description orgCode createdOn modifiedOn nodes { id name index auditRole carbonCopyUserIds } }
  }
` as unknown as DocumentNode<ApprovalFlowTemplateByIdResult, ApprovalFlowTemplateByIdVariables>;
export const createApprovalFlowTemplate = gql`
  mutation createApprovalFlowTemplate($request: CreateApprovalFlowTemplateRequest!) { createApprovalFlowTemplate(request: $request) { id } }
` as unknown as DocumentNode<{ createApprovalFlowTemplate: { id: string } }, CreateApprovalFlowTemplateVariables>;
export const editApprovalFlowTemplate = gql`
  mutation editApprovalFlowTemplate($request: EditApprovalFlowTemplateRequest!) { editApprovalFlowTemplate(request: $request) { id } }
` as unknown as DocumentNode<{ editApprovalFlowTemplate: { id: string } }, EditApprovalFlowTemplateVariables>;
export const deleteApprovalFlowTemplate = gql`
  mutation deleteApprovalFlowTemplate($ids: [String!]!) { deleteApprovalFlowTemplate(ids: $ids) }
` as unknown as DocumentNode<{ deleteApprovalFlowTemplate: boolean }, DeleteApprovalFlowTemplateVariables>;
