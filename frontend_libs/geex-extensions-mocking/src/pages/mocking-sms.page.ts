import { Component, OnInit, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { Apollo } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import { CLEAR_MOCK_SMS_MESSAGES, MOCK_SMS_MESSAGES } from "../graphql";

type MockSmsMessageRow = {
  phoneNumber: string;
  templateParams?: unknown;
  captchaCandidate?: string | null;
  success: boolean;
  sentAt?: string | null;
};

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div style="padding:16px;font-family:sans-serif">
      <h2>Mock SMS Inbox</h2>
      <div style="display:flex;gap:8px;margin-bottom:12px">
        <input [(ngModel)]="phoneNumber" name="phoneNumber" placeholder="filter phone" />
        <button type="button" (click)="reload()">Refresh</button>
        <button type="button" (click)="clear()">Clear</button>
      </div>
      @if (loading()) {
        <p>Loading...</p>
      }
      @if (error()) {
        <p style="color:red">{{ error() }}</p>
      }
      <table border="1" cellpadding="6" style="border-collapse:collapse;width:100%">
        <thead>
          <tr><th>Phone</th><th>Params</th><th>Captcha</th><th>Success</th><th>SentAt</th><th></th></tr>
        </thead>
        <tbody>
          @for (m of messages(); track $index) {
            <tr>
              <td>{{ m.phoneNumber }}</td>
              <td>{{ m.templateParams | json }}</td>
              <td>{{ m.captchaCandidate }}</td>
              <td>{{ m.success }}</td>
              <td>{{ m.sentAt }}</td>
              <td><button type="button" (click)="copy(m.captchaCandidate)">Copy</button></td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class MockingSmsPage implements OnInit {
  private readonly apollo = inject(Apollo);

  messages = signal<MockSmsMessageRow[]>([]);
  loading = signal(false);
  error = signal("");
  phoneNumber = "";

  ngOnInit(): void {
    void this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set("");
    try {
      const result = await firstValueFrom(
        this.apollo.query<{ mockSmsMessages: MockSmsMessageRow[] }>({
          query: MOCK_SMS_MESSAGES,
          variables: { phoneNumber: this.phoneNumber || null },
          fetchPolicy: "network-only",
        }),
      );
      this.messages.set(result.data?.mockSmsMessages ?? []);
    } catch (err: any) {
      this.error.set(err?.message ?? "Failed to load SMS messages");
      this.messages.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async clear(): Promise<void> {
    this.error.set("");
    try {
      await firstValueFrom(
        this.apollo.mutate({
          mutation: CLEAR_MOCK_SMS_MESSAGES,
          variables: { request: { phoneNumber: this.phoneNumber || null } },
        }),
      );
      await this.reload();
    } catch (err: any) {
      this.error.set(err?.message ?? "Failed to clear SMS messages");
    }
  }

  copy(value?: string | null): void {
    if (!value) return;
    void navigator.clipboard.writeText(value);
  }
}
