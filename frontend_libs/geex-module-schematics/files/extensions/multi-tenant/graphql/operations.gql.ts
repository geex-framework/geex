import * as Types from '../../../shared/graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
import { PageInfo } from '../../../shared/graphql/fragments.gql';
export type tenantsVariables = Types.Exact<{
  skip?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  take?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  filter?: Types.InputMaybe<Types.ITenantFilterInput>;
}>;


export type tenantsResult = { __typename?: 'Query', tenants?: { __typename?: 'TenantsCollectionSegment', totalCount: number, items?: Array<{ __typename?: 'Tenant', code: string, name: string, isEnabled: boolean, id: string, createdOn: any }> | null, pageInfo: { __typename?: 'CollectionSegmentInfo', hasPreviousPage: boolean, hasNextPage: boolean } } | null };

export type toggleTenantAvailabilityVariables = Types.Exact<{
  code: Types.Scalars['String']['input'];
}>;


export type toggleTenantAvailabilityResult = { __typename?: 'Mutation', toggleTenantAvailability: boolean };

export type editTenantVariables = Types.Exact<{
  code: Types.Scalars['String']['input'];
  name: Types.Scalars['String']['input'];
}>;


export type editTenantResult = { __typename?: 'Mutation', editTenant: { __typename?: 'Tenant', id: string } };

export type createTenantVariables = Types.Exact<{
  code: Types.Scalars['String']['input'];
  name: Types.Scalars['String']['input'];
}>;


export type createTenantResult = { __typename?: 'Mutation', createTenant: { __typename?: 'Tenant', code: string, isEnabled: boolean, name: string, id: string } };


export const tenants = gql`
    query tenants($skip: Int, $take: Int, $filter: ITenantFilterInput) {
  tenants(skip: $skip, take: $take, filter: $filter) {
    items {
      code
      name
      isEnabled
      id
      createdOn
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${PageInfo}` as unknown as DocumentNode<tenantsResult, tenantsVariables>;
export const toggleTenantAvailability = gql`
    mutation toggleTenantAvailability($code: String!) {
  toggleTenantAvailability(request: {code: $code})
}
    ` as unknown as DocumentNode<toggleTenantAvailabilityResult, toggleTenantAvailabilityVariables>;
export const editTenant = gql`
    mutation editTenant($code: String!, $name: String!) {
  editTenant(request: {code: $code, name: $name}) {
    id
  }
}
    ` as unknown as DocumentNode<editTenantResult, editTenantVariables>;
export const createTenant = gql`
    mutation createTenant($code: String!, $name: String!) {
  createTenant(request: {code: $code, name: $name}) {
    code
    isEnabled
    name
    id
  }
}
    ` as unknown as DocumentNode<createTenantResult, createTenantVariables>;