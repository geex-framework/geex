import gql from "graphql-tag";

export const payments = gql`
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

export const paymentRefunds = gql`
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

export const closePayment = gql`
  mutation closePayment($request: ClosePaymentRequest!) {
    closePayment(request: $request) {
      clientSn
      status
    }
  }
`;

export const revokePayment = gql`
  mutation revokePayment($request: RevokePaymentRequest!) {
    revokePayment(request: $request) {
      clientSn
      status
    }
  }
`;

export const syncPayment = gql`
  mutation syncPayment($request: SyncPaymentRequest!) {
    syncPayment(request: $request) {
      clientSn
      status
    }
  }
`;

export const syncPaymentRefund = gql`
  mutation syncPaymentRefund($request: SyncPaymentRefundRequest!) {
    syncPaymentRefund(request: $request) {
      refundRequestNo
      status
    }
  }
`;

export interface PaymentBrief {
  id: string;
  clientSn?: string | null;
  status?: string | null;
  amount?: number | null;
  subject?: string | null;
  provider?: string | null;
}

export interface PaymentRefundBrief {
  id: string;
  refundRequestNo?: string | null;
  clientSn?: string | null;
  status?: string | null;
  amount?: number | null;
}
