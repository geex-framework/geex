export const GEEX_SUPER_ADMIN_ID = "000000000000000000000001";

export interface GeexMockingCapabilities {
  enabled: boolean;
  wechatWeb: boolean;
  payments: boolean;
  sms: boolean;
  management: boolean;
}

export interface GeexMockingOptions {
  /** Optional override for GraphQL path. Default: /graphql */
  graphqlPath?: string;
}
