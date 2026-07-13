import * as Types from './schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
export type SettingInfo = { __typename?: 'Setting', id: string, name: Types.SettingDefinition, value?: any | null };

export const SettingInfo = gql`
    fragment SettingInfo on Setting {
  id
  name
  value
}
    ` as unknown as DocumentNode<SettingInfo, unknown>;