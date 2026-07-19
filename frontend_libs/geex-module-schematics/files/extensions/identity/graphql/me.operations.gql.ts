import * as Types from '../../../shared/graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
export type generatePersonalAccessTokenVariables = Types.Exact<{
  req: Types.GeneratePersonalAccessTokenRequest;
}>;


export type generatePersonalAccessTokenResult = { __typename?: 'Mutation', generatePersonalAccessToken: { __typename?: 'UserSession', userId: string, token: string } };


export const generatePersonalAccessToken = gql`
    mutation generatePersonalAccessToken($req: GeneratePersonalAccessTokenRequest!) {
  generatePersonalAccessToken(request: $req) {
    userId
    token
  }
}
    ` as unknown as DocumentNode<generatePersonalAccessTokenResult, generatePersonalAccessTokenVariables>;
