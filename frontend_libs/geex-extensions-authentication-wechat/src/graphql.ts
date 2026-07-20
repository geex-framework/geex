import { TypedDocumentNode as DocumentNode } from "@graphql-typed-document-node/core";
import gql from "graphql-tag";
import { ResolveLoginResult, WechatLoginProvider } from "./authentication-wechat.types";

export type UserSessionResult = {
  token?: string | null;
  userId: string;
  loginProvider: string;
};

export type ResolveLoginVariables = {
  loginProvider: WechatLoginProvider;
  code: string;
};

export type ResolveLoginMutationResult = {
  resolveLogin: ResolveLoginResult;
};

export type LinkLoginVariables = {
  userLoginLinkToken: string;
};

export type LinkLoginResult = {
  linkLogin: UserSessionResult;
};

export type RegisterAndLinkLoginVariables = {
  userLoginLinkToken: string;
  username: string;
  password: string;
  phoneNumber?: string | null;
  email?: string | null;
  nickname?: string | null;
};

export type RegisterAndLinkLoginResult = {
  registerAndLinkLogin: UserSessionResult;
};

export const RESOLVE_LOGIN = gql`
  mutation resolveLogin($loginProvider: LoginProviderEnum!, $code: String!) {
    resolveLogin(request: { loginProvider: $loginProvider, code: $code }) {
      isLinked
      userLoginLinkToken
      displayName
      session {
        token
        userId
        loginProvider
      }
    }
  }
` as unknown as DocumentNode<ResolveLoginMutationResult, ResolveLoginVariables>;

export const LINK_LOGIN = gql`
  mutation linkLogin($userLoginLinkToken: String!) {
    linkLogin(request: { userLoginLinkToken: $userLoginLinkToken }) {
      token
      userId
      loginProvider
    }
  }
` as unknown as DocumentNode<LinkLoginResult, LinkLoginVariables>;

export const REGISTER_AND_LINK_LOGIN = gql`
  mutation registerAndLinkLogin(
    $userLoginLinkToken: String!
    $username: String!
    $password: String!
    $phoneNumber: String
    $email: String
    $nickname: String
  ) {
    registerAndLinkLogin(
      request: {
        userLoginLinkToken: $userLoginLinkToken
        username: $username
        password: $password
        phoneNumber: $phoneNumber
        email: $email
        nickname: $nickname
      }
    ) {
      token
      userId
      loginProvider
    }
  }
` as unknown as DocumentNode<RegisterAndLinkLoginResult, RegisterAndLinkLoginVariables>;
