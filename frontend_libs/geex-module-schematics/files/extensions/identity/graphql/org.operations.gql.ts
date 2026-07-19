import * as Types from '../../../shared/graphql/schema.gql';

import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
export type orgsVariables = Types.Exact<{
  filter?: Types.InputMaybe<Types.IOrgFilterInput>;
}>;


export type orgsResult = { __typename?: 'Query', orgs?: { __typename?: 'OrgsCollectionSegment', items?: Array<{ __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string } | null> | null } | null };

export type createOrgVariables = Types.Exact<{
  request: Types.CreateOrgRequest;
}>;


export type createOrgResult = { __typename?: 'Mutation', createOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string } };

export type deleteOrgVariables = Types.Exact<{
  ids: Array<Types.Scalars['String']['input']> | Types.Scalars['String']['input'];
}>;


export type deleteOrgResult = { __typename?: 'Mutation', deleteOrg?: boolean | null };

export type OrgBrief = { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string };

export type OrgDetail = { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, allSubOrgs: Array<{ __typename?: 'Org', name: string, code: string }>, directSubOrgs: Array<{ __typename?: 'Org', name: string, code: string }> };

export type OrgRecursiveParent = { __typename?: 'Org', parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string, parentOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string } } } } } } } } } } };

export type updateOrgVariables = Types.Exact<{
  request: Types.UpdateOrgRequest;
}>;


export type updateOrgResult = { __typename?: 'Mutation', updateOrg: { __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string } };

export type moveOrgVariables = Types.Exact<{
  request: Types.MoveOrgRequest;
}>;


export type moveOrgResult = { __typename?: 'Mutation', moveOrg: boolean };

export type importOrgVariables = Types.Exact<{
  request: Types.ImportOrgRequest;
}>;


export type importOrgResult = { __typename?: 'Mutation', importOrg: Array<{ __typename?: 'Org', code: string, name: string, orgType: Types.OrgTypeEnum, parentOrgCode: string, id: string }> };

export type fixUserOrgVariables = Types.Exact<{ [key: string]: never; }>;


export type fixUserOrgResult = { __typename?: 'Mutation', fixUserOrg: boolean };

export const OrgBrief = gql`
    fragment OrgBrief on Org {
  code
  name
  orgType
  parentOrgCode
  id
}
    ` as unknown as DocumentNode<OrgBrief, unknown>;
export const OrgDetail = gql`
    fragment OrgDetail on Org {
  ...OrgBrief
  allSubOrgs {
    name
    code
  }
  directSubOrgs {
    name
    code
  }
}
    ${OrgBrief}` as unknown as DocumentNode<OrgDetail, unknown>;
export const OrgRecursiveParent = gql`
    fragment OrgRecursiveParent on Org {
  parentOrg {
    ...OrgBrief
    parentOrg {
      ...OrgBrief
      parentOrg {
        ...OrgBrief
        parentOrg {
          ...OrgBrief
          parentOrg {
            ...OrgBrief
            parentOrg {
              ...OrgBrief
              parentOrg {
                ...OrgBrief
                parentOrg {
                  ...OrgBrief
                  parentOrg {
                    ...OrgBrief
                    parentOrg {
                      ...OrgBrief
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
    ${OrgBrief}` as unknown as DocumentNode<OrgRecursiveParent, unknown>;
export const orgs = gql`
    query orgs($filter: IOrgFilterInput) {
  orgs(skip: 0, take: 999, filter: $filter) {
    items {
      ...OrgBrief
    }
  }
}
    ${OrgBrief}` as unknown as DocumentNode<orgsResult, orgsVariables>;
export const createOrg = gql`
    mutation createOrg($request: CreateOrgRequest!) {
  createOrg(request: $request) {
    ...OrgBrief
  }
}
    ${OrgBrief}` as unknown as DocumentNode<createOrgResult, createOrgVariables>;
export const deleteOrg = gql`
    mutation deleteOrg($ids: [String!]!) {
  deleteOrg(ids: $ids)
}
    ` as unknown as DocumentNode<deleteOrgResult, deleteOrgVariables>;
export const updateOrg = gql`
    mutation updateOrg($request: UpdateOrgRequest!) {
  updateOrg(request: $request) {
    ...OrgBrief
  }
}
    ${OrgBrief}` as unknown as DocumentNode<updateOrgResult, updateOrgVariables>;
export const moveOrg = gql`
    mutation moveOrg($request: MoveOrgRequest!) {
  moveOrg(request: $request)
}
    ` as unknown as DocumentNode<moveOrgResult, moveOrgVariables>;
export const importOrg = gql`
    mutation importOrg($request: ImportOrgRequest!) {
  importOrg(request: $request) {
    ...OrgBrief
  }
}
    ${OrgBrief}` as unknown as DocumentNode<importOrgResult, importOrgVariables>;
export const fixUserOrg = gql`
    mutation fixUserOrg {
  fixUserOrg
}
    ` as unknown as DocumentNode<fixUserOrgResult, fixUserOrgVariables>;
