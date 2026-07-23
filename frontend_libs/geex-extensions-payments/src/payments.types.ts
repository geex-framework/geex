import type { GeexModule } from "@geexcode/geex-angular";
import type { DocumentNode } from "graphql";

export interface PaymentItem {
  id: string;
  clientSn?: string | null;
  status?: string | null;
  amount?: number | null;
  subject?: string | null;
  provider?: string | null;
  [key: string]: unknown;
}

export interface PaymentRefundItem {
  id: string;
  refundRequestNo?: string | null;
  clientSn?: string | null;
  status?: string | null;
  amount?: number | null;
  [key: string]: unknown;
}

export interface CreatePaymentInput {
  amount: number;
  subject?: string | null;
  businessOrderId?: string | null;
  channel?: string | null;
  provider?: string | null;
}

export interface CreatePaymentResult {
  payment: PaymentItem;
  prepay?: { outTradeNo?: string | null; codeUrl?: string | null } | null;
}

export interface PaymentsModule extends GeexModule<{
  loadPayments(options?: { skip?: number; take?: number }): Promise<{
    items: PaymentItem[];
    totalCount: number;
  }>;
  loadPayment(clientSn: string): Promise<PaymentItem | null>;
  loadPaymentRefunds(options?: { skip?: number; take?: number }): Promise<{
    items: PaymentRefundItem[];
    totalCount: number;
  }>;
  closePayment(clientSn: string): Promise<PaymentItem | null>;
  revokePayment(clientSn: string): Promise<PaymentItem | null>;
  syncPayment(clientSn: string): Promise<PaymentItem | null>;
  createPayment(input: CreatePaymentInput): Promise<CreatePaymentResult | null>;
  createPaymentRefund(clientSn: string, amount: number): Promise<PaymentRefundItem | null>;
  syncPaymentRefund(refundRequestNo: string): Promise<PaymentRefundItem | null>;
  readonly documents: {
    payments: DocumentNode;
    payment: DocumentNode;
    paymentRefunds: DocumentNode;
    closePayment: DocumentNode;
    revokePayment: DocumentNode;
    syncPayment: DocumentNode;
    createPayment: DocumentNode;
    createPaymentRefund: DocumentNode;
    syncPaymentRefund: DocumentNode;
  };
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    payments: PaymentsModule;
  }
}
