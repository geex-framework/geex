import * as Types from '../../../shared/graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
import { UserDetail } from '../../identity/graphql/user.operations.gql';
export type AuthenticateResultFragment = { __typename?: 'UserSession', userId: string, token: string, user?: { __typename?: 'User', isEnable: boolean, permissions: Array<string>, orgCodes: Array<string>, avatarFileId?: string | null, id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, roleNames: Array<string>, roleIds: Array<string>, avatarFile?: { __typename?: 'BlobObject', url?: string | null, id: string, createdOn: any, fileSize: any, mimeType?: string | null, storageType: Types.BlobStorageType, fileName?: string | null, md5?: string | null } | null, orgs: Array<{ __typename?: 'Org', name: string, code: string, allParentOrgs: Array<{ __typename?: 'Org', code: string, name: string }> }>, claims: Array<{ __typename?: 'UserClaim', claimType: string, claimValue: string }> } | null };

export type authenticateVariables = Types.Exact<{
  request: Types.AuthenticateRequest;
}>;


export type authenticateResult = { __typename?: 'Mutation', authenticate: { __typename?: 'UserSession', userId: string, token: string, user?: { __typename?: 'User', isEnable: boolean, permissions: Array<string>, orgCodes: Array<string>, avatarFileId?: string | null, id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, roleNames: Array<string>, roleIds: Array<string>, avatarFile?: { __typename?: 'BlobObject', url?: string | null, id: string, createdOn: any, fileSize: any, mimeType?: string | null, storageType: Types.BlobStorageType, fileName?: string | null, md5?: string | null } | null, orgs: Array<{ __typename?: 'Org', name: string, code: string, allParentOrgs: Array<{ __typename?: 'Org', code: string, name: string }> }>, claims: Array<{ __typename?: 'UserClaim', claimType: string, claimValue: string }> } | null } };

export type federateAuthenticateVariables = Types.Exact<{
  code: Types.Scalars['String']['input'];
  loginProvider: Types.LoginProviderEnum;
}>;


export type federateAuthenticateResult = { __typename?: 'Mutation', federateAuthenticate: { __typename?: 'UserSession', userId: string, token: string, user?: { __typename?: 'User', isEnable: boolean, permissions: Array<string>, orgCodes: Array<string>, avatarFileId?: string | null, id: string, username: string, nickname?: string | null, phoneNumber?: string | null, email?: string | null, roleNames: Array<string>, roleIds: Array<string>, avatarFile?: { __typename?: 'BlobObject', url?: string | null, id: string, createdOn: any, fileSize: any, mimeType?: string | null, storageType: Types.BlobStorageType, fileName?: string | null, md5?: string | null } | null, orgs: Array<{ __typename?: 'Org', name: string, code: string, allParentOrgs: Array<{ __typename?: 'Org', code: string, name: string }> }>, claims: Array<{ __typename?: 'UserClaim', claimType: string, claimValue: string }> } | null } };

export type registerAndSignInVariables = Types.Exact<{
  registerRequest: Types.RegisterUserRequest;
  authenticateRequest: Types.AuthenticateRequest;
}>;


export type registerAndSignInResult = { __typename?: 'Mutation', register: boolean, authenticate: { __typename?: 'UserSession', userId: string, token: string, user?: { __typename?: 'User', roleNames: Array<string>, roleIds: Array<string>, permissions: Array<string>, id: string, phoneNumber?: string | null, email?: string | null, username: string, avatarFile?: { __typename?: 'BlobObject', url?: string | null } | null } | null } };

export type sendSmsCaptchaVariables = Types.Exact<{
  phoneOrEmail: Types.Scalars['ChinesePhoneNumber']['input'];
}>;


export type sendSmsCaptchaResult = { __typename?: 'Mutation', generateCaptcha: { __typename?: 'Captcha', captchaType: Types.CaptchaType, key: string } };

export type validateSmsCaptchaVariables = Types.Exact<{
  captchaKey: Types.Scalars['String']['input'];
  captchaCode: Types.Scalars['String']['input'];
}>;


export type validateSmsCaptchaResult = { __typename?: 'Mutation', validateCaptcha: boolean };

export const AuthenticateResultFragment = gql`
    fragment AuthenticateResultFragment on UserSession {
  userId
  user {
    ...UserDetail
  }
  token
}
    ${UserDetail}` as unknown as DocumentNode<AuthenticateResultFragment, unknown>;
export const authenticate = gql`
    mutation authenticate($request: AuthenticateRequest!) {
  authenticate(request: $request) {
    ...AuthenticateResultFragment
  }
}
    ${AuthenticateResultFragment}` as unknown as DocumentNode<authenticateResult, authenticateVariables>;
export const federateAuthenticate = gql`
    mutation federateAuthenticate($code: String!, $loginProvider: LoginProviderEnum!) {
  federateAuthenticate(request: {code: $code, loginProvider: $loginProvider}) {
    ...AuthenticateResultFragment
  }
}
    ${AuthenticateResultFragment}` as unknown as DocumentNode<federateAuthenticateResult, federateAuthenticateVariables>;
export const registerAndSignIn = gql`
    mutation registerAndSignIn($registerRequest: RegisterUserRequest!, $authenticateRequest: AuthenticateRequest!) {
  register(request: $registerRequest)
  authenticate(request: $authenticateRequest) {
    userId
    user {
      id
      ... on User {
        roleNames
        roleIds
        permissions
        avatarFile {
          url
        }
      }
      phoneNumber
      email
      username
    }
    token
  }
}
    ` as unknown as DocumentNode<registerAndSignInResult, registerAndSignInVariables>;
export const sendSmsCaptcha = gql`
    mutation sendSmsCaptcha($phoneOrEmail: ChinesePhoneNumber!) {
  generateCaptcha(
    request: {captchaProvider: Sms, smsCaptchaPhoneNumber: $phoneOrEmail}
  ) {
    captchaType
    key
  }
}
    ` as unknown as DocumentNode<sendSmsCaptchaResult, sendSmsCaptchaVariables>;
export const validateSmsCaptcha = gql`
    mutation validateSmsCaptcha($captchaKey: String!, $captchaCode: String!) {
  validateCaptcha(
    request: {captchaProvider: Sms, captchaKey: $captchaKey, captchaCode: $captchaCode}
  )
}
    ` as unknown as DocumentNode<validateSmsCaptchaResult, validateSmsCaptchaVariables>;

