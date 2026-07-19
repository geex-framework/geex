import * as Types from '../../../shared/graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
import { PageInfo } from '../../../shared/graphql/fragments.gql';
export type editSettingVariables = Types.Exact<{
  request: Types.EditSettingRequest;
}>;


export type editSettingResult = { __typename?: 'Mutation', editSetting: { __typename?: 'Setting', name: Types.SettingDefinition, value?: any | null } };

export type settingsVariables = Types.Exact<{
  request: Types.GetSettingsRequest;
  skip?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  take?: Types.InputMaybe<Types.Scalars['Int']['input']>;
  filter?: Types.InputMaybe<Types.ISettingFilterInput>;
  includeDetail: Types.Scalars['Boolean']['input'];
}>;


export type settingsResult = { __typename?: 'Query', settings?: { __typename?: 'SettingsCollectionSegment', totalCount: number, items?: Array<{ __typename?: 'Setting', id: string, name: Types.SettingDefinition, value?: any | null, scope: Types.SettingScopeEnumeration, scopedKey?: string | null } | null> | null, pageInfo: { __typename?: 'CollectionSegmentInfo', hasPreviousPage: boolean, hasNextPage: boolean } } | null };

export type activeSettingsVariables = Types.Exact<{ [key: string]: never; }>;


export type activeSettingsResult = { __typename?: 'Query', activeSettings: Array<{ __typename?: 'Setting', id: string, name: Types.SettingDefinition, value?: any | null }> };

export type SettingBrief = { __typename?: 'Setting', id: string, name: Types.SettingDefinition, value?: any | null };

export type SettingDetail = { __typename?: 'Setting', scope: Types.SettingScopeEnumeration, scopedKey?: string | null };

export const SettingBrief = gql`
    fragment SettingBrief on Setting {
  id
  name
  value
}
    ` as unknown as DocumentNode<SettingBrief, unknown>;
export const SettingDetail = gql`
    fragment SettingDetail on Setting {
  scope
  scopedKey
}
    ` as unknown as DocumentNode<SettingDetail, unknown>;
export const editSetting = gql`
    mutation editSetting($request: EditSettingRequest!) {
  editSetting(request: $request) {
    name
    value
  }
}
    ` as unknown as DocumentNode<editSettingResult, editSettingVariables>;
export const settings = gql`
    query settings($request: GetSettingsRequest!, $skip: Int, $take: Int, $filter: ISettingFilterInput, $includeDetail: Boolean!) {
  settings(request: $request, skip: $skip, take: $take, filter: $filter) {
    items {
      ...SettingBrief
      ...SettingDetail @include(if: $includeDetail)
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${SettingBrief}
${SettingDetail}
${PageInfo}` as unknown as DocumentNode<settingsResult, settingsVariables>;
export const activeSettings = gql`
    query activeSettings {
  activeSettings {
    ...SettingBrief
  }
}
    ${SettingBrief}` as unknown as DocumentNode<activeSettingsResult, activeSettingsVariables>;