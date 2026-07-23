import { Apollo } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import gql from 'graphql-tag';
import { InjectionToken, makeEnvironmentProviders } from '@angular/core';
import { provideGeexModuleContribution } from '@geexcode/geex-angular';

const GQL_PAYMENTS = gql `
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
const GQL_PAYMENT = gql `
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
const GQL_PAYMENT_REFUNDS = gql `
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
const GQL_CLOSE_PAYMENT = gql `
  mutation closePayment($request: ClosePaymentRequest!) {
    closePayment(request: $request) {
      clientSn
      status
    }
  }
`;
const GQL_REVOKE_PAYMENT = gql `
  mutation revokePayment($request: RevokePaymentRequest!) {
    revokePayment(request: $request) {
      clientSn
      status
    }
  }
`;
const GQL_SYNC_PAYMENT = gql `
  mutation syncPayment($request: SyncPaymentRequest!) {
    syncPayment(request: $request) {
      clientSn
      status
    }
  }
`;
const GQL_CREATE_PAYMENT = gql `
  mutation createPayment($request: CreatePaymentRequest!) {
    createPayment(request: $request) {
      payment {
        id
        clientSn
        status
        amount
        subject
        provider
      }
      prepay {
        outTradeNo
        codeUrl
      }
    }
  }
`;
const GQL_CREATE_PAYMENT_REFUND = gql `
  mutation createPaymentRefund($request: CreatePaymentRefundRequest!) {
    createPaymentRefund(request: $request) {
      refundRequestNo
      clientSn
      amount
      status
    }
  }
`;
const GQL_SYNC_PAYMENT_REFUND = gql `
  mutation syncPaymentRefund($request: SyncPaymentRefundRequest!) {
    syncPaymentRefund(request: $request) {
      refundRequestNo
      clientSn
      amount
      status
    }
  }
`;

function createPaymentsModule(injector) {
    const apollo = () => injector.get(Apollo);
    return {
        documents: {
            payments: GQL_PAYMENTS,
            payment: GQL_PAYMENT,
            paymentRefunds: GQL_PAYMENT_REFUNDS,
            closePayment: GQL_CLOSE_PAYMENT,
            revokePayment: GQL_REVOKE_PAYMENT,
            syncPayment: GQL_SYNC_PAYMENT,
            createPayment: GQL_CREATE_PAYMENT,
            createPaymentRefund: GQL_CREATE_PAYMENT_REFUND,
            syncPaymentRefund: GQL_SYNC_PAYMENT_REFUND,
        },
        async loadPayments(options = {}) {
            const res = await firstValueFrom(apollo().query({
                query: GQL_PAYMENTS,
                variables: { skip: options.skip ?? 0, take: options.take ?? 20 },
                fetchPolicy: "no-cache",
            }));
            return {
                items: res.data?.payments?.items ?? [],
                totalCount: res.data?.payments?.totalCount ?? 0,
            };
        },
        async loadPayment(clientSn) {
            const res = await firstValueFrom(apollo().query({
                query: GQL_PAYMENT,
                variables: { clientSn },
                fetchPolicy: "no-cache",
            }));
            return res.data?.payment ?? null;
        },
        async loadPaymentRefunds(options = {}) {
            const res = await firstValueFrom(apollo().query({
                query: GQL_PAYMENT_REFUNDS,
                variables: { skip: options.skip ?? 0, take: options.take ?? 20 },
                fetchPolicy: "no-cache",
            }));
            return {
                items: res.data?.paymentRefunds?.items ?? [],
                totalCount: res.data?.paymentRefunds?.totalCount ?? 0,
            };
        },
        async closePayment(clientSn) {
            const res = await firstValueFrom(apollo().mutate({
                mutation: GQL_CLOSE_PAYMENT,
                variables: { request: { clientSn } },
            }));
            return res.data?.closePayment ?? null;
        },
        async revokePayment(clientSn) {
            const res = await firstValueFrom(apollo().mutate({
                mutation: GQL_REVOKE_PAYMENT,
                variables: { request: { clientSn } },
            }));
            return res.data?.revokePayment ?? null;
        },
        async syncPayment(clientSn) {
            const res = await firstValueFrom(apollo().mutate({
                mutation: GQL_SYNC_PAYMENT,
                variables: { request: { clientSn } },
            }));
            return res.data?.syncPayment ?? null;
        },
        async createPayment(input) {
            const res = await firstValueFrom(apollo().mutate({
                mutation: GQL_CREATE_PAYMENT,
                variables: { request: input },
            }));
            return res.data?.createPayment ?? null;
        },
        async createPaymentRefund(clientSn, amount) {
            const res = await firstValueFrom(apollo().mutate({
                mutation: GQL_CREATE_PAYMENT_REFUND,
                variables: { request: { clientSn, amount } },
            }));
            return res.data?.createPaymentRefund ?? null;
        },
        async syncPaymentRefund(refundRequestNo) {
            const res = await firstValueFrom(apollo().mutate({
                mutation: GQL_SYNC_PAYMENT_REFUND,
                variables: { request: { refundRequestNo } },
            }));
            return res.data?.syncPaymentRefund ?? null;
        },
        init: async () => undefined,
    };
}

const GEEX_PAYMENTS_OPTIONS = new InjectionToken("GEEX_PAYMENTS_OPTIONS");
function provideGeexPayments(options = {}) {
    return makeEnvironmentProviders([
        { provide: GEEX_PAYMENTS_OPTIONS, useValue: options },
        provideGeexModuleContribution({
            createModules: ({ injector }) => ({
                payments: (options.createPaymentsModule ?? createPaymentsModule)(injector),
            }),
        }),
    ]);
}

/**
 * Generated bundle index. Do not edit.
 */

export { GEEX_PAYMENTS_OPTIONS, GQL_CLOSE_PAYMENT, GQL_CREATE_PAYMENT, GQL_CREATE_PAYMENT_REFUND, GQL_PAYMENT, GQL_PAYMENTS, GQL_PAYMENT_REFUNDS, GQL_REVOKE_PAYMENT, GQL_SYNC_PAYMENT, GQL_SYNC_PAYMENT_REFUND, createPaymentsModule, provideGeexPayments };
//# sourceMappingURL=geexcode-geex-extensions-payments.mjs.map
