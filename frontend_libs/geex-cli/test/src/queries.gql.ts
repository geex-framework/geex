import * as Types from './graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
import { SettingInfo } from './graphql/fragments.gql';
export type GetActiveSettingsVariables = Types.Exact<{ [key: string]: never; }>;


export type GetActiveSettingsResult = { __typename?: 'Query', activeSettings: Array<{ __typename?: 'Setting', id: string, name: Types.SettingDefinition, value?: any | null }> };

export type OrgCountVariables = Types.Exact<{
  orgName: Types.Scalars['String']['input'];
}>;


export type OrgCountResult = { __typename?: 'Query', orgs?: { __typename?: 'OrgsCollectionSegment', totalCount: number } | null };

export type editSettingVariables = Types.Exact<{
  name: Types.SettingDefinition;
  value: Types.Scalars['Any']['input'];
}>;


export type editSettingResult = { __typename?: 'Mutation', editSetting: { __typename?: 'Setting', id: string, name: Types.SettingDefinition, value?: any | null } };


export const GetActiveSettings = gql`
    query GetActiveSettings {
  activeSettings {
    ...SettingInfo
  }
}
    ${SettingInfo}` as unknown as DocumentNode<GetActiveSettingsResult, GetActiveSettingsVariables>;
export const OrgCount = gql`
    query OrgCount($orgName: String!) {
  orgs(filter: {name: {eq: $orgName}}) {
    totalCount
  }
}
    ` as unknown as DocumentNode<OrgCountResult, OrgCountVariables>;
export const editSetting = gql`
    mutation editSetting($name: SettingDefinition!, $value: Any!) {
  editSetting(request: {name: $name, value: $value}) {
    ...SettingInfo
  }
}
    ${SettingInfo}` as unknown as DocumentNode<editSettingResult, editSettingVariables>;