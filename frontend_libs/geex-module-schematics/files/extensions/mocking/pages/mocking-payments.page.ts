import { Component, inject } from "@angular/core";
import { SharedModule } from "@/shared/shared.module";
import { GEEX_I18N } from "@geexcode/geex-angular";

@Component({
  standalone: true,
  imports: [SharedModule],
  template: `
    <page-header [title]="I18N.Mocking.payments.title" [autoBreadcrumb]="true" />
    <nz-card>
      <nz-alert nzType="info" nzShowIcon [nzMessage]="I18N.Mocking.payments.description"></nz-alert>
    </nz-card>
  `,
})
export class MockingPaymentsPage {
  I18N = inject(GEEX_I18N) as any;
}
