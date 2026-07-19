import * as Types from '../../../shared/graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
import { BlobObjectBrief, PageInfo } from '../../../shared/graphql/fragments.gql';
export type userListsVariables = Types.Exact<{
  skip?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  take?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  filter?: Types.InputMaybe<Types.IUserFilterInput>;
  sort?: Types.InputMaybe<Array<Types.IUserSortInput> | Types.IUserSortInput>;
}>;


export type userListsResult = { __typename?: 'Query', users?: { __typename?: 'UsersCollectionSegment', totalCount: number, items?: Array<{ __typename?: 'User', createdOn: any, orgCodes: Array<string>, id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, isEnable: boolean, roleNames: Array<string>, roleIds: Array<string> } | null> | null, pageInfo: { __typename?: 'CollectionSegmentInfo', hasPreviousPage: boolean, hasNextPage: boolean } } | null };

export type userByIdVariables = Types.Exact<{
  id?: Types.InputMaybe<Types.Scalars['String']['input']>;
}>;


export type userByIdResult = { __typename?: 'Query', users?: { __typename?: 'UsersCollectionSegment', items?: Array<{ __typename?: 'User', isEnable: boolean, permissions: Array<string>, orgCodes: Array<string>, avatarFileId?: string | null, id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, roleNames: Array<string>, roleIds: Array<string>, avatarFile?: { __typename?: 'BlobObject', url?: string | null, id: string, createdOn: any, fileSize: any, mimeType?: string | null, storageType: Types.BlobStorageType, fileName?: string | null, md5?: string | null } | null, orgs: Array<{ __typename?: 'Org', name: string, code: string, allParentOrgs: Array<{ __typename?: 'Org', code: string, name: string }> }>, claims: Array<{ __typename?: 'UserClaim', claimType: string, claimValue: string }> } | null> | null } | null };

export type userMenusVariables = Types.Exact<{
  filter?: Types.InputMaybe<Types.IUserFilterInput>;
}>;


export type userMenusResult = { __typename?: 'Query', users?: { __typename?: 'UsersCollectionSegment', items?: Array<{ __typename?: 'User', id: string, username: string, nickname?: string | null } | null> | null } | null };

export type editUserVariables = Types.Exact<{
  request: Types.EditUserRequest;
}>;


export type editUserResult = { __typename?: 'Mutation', editUser: { __typename?: 'User', id: string } };

export type createUserVariables = Types.Exact<{
  request: Types.CreateUserRequest;
}>;


export type createUserResult = { __typename?: 'Mutation', createUser: { __typename?: 'User', id: string } };

export type resetUserPasswordVariables = Types.Exact<{
  request: Types.ResetUserPasswordRequest;
}>;


export type resetUserPasswordResult = { __typename?: 'Mutation', resetUserPassword: { __typename?: 'User', id: string } };

export type changePasswordVariables = Types.Exact<{
  request: Types.ChangePasswordRequest;
}>;


export type changePasswordResult = { __typename?: 'Mutation', changePassword: boolean };

export type deleteUserVariables = Types.Exact<{
  ids: Array<Types.Scalars['String']['input']> | Types.Scalars['String']['input'];
}>;


export type deleteUserResult = { __typename?: 'Mutation', deleteUser?: boolean | null };

export type UserBrief = { __typename?: 'User', id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, isEnable: boolean, roleNames: Array<string>, roleIds: Array<string> };

export type UserList = { __typename?: 'User', createdOn: any, orgCodes: Array<string>, id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, isEnable: boolean, roleNames: Array<string>, roleIds: Array<string> };

export type UserCacheDto = { __typename?: 'User', id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, isEnable: boolean, roleNames: Array<string>, roleIds: Array<string>, avatarFile?: { __typename?: 'BlobObject', url?: string | null } | null };

export type UserMinimal = { __typename?: 'User', id: string, username: string, nickname?: string | null };

export type UserDetail = { __typename?: 'User', isEnable: boolean, permissions: Array<string>, orgCodes: Array<string>, avatarFileId?: string | null, id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, roleNames: Array<string>, roleIds: Array<string>, avatarFile?: { __typename?: 'BlobObject', url?: string | null, id: string, createdOn: any, fileSize: any, mimeType?: string | null, storageType: Types.BlobStorageType, fileName?: string | null, md5?: string | null } | null, orgs: Array<{ __typename?: 'Org', name: string, code: string, allParentOrgs: Array<{ __typename?: 'Org', code: string, name: string }> }>, claims: Array<{ __typename?: 'UserClaim', claimType: string, claimValue: string }> };

export type assignOrgsVariables = Types.Exact<{
  request: Types.AssignOrgRequest;
}>;


export type assignOrgsResult = { __typename?: 'Mutation', assignOrgs: boolean };

export const UserBrief = gql`
    fragment UserBrief on User {
  id
  username
  nickname
  phoneNumber
  email
  isEnable
  roleNames
  roleIds
}
    ` as unknown as DocumentNode<UserBrief, unknown>;
export const UserList = gql`
    fragment UserList on User {
  ...UserBrief
  createdOn
  orgCodes
}
    ${UserBrief}` as unknown as DocumentNode<UserList, unknown>;
export const UserCacheDto = gql`
    fragment UserCacheDto on User {
  ...UserBrief
  avatarFile {
    url
  }
}
    ${UserBrief}` as unknown as DocumentNode<UserCacheDto, unknown>;
export const UserMinimal = gql`
    fragment UserMinimal on User {
  id
  username
  nickname
}
    ` as unknown as DocumentNode<UserMinimal, unknown>;
export const UserDetail = gql`
    fragment UserDetail on User {
  ...UserBrief
  isEnable
  permissions
  avatarFile {
    url
  }
  orgs {
    allParentOrgs {
      code
      name
    }
    name
    code
  }
  claims {
    claimType
    claimValue
  }
  orgCodes
  avatarFileId
  avatarFile {
    ...BlobObjectBrief
  }
}
    ${UserBrief}
${BlobObjectBrief}` as unknown as DocumentNode<UserDetail, unknown>;
export const userLists = gql`
    query userLists($skip: Int, $take: Int, $filter: IUserFilterInput, $sort: [IUserSortInput!]) {
  users(skip: $skip, take: $take, filter: $filter, sort: $sort) {
    items {
      ...UserList
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${UserList}
${PageInfo}` as unknown as DocumentNode<userListsResult, userListsVariables>;
export const userById = gql`
    query userById($id: String) {
  users(skip: 0, take: 1, filter: {id: {eq: $id}}) {
    items {
      ...UserDetail
    }
  }
}
    ${UserDetail}` as unknown as DocumentNode<userByIdResult, userByIdVariables>;
export const userMenus = gql`
    query userMenus($filter: IUserFilterInput) {
  users(skip: 0, take: 999, filter: $filter) {
    items {
      ...UserMinimal
    }
  }
}
    ${UserMinimal}` as unknown as DocumentNode<userMenusResult, userMenusVariables>;
export const editUser = gql`
    mutation editUser($request: EditUserRequest!) {
  editUser(request: $request) {
    id
  }
}
    ` as unknown as DocumentNode<editUserResult, editUserVariables>;
export const createUser = gql`
    mutation createUser($request: CreateUserRequest!) {
  createUser(request: $request) {
    id
  }
}
    ` as unknown as DocumentNode<createUserResult, createUserVariables>;
export const resetUserPassword = gql`
    mutation resetUserPassword($request: ResetUserPasswordRequest!) {
  resetUserPassword(request: $request) {
    id
  }
}
    ` as unknown as DocumentNode<resetUserPasswordResult, resetUserPasswordVariables>;
export const changePassword = gql`
    mutation changePassword($request: ChangePasswordRequest!) {
  changePassword(request: $request)
}
    ` as unknown as DocumentNode<changePasswordResult, changePasswordVariables>;
export const deleteUser = gql`
    mutation deleteUser($ids: [String!]!) {
  deleteUser(ids: $ids)
}
    ` as unknown as DocumentNode<deleteUserResult, deleteUserVariables>;
export const assignOrgs = gql`
    mutation assignOrgs($request: AssignOrgRequest!) {
  assignOrgs(request: $request)
}
    ` as unknown as DocumentNode<assignOrgsResult, assignOrgsVariables>;
