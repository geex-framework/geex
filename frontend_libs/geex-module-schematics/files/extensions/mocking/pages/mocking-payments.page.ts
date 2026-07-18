import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="padding:16px;font-family:sans-serif">
      <h2>Mock Payments</h2>
      <p>Open a payment checkout URL returned as CodeUrl from createPayment, for example <code>/mocking/payments/&#123;token&#125;</code>.</p>
    </div>
  `,
})
export class MockingPaymentsPage {}
