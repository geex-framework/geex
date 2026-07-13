import gql from "graphql-tag";

export const RESOLVE_EXTERNAL_LOGIN = gql`
  mutation resolveExternalLogin($loginProvider: LoginProviderEnum!, $code: String!) {
    resolveExternalLogin(request: { loginProvider: $loginProvider, code: $code }) {
      isLinked
      accountLinkToken
      displayName
      session {
        token
        userId
        loginProvider
      }
    }
  }
`;

export const LINK_EXTERNAL_LOGIN = gql`
  mutation linkExternalLogin($accountLinkToken: String!) {
    linkExternalLogin(request: { accountLinkToken: $accountLinkToken }) {
      token
      userId
      loginProvider
    }
  }
`;

export const REGISTER_AND_LINK_EXTERNAL_LOGIN = gql`
  mutation registerAndLinkExternalLogin(
    $accountLinkToken: String!
    $username: String!
    $password: String!
    $phoneNumber: String
    $email: String
    $nickname: String
  ) {
    registerAndLinkExternalLogin(
      request: {
        accountLinkToken: $accountLinkToken
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
`;
