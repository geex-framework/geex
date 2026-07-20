import { Component, OnInit, inject, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Apollo } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import { CLEAR_MOCK_SMS_MESSAGES, MOCK_SMS_MESSAGES } from "@geexcode/geex-extensions-mocking";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";

type MockSmsMessageRow = {
  phoneNumber: string;
  templateParams?: unknown;
  captchaCandidate?: string | null;
  success: boolean;
  sentAt?: string | null;
};

@Component({
  standalone: true,
  imports: [SharedModule, FormsModule],
  template: `
    <page-header [title]="I18N.Mocking.sms.title" [autoBreadcrumb]="true" [extra]="phExtra">
      <form nz-form nzLayout="inline">
        <nz-form-item>
          <nz-form-label>{{ I18N.Mocking.sms.phone }}</nz-form-label>
          <nz-form-control>
            <input nz-input [(ngModel)]="phoneNumber" name="phoneNumber" [placeholder]="I18N.Mocking.sms.phoneFilter" (keyup.enter)="reload()" />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <button nz-button nzType="primary" type="button" (click)="reload()">
            <i nz-icon nzType="search" nzTheme="outline"></i>{{ I18N.Common.action.search }}
          </button>
        </nz-form-item>
      </form>
      <ng-template #phExtra>
        <button nz-button nzDanger type="button" (click)="clear()">
          <i nz-icon nzType="clear"></i>{{ I18N.Common.action.clear }}
        </button>
      </ng-template>
    </page-header>
    <nz-card>
      @if (error()) {
        <nz-alert nzType="error" [nzMessage]="error()" class="mb-md"></nz-alert>
      }

      <nz-table #table [nzData]="messages()" [nzLoading]="loading()" [nzFrontPagination]="false" [nzShowPagination]="false">
        <thead>
          <tr>
            <th>{{ I18N.Mocking.sms.phone }}</th>
            <th>{{ I18N.Mocking.sms.params }}</th>
            <th>{{ I18N.Mocking.sms.captcha }}</th>
            <th>{{ I18N.Mocking.sms.success }}</th>
            <th>{{ I18N.Mocking.sms.sentAt }}</th>
            <th>{{ I18N.Common.list.actions }}</th>
          </tr>
        </thead>
        <tbody>
          @for (m of table.data; track $index) {
            <tr>
              <td>{{ m.phoneNumber }}</td>
              <td>{{ m.templateParams | json }}</td>
              <td>{{ m.captchaCandidate }}</td>
              <td>{{ m.success }}</td>
              <td>{{ m.sentAt }}</td>
              <td>
                <button nz-button nzType="link" type="button" (click)="copy(m.captchaCandidate)">
                  {{ I18N.Common.action.copy }}
                </button>
              </td>
            </tr>
          }
        </tbody>
      </nz-table>
    </nz-card>
  `,
})
export class MockingSmsPage implements OnInit {
  I18N = inject(GEEX_I18N) as any;
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
      this.error.set(err?.message ?? this.I18N.Mocking.sms.loadFailed);
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
      this.error.set(err?.message ?? this.I18N.Mocking.sms.clearFailed);
    }
  }

  copy(value?: string | null): void {
    if (!value) return;
    void navigator.clipboard.writeText(value);
  }
}
