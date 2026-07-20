import { Injector } from "@angular/core";
import { Apollo } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import {
  GQL_CLOSE_PAYMENT,
  GQL_CREATE_PAYMENT_REFUND,
  GQL_PAYMENT,
  GQL_PAYMENT_REFUNDS,
  GQL_PAYMENTS,
  GQL_REVOKE_PAYMENT,
  GQL_SYNC_PAYMENT,
  GQL_SYNC_PAYMENT_REFUND,
} from "./graphql";
import type { PaymentItem, PaymentRefundItem, PaymentsModule } from "./payments.types";

export function createPaymentsModule(injector: Injector): PaymentsModule {
  const apollo = () => injector.get(Apollo);

  return {
    documents: {
      payments: GQL_PAYMENTS,
      payment: GQL_PAYMENT,
      paymentRefunds: GQL_PAYMENT_REFUNDS,
      closePayment: GQL_CLOSE_PAYMENT,
      revokePayment: GQL_REVOKE_PAYMENT,
      syncPayment: GQL_SYNC_PAYMENT,
      createPaymentRefund: GQL_CREATE_PAYMENT_REFUND,
      syncPaymentRefund: GQL_SYNC_PAYMENT_REFUND,
    },
    async loadPayments(options = {}) {
      const res = await firstValueFrom(
        apollo().query<{ payments: { items: PaymentItem[]; totalCount: number } }>({
          query: GQL_PAYMENTS,
          variables: { skip: options.skip ?? 0, take: options.take ?? 20 },
          fetchPolicy: "no-cache",
        }),
      );
      return {
        items: res.data?.payments?.items ?? [],
        totalCount: res.data?.payments?.totalCount ?? 0,
      };
    },
    async loadPayment(clientSn: string) {
      const res = await firstValueFrom(
        apollo().query<{ payment: PaymentItem | null }>({
          query: GQL_PAYMENT,
          variables: { clientSn },
          fetchPolicy: "no-cache",
        }),
      );
      return res.data?.payment ?? null;
    },
    async loadPaymentRefunds(options = {}) {
      const res = await firstValueFrom(
        apollo().query<{ paymentRefunds: { items: PaymentRefundItem[]; totalCount: number } }>({
          query: GQL_PAYMENT_REFUNDS,
          variables: { skip: options.skip ?? 0, take: options.take ?? 20 },
          fetchPolicy: "no-cache",
        }),
      );
      return {
        items: res.data?.paymentRefunds?.items ?? [],
        totalCount: res.data?.paymentRefunds?.totalCount ?? 0,
      };
    },
    async closePayment(clientSn: string) {
      const res = await firstValueFrom(
        apollo().mutate<{ closePayment: PaymentItem }>({
          mutation: GQL_CLOSE_PAYMENT,
          variables: { request: { clientSn } },
        }),
      );
      return res.data?.closePayment ?? null;
    },
    async revokePayment(clientSn: string) {
      const res = await firstValueFrom(
        apollo().mutate<{ revokePayment: PaymentItem }>({
          mutation: GQL_REVOKE_PAYMENT,
          variables: { request: { clientSn } },
        }),
      );
      return res.data?.revokePayment ?? null;
    },
    async syncPayment(clientSn: string) {
      const res = await firstValueFrom(
        apollo().mutate<{ syncPayment: PaymentItem }>({
          mutation: GQL_SYNC_PAYMENT,
          variables: { request: { clientSn } },
        }),
      );
      return res.data?.syncPayment ?? null;
    },
    async createPaymentRefund(clientSn: string, amount: number) {
      const res = await firstValueFrom(
        apollo().mutate<{ createPaymentRefund: PaymentRefundItem }>({
          mutation: GQL_CREATE_PAYMENT_REFUND,
          variables: { request: { clientSn, amount } },
        }),
      );
      return res.data?.createPaymentRefund ?? null;
    },
    async syncPaymentRefund(refundRequestNo: string) {
      const res = await firstValueFrom(
        apollo().mutate<{ syncPaymentRefund: PaymentRefundItem }>({
          mutation: GQL_SYNC_PAYMENT_REFUND,
          variables: { request: { refundRequestNo } },
        }),
      );
      return res.data?.syncPaymentRefund ?? null;
    },
    init: async () => undefined,
  };
}
