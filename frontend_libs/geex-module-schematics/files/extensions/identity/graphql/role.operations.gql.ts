import * as Types from '../../../shared/graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
import { PageInfo } from '../../../shared/graphql/fragments.gql';
export type roleListsVariables = Types.Exact<{
  skip?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  take?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  filter?: Types.InputMaybe<Types.IRoleFilterInput>;
}>;


export type roleListsResult = { __typename?: 'Query', roles?: { __typename?: 'RolesCollectionSegment', totalCount: number, items?: Array<{ __typename?: 'Role', createdOn: any, name: string, id: string, isStatic: boolean, isDefault: boolean } | null> | null, pageInfo: { __typename?: 'CollectionSegmentInfo', hasPreviousPage: boolean, hasNextPage: boolean } } | null };

export type roleMenusVariables = Types.Exact<{
  filter?: Types.InputMaybe<Types.IRoleFilterInput>;
}>;


export type roleMenusResult = { __typename?: 'Query', roles?: { __typename?: 'RolesCollectionSegment', items?: Array<{ __typename?: 'Role', id: string, name: string } | null> | null } | null };

export type roleByNameVariables = Types.Exact<{
  name?: Types.InputMaybe<Types.Scalars['String']['input']>;
}>;


export type roleByNameResult = { __typename?: 'Query', roles?: { __typename?: 'RolesCollectionSegment', items?: Array<{ __typename?: 'Role', permissions: Array<string>, name: string, createdOn: any, id: string, isStatic: boolean, isDefault: boolean, users: Array<{ __typename?: 'User', id: string }> } | null> | null } | null };

export type roleByIdVariables = Types.Exact<{
  id?: Types.InputMaybe<Types.Scalars['String']['input']>;
}>;


export type roleByIdResult = { __typename?: 'Query', roles?: { __typename?: 'RolesCollectionSegment', items?: Array<{ __typename?: 'Role', permissions: Array<string>, name: string, createdOn: any, id: string, isStatic: boolean, isDefault: boolean, users: Array<{ __typename?: 'User', id: string }> } | null> | null } | null };

export type createRoleVariables = Types.Exact<{
  request: Types.CreateRoleRequest;
}>;


export type createRoleResult = { __typename?: 'Mutation', createRole: { __typename?: 'Role', id: string, createdOn: any, name: string, permissions: Array<string>, users: Array<{ __typename?: 'User', permissions: Array<string>, id: string, username: string, email?: string | null, phoneNumber?: string | null }> } };

export type authorizeVariables = Types.Exact<{
  request: Types.AuthorizeRequest;
}>;


export type authorizeResult = { __typename?: 'Mutation', authorize: boolean };

export type setRoleDefaultVariables = Types.Exact<{
  roleId: Types.Scalars['String']['input'];
}>;


export type setRoleDefaultResult = { __typename?: 'Mutation', setRoleDefault: boolean };

export type deleteRoleVariables = Types.Exact<{
  ids: Array<Types.Scalars['String']['input']> | Types.Scalars['String']['input'];
}>;


export type deleteRoleResult = { __typename?: 'Mutation', deleteRole?: boolean | null };

export type RoleBrief = { __typename?: 'Role', createdOn: any, name: string, id: string, isStatic: boolean, isDefault: boolean };

export type RoleDetail = { __typename?: 'Role', permissions: Array<string>, name: string, createdOn: any, id: string, isStatic: boolean, isDefault: boolean, users: Array<{ __typename?: 'User', id: string }> };

export type RoleMinimal = { __typename?: 'Role', id: string, name: string };

export const RoleBrief = gql`
    fragment RoleBrief on Role {
  createdOn
  name
  id
  isStatic
  isDefault
}
    ` as unknown as DocumentNode<RoleBrief, unknown>;
export const RoleDetail = gql`
    fragment RoleDetail on Role {
  ...RoleBrief
  permissions
  name
  users {
    id
  }
}
    ${RoleBrief}` as unknown as DocumentNode<RoleDetail, unknown>;
export const RoleMinimal = gql`
    fragment RoleMinimal on Role {
  id
  name
}
    ` as unknown as DocumentNode<RoleMinimal, unknown>;
export const roleLists = gql`
    query roleLists($skip: Int, $take: Int, $filter: IRoleFilterInput) {
  roles(skip: $skip, take: $take, filter: $filter) {
    items {
      ...RoleBrief
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${RoleBrief}
${PageInfo}` as unknown as DocumentNode<roleListsResult, roleListsVariables>;
export const roleMenus = gql`
    query roleMenus($filter: IRoleFilterInput) {
  roles(skip: 0, take: 999, filter: $filter) {
    items {
      ...RoleMinimal
    }
  }
}
    ${RoleMinimal}` as unknown as DocumentNode<roleMenusResult, roleMenusVariables>;
export const roleByName = gql`
    query roleByName($name: String) {
  roles(skip: 0, take: 1, filter: {name: {eq: $name}}) {
    items {
      ...RoleDetail
    }
  }
}
    ${RoleDetail}` as unknown as DocumentNode<roleByNameResult, roleByNameVariables>;
export const roleById = gql`
    query roleById($id: String) {
  roles(skip: 0, take: 1, filter: {id: {eq: $id}}) {
    items {
      ...RoleDetail
    }
  }
}
    ${RoleDetail}` as unknown as DocumentNode<roleByIdResult, roleByIdVariables>;
export const createRole = gql`
    mutation createRole($request: CreateRoleRequest!) {
  createRole(request: $request) {
    id
    createdOn
    name
    users {
      id
      username
      email
      phoneNumber
      ... on User {
        permissions
      }
    }
    permissions
  }
}
    ` as unknown as DocumentNode<createRoleResult, createRoleVariables>;
export const authorize = gql`
    mutation authorize($request: AuthorizeRequest!) {
  authorize(request: $request)
}
    ` as unknown as DocumentNode<authorizeResult, authorizeVariables>;
export const setRoleDefault = gql`
    mutation setRoleDefault($roleId: String!) {
  setRoleDefault(request: {roleId: $roleId})
}
    ` as unknown as DocumentNode<setRoleDefaultResult, setRoleDefaultVariables>;
export const deleteRole = gql`
    mutation deleteRole($ids: [String!]!) {
  deleteRole(ids: $ids)
}
    ` as unknown as DocumentNode<deleteRoleResult, deleteRoleVariables>;
