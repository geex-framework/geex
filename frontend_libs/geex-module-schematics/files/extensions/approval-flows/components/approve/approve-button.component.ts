import { Component, EventEmitter, inject, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { NzButtonModule } from "ng-zorro-antd/button";
import { NzIconModule } from "ng-zorro-antd/icon";
import { NzInputModule } from "ng-zorro-antd/input";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzPopoverModule } from "ng-zorro-antd/popover";
import { NzTabChangeEvent, NzTabsModule } from "ng-zorro-antd/tabs";

export type ClickParams = {
  approve: boolean;
  rejectReason?: string;
};

@Component({
  selector: "approve-button",
  template: `
    <a nz-popover [nzPopoverContent]="contentTemplate"
      ><i nz-icon nzType="audit"></i>
      <ng-content></ng-content>
    </a>
    <ng-template #contentTemplate>
      <nz-tabs (nzSelectChange)="onTabChange($event)">
        <nz-tab [nzTitle]="I18N.ApprovalFlows.approve.pass">
          <span>{{ I18N.ApprovalFlows.approve.passHint }}</span>
        </nz-tab>
        <nz-tab [nzTitle]="I18N.ApprovalFlows.approve.reject">
          <input nz-input [placeholder]="I18N.ApprovalFlows.approve.rejectReasonPlaceholder" [(ngModel)]="rejectReason" />
        </nz-tab>
      </nz-tabs>
      <button style="margin-top: 10px;" nz-button nzType="primary" nzBlock (click)="approveClick()">
        {{ I18N.Common.action.confirm }}
      </button>
    </ng-template>
  `,
  standalone: true,
  imports: [FormsModule, NzButtonModule, NzIconModule, NzInputModule, NzPopoverModule, NzTabsModule],
})
export class ApproveButtonComponent {
  readonly I18N = inject(GEEX_I18N);
  private readonly message = inject(NzMessageService);
  isReject = false;
  rejectReason = "";
  @Output() readonly nzClick = new EventEmitter<ClickParams>();

  onTabChange({ index }: NzTabChangeEvent): void {
    this.isReject = index === 1;
  }

  approveClick(): void {
    if (this.isReject) {
      if (!this.rejectReason.trim()) {
        this.message.error(this.I18N.ApprovalFlows.approve.rejectReasonRequired);
        return;
      }
      this.nzClick.emit({ rejectReason: this.rejectReason.trim(), approve: false });
      return;
    }
    this.nzClick.emit({ approve: true });
  }
}
