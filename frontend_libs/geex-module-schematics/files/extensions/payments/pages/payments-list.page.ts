import { Component, inject, OnInit, signal } from "@angular/core";
import type { STChange, STColumn } from "@delon/abc/st";
import { Apollo } from "apollo-angular";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { SharedModule } from "@/shared/shared.module";
import {
  closePayment,
  paymentRefunds,
  payments,
  revokePayment,
  syncPayment,
  syncPaymentRefund,
  type PaymentBrief,
  type PaymentRefundBrief,
} from "../graphql/operations.gql";

@Component({
  selector: "app-payments-list",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./payments-list.page.html",
})
export class PaymentsListPage implements OnInit {
  readonly I18N = inject(GEEX_I18N) as any;
  private readonly apollo = inject(Apollo);
  private readonly message = inject(NzMessageService);
  readonly loading = signal(false);
  readonly tabIndex = signal(0);
  readonly paymentData = signal<PaymentBrief[]>([]);
  readonly paymentTotal = signal(0);
  readonly refundData = signal<PaymentRefundBrief[]>([]);
  readonly refundTotal = signal(0);
  paymentPageIndex = 1;
  paymentPageSize = 10;
  refundPageIndex = 1;
  refundPageSize = 10;

  readonly paymentColumns: Array<STColumn<PaymentBrief>> = [
    { title: this.I18N.Payments.columnClientSn, index: "clientSn" },
    { title: this.I18N.Payments.columnStatus, index: "status" },
    { title: this.I18N.Payments.columnAmount, index: "amount", type: "number" },
    { title: this.I18N.Payments.columnSubject, index: "subject" },
    { title: this.I18N.Payments.columnProvider, index: "provider" },
    {
      title: this.I18N.Payments.columnActions,
      buttons: [
        { text: this.I18N.Payments.sync, click: item => this.runPaymentAction(syncPayment, item.clientSn) },
        { text: this.I18N.Payments.close, click: item => this.runPaymentAction(closePayment, item.clientSn) },
        { text: this.I18N.Payments.revoke, click: item => this.runPaymentAction(revokePayment, item.clientSn) },
      ],
    },
  ];

  readonly refundColumns: Array<STColumn<PaymentRefundBrief>> = [
    { title: this.I18N.Payments.columnRefundNo, index: "refundRequestNo" },
    { title: this.I18N.Payments.columnClientSn, index: "clientSn" },
    { title: this.I18N.Payments.columnStatus, index: "status" },
    { title: this.I18N.Payments.columnAmount, index: "amount", type: "number" },
    {
      title: this.I18N.Payments.columnActions,
      buttons: [
        {
          text: this.I18N.Payments.syncRefund,
          click: item => this.runRefundSync(item.refundRequestNo),
        },
      ],
    },
  ];

  ngOnInit(): void {
    void this.loadPayments();
  }

  onTabChange(index: number): void {
    this.tabIndex.set(index);
    if (index === 0) {
      void this.loadPayments();
    } else {
      void this.loadRefunds();
    }
  }

  async loadPayments(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.apollo
        .query<{
          payments?: { items?: PaymentBrief[]; totalCount?: number };
        }>({
          query: payments,
          variables: {
            skip: (this.paymentPageIndex - 1) * this.paymentPageSize,
            take: this.paymentPageSize,
          },
          fetchPolicy: "no-cache",
        })
        .firstValuePromise();
      this.paymentData.set(result.data?.payments?.items ?? []);
      this.paymentTotal.set(result.data?.payments?.totalCount ?? 0);
    } finally {
      this.loading.set(false);
    }
  }

  async loadRefunds(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.apollo
        .query<{
          paymentRefunds?: { items?: PaymentRefundBrief[]; totalCount?: number };
        }>({
          query: paymentRefunds,
          variables: {
            skip: (this.refundPageIndex - 1) * this.refundPageSize,
            take: this.refundPageSize,
          },
          fetchPolicy: "no-cache",
        })
        .firstValuePromise();
      this.refundData.set(result.data?.paymentRefunds?.items ?? []);
      this.refundTotal.set(result.data?.paymentRefunds?.totalCount ?? 0);
    } finally {
      this.loading.set(false);
    }
  }

  onPaymentTableChange(change: STChange): void {
    if (change.type === "pi" || change.type === "ps") {
      this.paymentPageIndex = change.pi ?? this.paymentPageIndex;
      this.paymentPageSize = change.ps ?? this.paymentPageSize;
      void this.loadPayments();
    }
  }

  onRefundTableChange(change: STChange): void {
    if (change.type === "pi" || change.type === "ps") {
      this.refundPageIndex = change.pi ?? this.refundPageIndex;
      this.refundPageSize = change.ps ?? this.refundPageSize;
      void this.loadRefunds();
    }
  }

  async runPaymentAction(mutation: typeof syncPayment, clientSn?: string | null): Promise<void> {
    if (!clientSn) {
      return;
    }
    await this.apollo.mutate({ mutation, variables: { request: { clientSn } } }).firstValuePromise();
    this.message.success(this.I18N.Payments.actionSuccess);
    await this.loadPayments();
  }

  async runRefundSync(refundRequestNo?: string | null): Promise<void> {
    if (!refundRequestNo) {
      return;
    }
    await this.apollo
      .mutate({ mutation: syncPaymentRefund, variables: { request: { refundRequestNo } } })
      .firstValuePromise();
    this.message.success(this.I18N.Payments.actionSuccess);
    await this.loadRefunds();
  }
}
