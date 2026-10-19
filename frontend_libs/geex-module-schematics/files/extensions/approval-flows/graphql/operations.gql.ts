import * as Types from '../../../shared/graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
export type ApprovalFlowNodeStatusBrief = { __typename?: 'ApprovalFlowNodeStatusFlags', isApproved: boolean, isRejected: boolean, isStarted: boolean, isCreated: boolean, isConsulting: boolean, isViewed: boolean, isTransferred: boolean };

export type ApprovalFlowNodeDetail = { __typename?: 'ApprovalFlowNode', id: string, name: string, index: number, auditUserId?: string | null, auditRole?: string | null, carbonCopyUserIds: Array<string>, description?: string | null, isFromTemplate: boolean, approvalComment?: string | null, approvalTime?: any | null, canEditForm: boolean, canOperate: boolean, canReply: boolean, auditUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, formFields: Array<{ __typename?: 'ApprovalFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, options: Array<string> }>, formValues: Array<{ __typename?: 'ApprovalFormValue', key: string, values: Array<string> }>, consultations: Array<{ __typename?: 'ApprovalFlowConsultation', id: string, userId: string, fromUserId: string, message: string, createdAt: any, reply?: string | null, repliedAt?: any | null }>, nodeStatus: { __typename?: 'ApprovalFlowNodeStatusFlags', isApproved: boolean, isRejected: boolean, isStarted: boolean, isCreated: boolean, isConsulting: boolean, isViewed: boolean, isTransferred: boolean }, chatLogs: Array<{ __typename?: 'ApprovalFlowNodeLog', id: string, logType: Types.ApprovalFlowNodeLogType, message: string, creationTime: any, fromUserId: string, toUserId?: string | null, from?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, to?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null }> };

export type ApprovalFlowBrief = { __typename?: 'ApprovalFlow', id: string, name: string, description?: string | null, status: Types.ApprovalFlowStatus, orgCode?: string | null, activeIndex: number, canEdit: boolean, createdOn: any, modifiedOn: any, tags: Array<string>, creatorUserId?: string | null, creatorUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, nodes: Array<{ __typename?: 'ApprovalFlowNode', id: string, index: number, nodeStatus: { __typename?: 'ApprovalFlowNodeStatusFlags', isApproved: boolean, isRejected: boolean, isStarted: boolean, isCreated: boolean, isConsulting: boolean, isViewed: boolean, isTransferred: boolean } }> };

export type ApprovalFlowDetail = { __typename?: 'ApprovalFlow', templateId?: string | null, templateName?: string | null, associatedEntityType?: Types.ApprovalFlowAssociatedEntityType | null, associatedEntityId?: string | null, canEditForm: boolean, archivedAt?: any | null, replacedByFlowId?: string | null, id: string, name: string, description?: string | null, status: Types.ApprovalFlowStatus, orgCode?: string | null, activeIndex: number, canEdit: boolean, createdOn: any, modifiedOn: any, tags: Array<string>, creatorUserId?: string | null, associatedEntitySummary?: { __typename?: 'ApprovalFlowEntitySummary', id: string, name: string, typeName: string, approveStatus: Types.ApproveStatus, detailUrl?: string | null } | null, formFields: Array<{ __typename?: 'ApprovalFlowFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, allowEdit: boolean, options: Array<string> }>, formValues: Array<{ __typename?: 'ApprovalFormValue', key: string, values: Array<string> }>, activeNode?: { __typename?: 'ApprovalFlowNode', id: string, name: string, index: number, auditUserId?: string | null, auditRole?: string | null, carbonCopyUserIds: Array<string>, description?: string | null, isFromTemplate: boolean, approvalComment?: string | null, approvalTime?: any | null, canEditForm: boolean, canOperate: boolean, canReply: boolean, auditUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, formFields: Array<{ __typename?: 'ApprovalFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, options: Array<string> }>, formValues: Array<{ __typename?: 'ApprovalFormValue', key: string, values: Array<string> }>, consultations: Array<{ __typename?: 'ApprovalFlowConsultation', id: string, userId: string, fromUserId: string, message: string, createdAt: any, reply?: string | null, repliedAt?: any | null }>, nodeStatus: { __typename?: 'ApprovalFlowNodeStatusFlags', isApproved: boolean, isRejected: boolean, isStarted: boolean, isCreated: boolean, isConsulting: boolean, isViewed: boolean, isTransferred: boolean }, chatLogs: Array<{ __typename?: 'ApprovalFlowNodeLog', id: string, logType: Types.ApprovalFlowNodeLogType, message: string, creationTime: any, fromUserId: string, toUserId?: string | null, from?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, to?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null }> } | null, nodes: Array<{ __typename?: 'ApprovalFlowNode', id: string, index: number, name: string, auditUserId?: string | null, auditRole?: string | null, carbonCopyUserIds: Array<string>, description?: string | null, isFromTemplate: boolean, approvalComment?: string | null, approvalTime?: any | null, canEditForm: boolean, canOperate: boolean, canReply: boolean, nodeStatus: { __typename?: 'ApprovalFlowNodeStatusFlags', isApproved: boolean, isRejected: boolean, isStarted: boolean, isCreated: boolean, isConsulting: boolean, isViewed: boolean, isTransferred: boolean }, auditUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, formFields: Array<{ __typename?: 'ApprovalFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, options: Array<string> }>, formValues: Array<{ __typename?: 'ApprovalFormValue', key: string, values: Array<string> }>, consultations: Array<{ __typename?: 'ApprovalFlowConsultation', id: string, userId: string, fromUserId: string, message: string, createdAt: any, reply?: string | null, repliedAt?: any | null }>, chatLogs: Array<{ __typename?: 'ApprovalFlowNodeLog', id: string, logType: Types.ApprovalFlowNodeLogType, message: string, creationTime: any, fromUserId: string, toUserId?: string | null, from?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, to?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null }> }>, creatorUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null };

export type ApprovalFlowTemplateDetail = { __typename?: 'ApprovalFlowTemplate', id: string, name: string, description: string, orgCode?: string | null, createdOn: any, modifiedOn: any, tags: Array<string>, formFields: Array<{ __typename?: 'ApprovalFlowFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, allowEdit: boolean, options: Array<string> }>, nodes: Array<{ __typename?: 'ApprovalFlowNodeTemplate', id: string, name: string, index: number, auditRole: string, auditUserId?: string | null, description?: string | null, carbonCopyUserIds: Array<string>, formFields: Array<{ __typename?: 'ApprovalFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, options: Array<string> }> }> };

export type approvalFlowsVariables = Types.Exact<{
  filter?: Types.InputMaybe<Types.ApprovalFlowFilterInput>;
  request?: Types.InputMaybe<Types.QueryApprovalFlowRequest>;
  skip?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  take?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  sort?: Types.InputMaybe<Array<Types.ApprovalFlowSortInput> | Types.ApprovalFlowSortInput>;
}>;


export type approvalFlowsResult = { __typename?: 'Query', approvalFlow?: { __typename?: 'ApprovalFlowCollectionSegment', totalCount: number, items?: Array<{ __typename?: 'ApprovalFlow', id: string, name: string, description?: string | null, status: Types.ApprovalFlowStatus, orgCode?: string | null, activeIndex: number, canEdit: boolean, createdOn: any, modifiedOn: any, tags: Array<string>, creatorUserId?: string | null, creatorUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, nodes: Array<{ __typename?: 'ApprovalFlowNode', id: string, index: number, nodeStatus: { __typename?: 'ApprovalFlowNodeStatusFlags', isApproved: boolean, isRejected: boolean, isStarted: boolean, isCreated: boolean, isConsulting: boolean, isViewed: boolean, isTransferred: boolean } }> } | null> | null, pageInfo: { __typename?: 'CollectionSegmentInfo', hasPreviousPage: boolean, hasNextPage: boolean } } | null };

export type approvalFlowTemplatesVariables = Types.Exact<{
  filter?: Types.InputMaybe<Types.ApprovalFlowTemplateFilterInput>;
  request?: Types.InputMaybe<Types.QueryApprovalFlowTemplateRequest>;
  skip?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  take?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  sort?: Types.InputMaybe<Array<Types.ApprovalFlowTemplateSortInput> | Types.ApprovalFlowTemplateSortInput>;
}>;


export type approvalFlowTemplatesResult = { __typename?: 'Query', approvalFlowTemplate?: { __typename?: 'ApprovalFlowTemplateCollectionSegment', totalCount: number, items?: Array<{ __typename?: 'ApprovalFlowTemplate', id: string, name: string, description: string, orgCode?: string | null, createdOn: any, modifiedOn: any, tags: Array<string>, formFields: Array<{ __typename?: 'ApprovalFlowFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, allowEdit: boolean, options: Array<string> }>, nodes: Array<{ __typename?: 'ApprovalFlowNodeTemplate', id: string, name: string, index: number, auditRole: string, auditUserId?: string | null, description?: string | null, carbonCopyUserIds: Array<string>, formFields: Array<{ __typename?: 'ApprovalFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, options: Array<string> }> }> } | null> | null, pageInfo: { __typename?: 'CollectionSegmentInfo', hasPreviousPage: boolean, hasNextPage: boolean } } | null };

export type approvalFlowTemplateByIdVariables = Types.Exact<{
  id: Types.Scalars['String']['input'];
}>;


export type approvalFlowTemplateByIdResult = { __typename?: 'Query', approvalFlowTemplateById?: { __typename?: 'ApprovalFlowTemplate', id: string, name: string, description: string, orgCode?: string | null, createdOn: any, modifiedOn: any, tags: Array<string>, formFields: Array<{ __typename?: 'ApprovalFlowFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, allowEdit: boolean, options: Array<string> }>, nodes: Array<{ __typename?: 'ApprovalFlowNodeTemplate', id: string, name: string, index: number, auditRole: string, auditUserId?: string | null, description?: string | null, carbonCopyUserIds: Array<string>, formFields: Array<{ __typename?: 'ApprovalFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, options: Array<string> }> }> } | null };

export type createApprovalFlowTemplateVariables = Types.Exact<{
  request: Types.CreateApprovalFlowTemplateRequest;
}>;


export type createApprovalFlowTemplateResult = { __typename?: 'Mutation', createApprovalFlowTemplate: { __typename?: 'ApprovalFlowTemplate', id: string } };

export type editApprovalFlowTemplateVariables = Types.Exact<{
  request: Types.EditApprovalFlowTemplateRequest;
}>;


export type editApprovalFlowTemplateResult = { __typename?: 'Mutation', editApprovalFlowTemplate: { __typename?: 'ApprovalFlowTemplate', id: string } };

export type deleteApprovalFlowTemplateVariables = Types.Exact<{
  ids: Array<Types.Scalars['String']['input']> | Types.Scalars['String']['input'];
}>;


export type deleteApprovalFlowTemplateResult = { __typename?: 'Mutation', deleteApprovalFlowTemplate: boolean };

export type approvalFlowByIdVariables = Types.Exact<{
  id: Types.Scalars['String']['input'];
}>;


export type approvalFlowByIdResult = { __typename?: 'Query', approvalFlowById?: { __typename?: 'ApprovalFlow', templateId?: string | null, templateName?: string | null, associatedEntityType?: Types.ApprovalFlowAssociatedEntityType | null, associatedEntityId?: string | null, canEditForm: boolean, archivedAt?: any | null, replacedByFlowId?: string | null, id: string, name: string, description?: string | null, status: Types.ApprovalFlowStatus, orgCode?: string | null, activeIndex: number, canEdit: boolean, createdOn: any, modifiedOn: any, tags: Array<string>, creatorUserId?: string | null, associatedEntitySummary?: { __typename?: 'ApprovalFlowEntitySummary', id: string, name: string, typeName: string, approveStatus: Types.ApproveStatus, detailUrl?: string | null } | null, formFields: Array<{ __typename?: 'ApprovalFlowFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, allowEdit: boolean, options: Array<string> }>, formValues: Array<{ __typename?: 'ApprovalFormValue', key: string, values: Array<string> }>, activeNode?: { __typename?: 'ApprovalFlowNode', id: string, name: string, index: number, auditUserId?: string | null, auditRole?: string | null, carbonCopyUserIds: Array<string>, description?: string | null, isFromTemplate: boolean, approvalComment?: string | null, approvalTime?: any | null, canEditForm: boolean, canOperate: boolean, canReply: boolean, auditUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, formFields: Array<{ __typename?: 'ApprovalFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, options: Array<string> }>, formValues: Array<{ __typename?: 'ApprovalFormValue', key: string, values: Array<string> }>, consultations: Array<{ __typename?: 'ApprovalFlowConsultation', id: string, userId: string, fromUserId: string, message: string, createdAt: any, reply?: string | null, repliedAt?: any | null }>, nodeStatus: { __typename?: 'ApprovalFlowNodeStatusFlags', isApproved: boolean, isRejected: boolean, isStarted: boolean, isCreated: boolean, isConsulting: boolean, isViewed: boolean, isTransferred: boolean }, chatLogs: Array<{ __typename?: 'ApprovalFlowNodeLog', id: string, logType: Types.ApprovalFlowNodeLogType, message: string, creationTime: any, fromUserId: string, toUserId?: string | null, from?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, to?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null }> } | null, nodes: Array<{ __typename?: 'ApprovalFlowNode', id: string, index: number, name: string, auditUserId?: string | null, auditRole?: string | null, carbonCopyUserIds: Array<string>, description?: string | null, isFromTemplate: boolean, approvalComment?: string | null, approvalTime?: any | null, canEditForm: boolean, canOperate: boolean, canReply: boolean, nodeStatus: { __typename?: 'ApprovalFlowNodeStatusFlags', isApproved: boolean, isRejected: boolean, isStarted: boolean, isCreated: boolean, isConsulting: boolean, isViewed: boolean, isTransferred: boolean }, auditUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, formFields: Array<{ __typename?: 'ApprovalFormField', key: string, name: string, type: Types.ApprovalFormFieldType, required: boolean, options: Array<string> }>, formValues: Array<{ __typename?: 'ApprovalFormValue', key: string, values: Array<string> }>, consultations: Array<{ __typename?: 'ApprovalFlowConsultation', id: string, userId: string, fromUserId: string, message: string, createdAt: any, reply?: string | null, repliedAt?: any | null }>, chatLogs: Array<{ __typename?: 'ApprovalFlowNodeLog', id: string, logType: Types.ApprovalFlowNodeLogType, message: string, creationTime: any, fromUserId: string, toUserId?: string | null, from?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null, to?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null }> }>, creatorUser?: { __typename?: 'User', id: string, username: string, nickname?: string | null } | null } | null };

export type approveApprovalFlowNodeVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
  message: Types.Scalars['String']['input'];
}>;


export type approveApprovalFlowNodeResult = { __typename?: 'Mutation', approveApprovalFlowNode: { __typename?: 'ApprovalFlowNode', id: string, name: string, index: number } };

export type rejectApprovalFlowNodeVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
  message: Types.Scalars['String']['input'];
  targetNodeId: Types.Scalars['String']['input'];
}>;


export type rejectApprovalFlowNodeResult = { __typename?: 'Mutation', rejectApprovalFlowNode: { __typename?: 'ApprovalFlowNode', id: string, name: string, index: number } };

export type closeApprovalFlowVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
}>;


export type closeApprovalFlowResult = { __typename?: 'Mutation', closeApprovalFlow: boolean };

export type cancelApprovalFlowVariables = Types.Exact<{
  id: Types.Scalars['String']['input'];
}>;


export type cancelApprovalFlowResult = { __typename?: 'Mutation', cancelApprovalFlow: boolean };

export type editApprovalFlowVariables = Types.Exact<{
  request: Types.EditApprovalFlowRequest;
}>;


export type editApprovalFlowResult = { __typename?: 'Mutation', editApprovalFlow: { __typename?: 'ApprovalFlow', id: string } };

export type approvalFlowTagsVariables = Types.Exact<{ [key: string]: never; }>;


export type approvalFlowTagsResult = { __typename?: 'Query', approvalFlowTags: Array<string> };

export type saveApprovalFlowTagsVariables = Types.Exact<{
  tags: Array<Types.Scalars['String']['input']> | Types.Scalars['String']['input'];
}>;


export type saveApprovalFlowTagsResult = { __typename?: 'Mutation', saveApprovalFlowTags: Array<string> };

export type createApprovalFlowVariables = Types.Exact<{
  request: Types.CreateApprovalFlowRequest;
}>;


export type createApprovalFlowResult = { __typename?: 'Mutation', createApprovalFlow: { __typename?: 'ApprovalFlow', id: string, name: string, status: Types.ApprovalFlowStatus } };

export type approvalFlowByEntityVariables = Types.Exact<{
  entityType: Types.ApprovalFlowAssociatedEntityType;
  entityId: Types.Scalars['String']['input'];
}>;


export type approvalFlowByEntityResult = { __typename?: 'Query', approvalFlowByEntity?: { __typename?: 'ApprovalFlow', id: string, name: string, status: Types.ApprovalFlowStatus, activeIndex: number } | null };

export type saveApprovalFlowFormVariables = Types.Exact<{
  id: Types.Scalars['String']['input'];
  values: Array<Types.ApprovalFormValueInput> | Types.ApprovalFormValueInput;
}>;


export type saveApprovalFlowFormResult = { __typename?: 'Mutation', saveApprovalFlowForm: { __typename?: 'ApprovalFlow', id: string } };

export type saveApprovalFlowNodeFormVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
  values: Array<Types.ApprovalFormValueInput> | Types.ApprovalFormValueInput;
}>;


export type saveApprovalFlowNodeFormResult = { __typename?: 'Mutation', saveApprovalFlowNodeForm: { __typename?: 'ApprovalFlowNode', id: string } };

export type consultApprovalFlowNodeUsersVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
  userIds: Array<Types.Scalars['String']['input']> | Types.Scalars['String']['input'];
  message: Types.Scalars['String']['input'];
}>;


export type consultApprovalFlowNodeUsersResult = { __typename?: 'Mutation', consultApprovalFlowNodeUsers: { __typename?: 'ApprovalFlowNode', id: string } };

export type replyApprovalFlowNodeVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
  message: Types.Scalars['String']['input'];
}>;


export type replyApprovalFlowNodeResult = { __typename?: 'Mutation', replyApprovalFlowNode: { __typename?: 'ApprovalFlowNode', id: string } };

export type transferApprovalFlowNodeVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
  userId: Types.Scalars['String']['input'];
}>;


export type transferApprovalFlowNodeResult = { __typename?: 'Mutation', transferApprovalFlowNode: { __typename?: 'ApprovalFlowNode', id: string } };

export type carbonCopyApprovalFlowNodeVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
  userId: Types.Scalars['String']['input'];
}>;


export type carbonCopyApprovalFlowNodeResult = { __typename?: 'Mutation', carbonCopyApprovalFlowNode: { __typename?: 'ApprovalFlowNode', id: string } };

export type markApprovalFlowNodeViewedVariables = Types.Exact<{
  nodeId: Types.Scalars['String']['input'];
}>;


export type markApprovalFlowNodeViewedResult = { __typename?: 'Mutation', markApprovalFlowNodeViewed: boolean };

export const ApprovalFlowNodeStatusBrief = gql`
    fragment ApprovalFlowNodeStatusBrief on ApprovalFlowNodeStatusFlags {
  isApproved
  isRejected
  isStarted
  isCreated
  isConsulting
  isViewed
  isTransferred
}
    ` as unknown as DocumentNode<ApprovalFlowNodeStatusBrief, unknown>;
export const ApprovalFlowBrief = gql`
    fragment ApprovalFlowBrief on ApprovalFlow {
  id
  name
  description
  status
  orgCode
  activeIndex
  canEdit
  createdOn
  modifiedOn
  tags
  creatorUserId
  creatorUser {
    id
    username
    nickname
  }
  nodes {
    id
    index
    nodeStatus {
      ...ApprovalFlowNodeStatusBrief
    }
  }
}
    ${ApprovalFlowNodeStatusBrief}` as unknown as DocumentNode<ApprovalFlowBrief, unknown>;
export const ApprovalFlowNodeDetail = gql`
    fragment ApprovalFlowNodeDetail on ApprovalFlowNode {
  id
  name
  index
  auditUserId
  auditRole
  carbonCopyUserIds
  description
  isFromTemplate
  approvalComment
  approvalTime
  canEditForm
  canOperate
  canReply
  auditUser {
    id
    username
    nickname
  }
  formFields {
    key
    name
    type
    required
    options
  }
  formValues {
    key
    values
  }
  consultations {
    id
    userId
    fromUserId
    message
    createdAt
    reply
    repliedAt
  }
  nodeStatus {
    ...ApprovalFlowNodeStatusBrief
  }
  chatLogs {
    id
    logType
    message
    creationTime
    fromUserId
    toUserId
    from {
      id
      username
      nickname
    }
    to {
      id
      username
      nickname
    }
  }
}
    ${ApprovalFlowNodeStatusBrief}` as unknown as DocumentNode<ApprovalFlowNodeDetail, unknown>;
export const ApprovalFlowDetail = gql`
    fragment ApprovalFlowDetail on ApprovalFlow {
  ...ApprovalFlowBrief
  templateId
  templateName
  associatedEntityType
  associatedEntityId
  associatedEntitySummary {
    id
    name
    typeName
    approveStatus
    detailUrl
  }
  canEditForm
  archivedAt
  replacedByFlowId
  formFields {
    key
    name
    type
    required
    allowEdit
    options
  }
  formValues {
    key
    values
  }
  activeNode {
    ...ApprovalFlowNodeDetail
  }
  nodes {
    ...ApprovalFlowNodeDetail
  }
}
    ${ApprovalFlowBrief}
${ApprovalFlowNodeDetail}` as unknown as DocumentNode<ApprovalFlowDetail, unknown>;
export const ApprovalFlowTemplateDetail = gql`
    fragment ApprovalFlowTemplateDetail on ApprovalFlowTemplate {
  id
  name
  description
  orgCode
  createdOn
  modifiedOn
  tags
  formFields {
    key
    name
    type
    required
    allowEdit
    options
  }
  nodes {
    id
    name
    index
    auditRole
    auditUserId
    description
    formFields {
      key
      name
      type
      required
      options
    }
    carbonCopyUserIds
  }
}
    ` as unknown as DocumentNode<ApprovalFlowTemplateDetail, unknown>;
export const approvalFlows = gql`
    query approvalFlows($filter: ApprovalFlowFilterInput, $request: QueryApprovalFlowRequest, $skip: Int = 0, $take: Int = 10, $sort: [ApprovalFlowSortInput!]) {
  approvalFlow(
    filter: $filter
    request: $request
    skip: $skip
    take: $take
    sort: $sort
  ) {
    items {
      ...ApprovalFlowBrief
    }
    pageInfo {
      hasPreviousPage
      hasNextPage
    }
    totalCount
  }
}
    ${ApprovalFlowBrief}` as unknown as DocumentNode<approvalFlowsResult, approvalFlowsVariables>;
export const approvalFlowTemplates = gql`
    query approvalFlowTemplates($filter: ApprovalFlowTemplateFilterInput, $request: QueryApprovalFlowTemplateRequest, $skip: Int = 0, $take: Int = 10, $sort: [ApprovalFlowTemplateSortInput!]) {
  approvalFlowTemplate(
    filter: $filter
    request: $request
    skip: $skip
    take: $take
    sort: $sort
  ) {
    items {
      ...ApprovalFlowTemplateDetail
    }
    pageInfo {
      hasPreviousPage
      hasNextPage
    }
    totalCount
  }
}
    ${ApprovalFlowTemplateDetail}` as unknown as DocumentNode<approvalFlowTemplatesResult, approvalFlowTemplatesVariables>;
export const approvalFlowTemplateById = gql`
    query approvalFlowTemplateById($id: String!) {
  approvalFlowTemplateById(id: $id) {
    ...ApprovalFlowTemplateDetail
  }
}
    ${ApprovalFlowTemplateDetail}` as unknown as DocumentNode<approvalFlowTemplateByIdResult, approvalFlowTemplateByIdVariables>;
export const createApprovalFlowTemplate = gql`
    mutation createApprovalFlowTemplate($request: CreateApprovalFlowTemplateRequest!) {
  createApprovalFlowTemplate(request: $request) {
    id
  }
}
    ` as unknown as DocumentNode<createApprovalFlowTemplateResult, createApprovalFlowTemplateVariables>;
export const editApprovalFlowTemplate = gql`
    mutation editApprovalFlowTemplate($request: EditApprovalFlowTemplateRequest!) {
  editApprovalFlowTemplate(request: $request) {
    id
  }
}
    ` as unknown as DocumentNode<editApprovalFlowTemplateResult, editApprovalFlowTemplateVariables>;
export const deleteApprovalFlowTemplate = gql`
    mutation deleteApprovalFlowTemplate($ids: [String!]!) {
  deleteApprovalFlowTemplate(ids: $ids)
}
    ` as unknown as DocumentNode<deleteApprovalFlowTemplateResult, deleteApprovalFlowTemplateVariables>;
export const approvalFlowById = gql`
    query approvalFlowById($id: String!) {
  approvalFlowById(id: $id) {
    ...ApprovalFlowDetail
  }
}
    ${ApprovalFlowDetail}` as unknown as DocumentNode<approvalFlowByIdResult, approvalFlowByIdVariables>;
export const approveApprovalFlowNode = gql`
    mutation approveApprovalFlowNode($nodeId: String!, $message: String!) {
  approveApprovalFlowNode(nodeId: $nodeId, message: $message) {
    id
    name
    index
  }
}
    ` as unknown as DocumentNode<approveApprovalFlowNodeResult, approveApprovalFlowNodeVariables>;
export const rejectApprovalFlowNode = gql`
    mutation rejectApprovalFlowNode($nodeId: String!, $message: String!, $targetNodeId: String!) {
  rejectApprovalFlowNode(
    nodeId: $nodeId
    message: $message
    targetNodeId: $targetNodeId
  ) {
    id
    name
    index
  }
}
    ` as unknown as DocumentNode<rejectApprovalFlowNodeResult, rejectApprovalFlowNodeVariables>;
export const closeApprovalFlow = gql`
    mutation closeApprovalFlow($nodeId: String!) {
  closeApprovalFlow(nodeId: $nodeId)
}
    ` as unknown as DocumentNode<closeApprovalFlowResult, closeApprovalFlowVariables>;
export const cancelApprovalFlow = gql`
    mutation cancelApprovalFlow($id: String!) {
  cancelApprovalFlow(id: $id)
}
    ` as unknown as DocumentNode<cancelApprovalFlowResult, cancelApprovalFlowVariables>;
export const editApprovalFlow = gql`
    mutation editApprovalFlow($request: EditApprovalFlowRequest!) {
  editApprovalFlow(request: $request) {
    id
  }
}
    ` as unknown as DocumentNode<editApprovalFlowResult, editApprovalFlowVariables>;
export const approvalFlowTags = gql`
    query approvalFlowTags {
  approvalFlowTags
}
    ` as unknown as DocumentNode<approvalFlowTagsResult, approvalFlowTagsVariables>;
export const saveApprovalFlowTags = gql`
    mutation saveApprovalFlowTags($tags: [String!]!) {
  saveApprovalFlowTags(tags: $tags)
}
    ` as unknown as DocumentNode<saveApprovalFlowTagsResult, saveApprovalFlowTagsVariables>;
export const createApprovalFlow = gql`
    mutation createApprovalFlow($request: CreateApprovalFlowRequest!) {
  createApprovalFlow(request: $request) {
    id
    name
    status
  }
}
    ` as unknown as DocumentNode<createApprovalFlowResult, createApprovalFlowVariables>;
export const approvalFlowByEntity = gql`
    query approvalFlowByEntity($entityType: ApprovalFlowAssociatedEntityType!, $entityId: String!) {
  approvalFlowByEntity(entityType: $entityType, entityId: $entityId) {
    id
    name
    status
    activeIndex
  }
}
    ` as unknown as DocumentNode<approvalFlowByEntityResult, approvalFlowByEntityVariables>;
export const saveApprovalFlowForm = gql`
    mutation saveApprovalFlowForm($id: String!, $values: [ApprovalFormValueInput!]!) {
  saveApprovalFlowForm(id: $id, values: $values) {
    id
  }
}
    ` as unknown as DocumentNode<saveApprovalFlowFormResult, saveApprovalFlowFormVariables>;
export const saveApprovalFlowNodeForm = gql`
    mutation saveApprovalFlowNodeForm($nodeId: String!, $values: [ApprovalFormValueInput!]!) {
  saveApprovalFlowNodeForm(nodeId: $nodeId, values: $values) {
    id
  }
}
    ` as unknown as DocumentNode<saveApprovalFlowNodeFormResult, saveApprovalFlowNodeFormVariables>;
export const consultApprovalFlowNodeUsers = gql`
    mutation consultApprovalFlowNodeUsers($nodeId: String!, $userIds: [String!]!, $message: String!) {
  consultApprovalFlowNodeUsers(
    nodeId: $nodeId
    userIds: $userIds
    message: $message
  ) {
    id
  }
}
    ` as unknown as DocumentNode<consultApprovalFlowNodeUsersResult, consultApprovalFlowNodeUsersVariables>;
export const replyApprovalFlowNode = gql`
    mutation replyApprovalFlowNode($nodeId: String!, $message: String!) {
  replyApprovalFlowNode(nodeId: $nodeId, message: $message) {
    id
  }
}
    ` as unknown as DocumentNode<replyApprovalFlowNodeResult, replyApprovalFlowNodeVariables>;
export const transferApprovalFlowNode = gql`
    mutation transferApprovalFlowNode($nodeId: String!, $userId: String!) {
  transferApprovalFlowNode(nodeId: $nodeId, userId: $userId) {
    id
  }
}
    ` as unknown as DocumentNode<transferApprovalFlowNodeResult, transferApprovalFlowNodeVariables>;
export const carbonCopyApprovalFlowNode = gql`
    mutation carbonCopyApprovalFlowNode($nodeId: String!, $userId: String!) {
  carbonCopyApprovalFlowNode(nodeId: $nodeId, userId: $userId) {
    id
  }
}
    ` as unknown as DocumentNode<carbonCopyApprovalFlowNodeResult, carbonCopyApprovalFlowNodeVariables>;
export const markApprovalFlowNodeViewed = gql`
    mutation markApprovalFlowNodeViewed($nodeId: String!) {
  markApprovalFlowNodeViewed(nodeId: $nodeId)
}
    ` as unknown as DocumentNode<markApprovalFlowNodeViewedResult, markApprovalFlowNodeViewedVariables>;