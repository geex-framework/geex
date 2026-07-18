import { Component, EventEmitter, OnInit, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { NzButtonModule } from "ng-zorro-antd/button";
import { NzIconModule } from "ng-zorro-antd/icon";
import { NzInputModule } from "ng-zorro-antd/input";
import { NzPopoverModule } from "ng-zorro-antd/popover";
import { NzTabsModule } from "ng-zorro-antd/tabs";

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
        <nz-tab nzTitle="通过" (nzClick)="isReject = false">
          <span>执行该操作将审批通过</span>
        </nz-tab>
        <nz-tab nzTitle="驳回" (nzClick)="isReject = true">
          <input nz-input placeholder="驳回原因" [(ngModel)]="rejectReason" />
        </nz-tab>
      </nz-tabs>
      <button style="margin-top: 10px;" nz-button nzType="primary" nzBlock (click)="approveClick()">确定</button>
    </ng-template>
  `,
  standalone: true,
  imports: [FormsModule, NzButtonModule, NzIconModule, NzInputModule, NzPopoverModule, NzTabsModule],
})
export class ApproveButtonComponent implements OnInit {
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
