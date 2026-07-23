import { GeexModule } from '@geexcode/geex-angular';
import * as graphql from 'graphql';
import { DocumentNode } from 'graphql';
import { Injector, InjectionToken, EnvironmentProviders } from '@angular/core';

interface PaymentItem {
    id: string;
    clientSn?: string | null;
    status?: string | null;
    amount?: number | null;
    subject?: string | null;
    provider?: string | null;
    [key: string]: unknown;
}
interface PaymentRefundItem {
    id: string;
    refundRequestNo?: string | null;
    clientSn?: string | null;
    status?: string | null;
    amount?: number | null;
    [key: string]: unknown;
}
interface CreatePaymentInput {
    amount: number;
    subject?: string | null;
    businessOrderId?: string | null;
    channel?: string | null;
    provider?: string | null;
}
interface CreatePaymentResult {
    payment: PaymentItem;
    prepay?: {
        outTradeNo?: string | null;
        codeUrl?: string | null;
    } | null;
}
interface PaymentsModule extends GeexModule<{
    loadPayments(options?: {
        skip?: number;
        take?: number;
    }): Promise<{
        items: PaymentItem[];
        totalCount: number;
    }>;
    loadPayment(clientSn: string): Promise<PaymentItem | null>;
    loadPaymentRefunds(options?: {
        skip?: number;
        take?: number;
    }): Promise<{
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
}> {
}
declare module "@geexcode/geex-angular" {
    interface GeexModuleMap {
        payments: PaymentsModule;
    }
}

declare function createPaymentsModule(injector: Injector): PaymentsModule;

declare const GQL_PAYMENTS: graphql.DocumentNode;
declare const GQL_PAYMENT: graphql.DocumentNode;
declare const GQL_PAYMENT_REFUNDS: graphql.DocumentNode;
declare const GQL_CLOSE_PAYMENT: graphql.DocumentNode;
declare const GQL_REVOKE_PAYMENT: graphql.DocumentNode;
declare const GQL_SYNC_PAYMENT: graphql.DocumentNode;
declare const GQL_CREATE_PAYMENT: graphql.DocumentNode;
declare const GQL_CREATE_PAYMENT_REFUND: graphql.DocumentNode;
declare const GQL_SYNC_PAYMENT_REFUND: graphql.DocumentNode;

interface GeexPaymentsOptions {
    readonly createPaymentsModule?: (injector: Injector) => PaymentsModule;
}
declare const GEEX_PAYMENTS_OPTIONS: InjectionToken<Readonly<GeexPaymentsOptions>>;
declare function provideGeexPayments(options?: Readonly<GeexPaymentsOptions>): EnvironmentProviders;

export { GEEX_PAYMENTS_OPTIONS, GQL_CLOSE_PAYMENT, GQL_CREATE_PAYMENT, GQL_CREATE_PAYMENT_REFUND, GQL_PAYMENT, GQL_PAYMENTS, GQL_PAYMENT_REFUNDS, GQL_REVOKE_PAYMENT, GQL_SYNC_PAYMENT, GQL_SYNC_PAYMENT_REFUND, createPaymentsModule, provideGeexPayments };
export type { CreatePaymentInput, CreatePaymentResult, GeexPaymentsOptions, PaymentItem, PaymentRefundItem, PaymentsModule };
//# sourceMappingURL=index.d.ts.map
