import { TypedDocumentNode as DocumentNode } from "@graphql-typed-document-node/core";
import { gql } from "graphql-tag";
import { GeexMockingCapabilities } from "./types";

export type MockingCapabilitiesVariables = Record<string, never>;

export type MockingCapabilitiesResult = {
  mockingCapabilities: GeexMockingCapabilities;
};

export type MockWechatAuthorization = {
  token: string;
  qrSvg?: string | null;
  status: string;
  state?: string | null;
  redirectUri?: string | null;
  expiresAt?: string | null;
  code?: string | null;
  profileId?: string | null;
};

export type CreateMockWechatAuthorizationVariables = {
  request: {
    redirectUri: string;
    state?: string | null;
  };
};

export type CreateMockWechatAuthorizationResult = {
  createMockWechatAuthorization: MockWechatAuthorization & { qrSvg: string; redirectUri: string; state: string };
};

export type GetMockWechatAuthorizationStatusVariables = {
  token: string;
};

export type GetMockWechatAuthorizationStatusResult = {
  mockWechatAuthorizationStatus: MockWechatAuthorization;
};

export type ConfirmMockWechatAuthorizationVariables = {
  request: {
    token: string;
    profileId: string;
  };
};

export type ConfirmMockWechatAuthorizationResult = {
  confirmMockWechatAuthorization: Pick<
    MockWechatAuthorization,
    "token" | "status" | "code" | "state" | "redirectUri" | "profileId"
  >;
};

export type MockWechatProfile = {
  id: string;
  openId: string;
  unionId?: string | null;
  nickname: string;
  avatar?: string | null;
  enabled: boolean;
};

export type MockWechatProfilesVariables = Record<string, never>;

export type MockWechatProfilesResult = {
  mockWechatProfiles: MockWechatProfile[];
};

export type MockRule = {
  id: string;
  name: string;
  target: string;
  operation: string;
  priority: number;
  enabled: boolean;
  match?: unknown;
  response?: unknown;
  delayMilliseconds?: number | null;
  outcome?: string | null;
};

export type MockRulesVariables = Record<string, never>;

export type MockRulesResult = {
  mockRules: MockRule[];
};

export type MockSmsMessage = {
  id: string;
  phoneNumber: string;
  templateParams?: unknown;
  tenantCode?: string | null;
  success: boolean;
  errorMessage?: string | null;
  sentAt?: string | null;
  captchaCandidate?: string | null;
};

export type MockSmsMessagesVariables = {
  phoneNumber?: string | null;
};

export type MockSmsMessagesResult = {
  mockSmsMessages: MockSmsMessage[];
};

export type ClearMockSmsMessagesVariables = {
  request: {
    phoneNumber?: string | null;
  };
};

export type ClearMockSmsMessagesResult = {
  clearMockSmsMessages: boolean;
};

export type MockPaymentTransaction = {
  token: string;
  clientSn: string;
  amount: number;
  currency: string;
  subject?: string | null;
  status: string;
  expiresAt?: string | null;
};

export type GetMockPaymentTransactionVariables = {
  token: string;
};

export type GetMockPaymentTransactionResult = {
  mockPaymentTransaction: MockPaymentTransaction;
};

export type ConfirmMockPaymentTransactionVariables = {
  request: {
    token: string;
    status: string;
  };
};

export type ConfirmMockPaymentTransactionResult = {
  confirmMockPaymentTransaction: {
    token: string;
    status: string;
    transactionId?: string | null;
    clientSn: string;
  };
};

export const MOCKING_CAPABILITIES = gql`
  query mockingCapabilities {
    mockingCapabilities {
      enabled
      wechatWeb
      payments
      sms
      management
    }
  }
` as unknown as DocumentNode<MockingCapabilitiesResult, MockingCapabilitiesVariables>;

export const CREATE_MOCK_WECHAT_AUTHORIZATION = gql`
  mutation createMockWechatAuthorization($request: CreateMockWechatAuthorizationRequest!) {
    createMockWechatAuthorization(request: $request) {
      token
      qrSvg
      status
      state
      redirectUri
      expiresAt
      code
    }
  }
` as unknown as DocumentNode<CreateMockWechatAuthorizationResult, CreateMockWechatAuthorizationVariables>;

export const GET_MOCK_WECHAT_AUTHORIZATION_STATUS = gql`
  query mockWechatAuthorizationStatus($token: String!) {
    mockWechatAuthorizationStatus(token: $token) {
      token
      status
      state
      redirectUri
      expiresAt
      code
      profileId
    }
  }
` as unknown as DocumentNode<GetMockWechatAuthorizationStatusResult, GetMockWechatAuthorizationStatusVariables>;

export const CONFIRM_MOCK_WECHAT_AUTHORIZATION = gql`
  mutation confirmMockWechatAuthorization($request: ConfirmMockWechatAuthorizationRequest!) {
    confirmMockWechatAuthorization(request: $request) {
      token
      status
      code
      state
      redirectUri
      profileId
    }
  }
` as unknown as DocumentNode<ConfirmMockWechatAuthorizationResult, ConfirmMockWechatAuthorizationVariables>;

export const MOCK_WECHAT_PROFILES = gql`
  query mockWechatProfiles {
    mockWechatProfiles {
      id
      openId
      unionId
      nickname
      avatar
      enabled
    }
  }
` as unknown as DocumentNode<MockWechatProfilesResult, MockWechatProfilesVariables>;

export const MOCK_RULES = gql`
  query mockRules {
    mockRules {
      id
      name
      target
      operation
      priority
      enabled
      match
      response
      delayMilliseconds
      outcome
    }
  }
` as unknown as DocumentNode<MockRulesResult, MockRulesVariables>;

export const MOCK_SMS_MESSAGES = gql`
  query mockSmsMessages($phoneNumber: String) {
    mockSmsMessages(phoneNumber: $phoneNumber) {
      id
      phoneNumber
      templateParams
      tenantCode
      success
      errorMessage
      sentAt
      captchaCandidate
    }
  }
` as unknown as DocumentNode<MockSmsMessagesResult, MockSmsMessagesVariables>;

export const CLEAR_MOCK_SMS_MESSAGES = gql`
  mutation clearMockSmsMessages($request: ClearMockSmsMessagesRequest!) {
    clearMockSmsMessages(request: $request)
  }
` as unknown as DocumentNode<ClearMockSmsMessagesResult, ClearMockSmsMessagesVariables>;

export const GET_MOCK_PAYMENT_TRANSACTION = gql`
  query mockPaymentTransaction($token: String!) {
    mockPaymentTransaction(token: $token) {
      token
      clientSn
      amount
      currency
      subject
      status
      expiresAt
    }
  }
` as unknown as DocumentNode<GetMockPaymentTransactionResult, GetMockPaymentTransactionVariables>;

export const CONFIRM_MOCK_PAYMENT_TRANSACTION = gql`
  mutation confirmMockPaymentTransaction($request: ConfirmMockPaymentTransactionRequest!) {
    confirmMockPaymentTransaction(request: $request) {
      token
      status
      transactionId
      clientSn
    }
  }
` as unknown as DocumentNode<ConfirmMockPaymentTransactionResult, ConfirmMockPaymentTransactionVariables>;
