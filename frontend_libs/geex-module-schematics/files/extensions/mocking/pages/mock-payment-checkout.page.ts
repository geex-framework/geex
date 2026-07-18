import { Component, OnInit, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ActivatedRoute } from "@angular/router";
import { Apollo } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import { CONFIRM_MOCK_PAYMENT_TRANSACTION, GET_MOCK_PAYMENT_TRANSACTION } from "@geexcode/geex-extensions-mocking";

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="padding:16px;font-family:sans-serif;max-width:480px;margin:0 auto">
      <h2>Mock Payment Checkout</h2>
      @if (loading()) {
        <p>Loading payment...</p>
      }
      @if (error()) {
        <p style="color:red">{{ error() }}</p>
      }
      @if (tx(); as payment) {
        <p>Order: {{ payment.clientSn }}</p>
        <p>Subject: {{ payment.subject }}</p>
        <p>Amount: {{ payment.amount }} {{ payment.currency }}</p>
        <p>Status: {{ payment.status }}</p>
        @if (payment.status === "Paying") {
          <div style="display:flex;flex-direction:column;gap:8px">
            <button type="button" (click)="confirm('Succeeded')">Confirm success</button>
            <button type="button" (click)="confirm('Failed')">Fail payment</button>
            <button type="button" (click)="confirm('Closed')">Cancel payment</button>
          </div>
        }
      }
    </div>
  `,
})
export class MockPaymentCheckoutPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly apollo = inject(Apollo);

  token = "";
  tx = signal<any>(null);
  error = signal("");
  loading = signal(true);

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get("token") ?? "";
    void this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set("");
    try {
      const result = await firstValueFrom(
        this.apollo.query<{ mockPaymentTransaction: any }>({
          query: GET_MOCK_PAYMENT_TRANSACTION,
          variables: { token: this.token },
          fetchPolicy: "network-only",
        }),
      );
      this.tx.set(result.data?.mockPaymentTransaction ?? null);
    } catch (err: any) {
      this.error.set(err?.message ?? "Invalid payment token");
      this.tx.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  async confirm(status: string): Promise<void> {
    this.error.set("");
    try {
      await firstValueFrom(
        this.apollo.mutate({
          mutation: CONFIRM_MOCK_PAYMENT_TRANSACTION,
          variables: { request: { token: this.token, status } },
        }),
      );
      await this.reload();
    } catch (err: any) {
      this.error.set(err?.message ?? "Confirm failed");
    }
  }
}
