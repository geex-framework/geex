import { Component, EventEmitter, inject, OnInit, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { NzButtonModule } from "ng-zorro-antd/button";
import { NzIconModule } from "ng-zorro-antd/icon";
import { NzInputModule } from "ng-zorro-antd/input";
import { NzPopoverModule } from "ng-zorro-antd/popover";
import { NzTabsModule } from "ng-zorro-antd/tabs";
import { GEEX_I18N } from "@geexcode/geex-angular";

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
      <nz-tabs>
        <nz-tab [nzTitle]="I18N.ApprovalFlows.approve.pass" (nzClick)="isReject = false">
          <span>{{ I18N.ApprovalFlows.approve.passHint }}</span>
        </nz-tab>
        <nz-tab [nzTitle]="I18N.ApprovalFlows.approve.reject" (nzClick)="isReject = true">
          <input nz-input [placeholder]="I18N.ApprovalFlows.approve.rejectReasonPlaceholder" [(ngModel)]="rejectReason" />
        </nz-tab>
      </nz-tabs>
      <button style="margin-top: 10px;" nz-button nzType="primary" nzBlock (click)="approveClick()">{{ I18N.Common.action.confirm }}</button>
    </ng-template>
  `,
  standalone: true,
  imports: [FormsModule, NzButtonModule, NzIconModule, NzInputModule, NzPopoverModule, NzTabsModule],
})
export class ApproveButtonComponent implements OnInit {
  readonly I18N = inject(GEEX_I18N) as any;
  isReject = false;
  rejectReason = "";
  @Output() readonly nzClick = new EventEmitter<ClickParams>();

  ngOnInit(): void {}

  approveClick() {
    if (this.isReject) {
      this.nzClick.emit({ rejectReason: this.rejectReason, approve: false });
    } else {
      this.nzClick.emit({ approve: true });
    }
  }
}
