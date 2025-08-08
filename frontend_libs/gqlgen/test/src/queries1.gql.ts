import * as Types from './graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
import { SettingInfo } from './graphql/fragments.gql';
export type GetInitialSettings2Variables = Types.Exact<{ [key: string]: never; }>;


export type GetInitialSettings2Result = { __typename?: 'Query', initSettings: Array<{ __typename?: 'Setting', id: string, name: Types.SettingDefinition, value?: any | null }> };


export const GetInitialSettings2 = gql`
    query GetInitialSettings2 {
  initSettings {
    ...SettingInfo
  }
}
    ${SettingInfo}` as unknown as DocumentNode<GetInitialSettings2Result, GetInitialSettings2Variables>;