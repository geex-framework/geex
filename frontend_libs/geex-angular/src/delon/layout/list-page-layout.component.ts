import { Component, contentChild, inject, input, output, TemplateRef } from "@angular/core";
import { PageHeaderModule } from "@delon/abc/page-header";
import { STChange, STColumn, STData, STModule } from "@delon/abc/st";
import { NzAlertModule } from "ng-zorro-antd/alert";
import { NzCardModule } from "ng-zorro-antd/card";
import { NzDividerModule } from "ng-zorro-antd/divider";
import { NzIconModule } from "ng-zorro-antd/icon";

import { GEEX_I18N } from "../tokens";

@Component({
  selector: "list-page-layout",
  standalone: true,
  imports: [PageHeaderModule, STModule, NzCardModule, NzAlertModule, NzDividerModule, NzIconModule],
  template: `
    <page-header [title]="title()" [tab]="headerTabTpl()" [extra]="headerExtraTpl()" [action]="headerActionTpl()">
      @if (filtersInHeader()) {
        <ng-content select="[filters]" />
      }
    </page-header>

    <nz-card>
      @if (!filtersInHeader()) {
        <ng-content select="[filters]" />
      }
      <nz-alert class="mb-sm" nzType="info" nzShowIcon [nzMessage]="selectionMessage">
        <ng-template #selectionMessage>
          <span>{{ selectedLabel }}{{ selectedCount() }}{{ selectedUnitLabel }}</span>
          <nz-divider nzType="vertical" />
          <a (click)="refresh.emit()">
            <i nz-icon nzType="reload"></i>
            {{ refreshLabel }}
          </a>
          <ng-content select="[toolbar]" />
        </ng-template>
      </nz-alert>
      <st
        class="mt-sm"
        [multiSort]="multiSort()"
        [loading]="loading()"
        [total]="total()"
        [data]="data()"
        [pi]="pi()"
        [ps]="ps()"
        [columns]="columns()"
        (change)="tableChange.emit($event)"
      />
    </nz-card>
  `,
})
export class ListPageLayoutComponent {
  private i18n = inject(GEEX_I18N, { optional: true }) as any;

  title = input.required<string>();
  loading = input(false);
  total = input(0);
  data = input<STData[]>([]);
  columns = input<STColumn[]>([]);
  pi = input(1);
  ps = input(10);
  selectedCount = input(0);
  multiSort = input(true);
  filtersInHeader = input(true);

  tableChange = output<STChange>();
  refresh = output<void>();

  headerExtraTpl = contentChild<TemplateRef<void>>("headerExtra");
  headerTabTpl = contentChild<TemplateRef<void>>("headerTab");
  headerActionTpl = contentChild<TemplateRef<void>>("headerAction");

  get selectedLabel() {
    return this.i18n?.Common?.list?.selected ?? "";
  }
  get selectedUnitLabel() {
    return this.i18n?.Common?.list?.selectedUnit ?? "";
  }
  get refreshLabel() {
    return this.i18n?.Common?.list?.refresh ?? "Refresh";
  }
}
