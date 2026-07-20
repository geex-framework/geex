import gql from "graphql-tag";

export const GQL_PAYMENTS = gql`
  query payments($skip: Int, $take: Int) {
    payments(skip: $skip, take: $take) {
      totalCount
      items {
        id
        clientSn
        status
        amount
        subject
        provider
      }
    }
  }
`;

export const GQL_PAYMENT = gql`
  query payment($clientSn: String!) {
    payment(clientSn: $clientSn) {
      id
      clientSn
      status
      amount
      subject
      provider
    }
  }
`;

export const GQL_PAYMENT_REFUNDS = gql`
  query paymentRefunds($skip: Int, $take: Int) {
    paymentRefunds(skip: $skip, take: $take) {
      totalCount
      items {
        id
        refundRequestNo
        clientSn
        status
        amount
      }
    }
  }
`;

export const GQL_CLOSE_PAYMENT = gql`
  mutation closePayment($request: ClosePaymentRequest!) {
    closePayment(request: $request) {
      clientSn
      status
    }
  }
`;

export const GQL_REVOKE_PAYMENT = gql`
  mutation revokePayment($request: RevokePaymentRequest!) {
    revokePayment(request: $request) {
      clientSn
      status
    }
  }
`;

export const GQL_SYNC_PAYMENT = gql`
  mutation syncPayment($request: SyncPaymentRequest!) {
    syncPayment(request: $request) {
      clientSn
      status
    }
  }
`;

export const GQL_CREATE_PAYMENT_REFUND = gql`
  mutation createPaymentRefund($request: CreatePaymentRefundRequest!) {
    createPaymentRefund(request: $request) {
      refundRequestNo
      clientSn
      amount
      status
    }
  }
`;

export const GQL_SYNC_PAYMENT_REFUND = gql`
  mutation syncPaymentRefund($request: SyncPaymentRefundRequest!) {
    syncPaymentRefund(request: $request) {
      refundRequestNo
      clientSn
      amount
      status
    }
  }
`;
