import { Component, EventEmitter, OnInit, Output } from "@angular/core";
export type ClickParams = {
  audit: boolean;
  rejectReason?: string;
};
@Component({
  selector: "audit-button",
  template: `
    <a nz-popover [nzPopoverContent]="contentTemplate"
      ><i nz-icon nzType="audit"></i>
      <ng-content></ng-content>
    </a>
    <ng-template #contentTemplate>
      <nz-tabset>
        <nz-tab nzTitle="通过" (nzClick)="isReject = false">
          <span>执行该操作将审批通过</span>
        </nz-tab>
        <nz-tab nzTitle="驳回" (nzClick)="isReject = true">
          <input nz-input placeholder="驳回原因" [(ngModel)]="rejectReason" />
        </nz-tab>
      </nz-tabset>
      <button style="margin-top: 10px;" nz-button nzType="primary" nzBlock (click)="auditClick()">确定</button>
    </ng-template>
  `,
})
export class AuditButtonComponent implements OnInit {
  /**是否驳回 */
  isReject = false;
  /**驳回原因 */
  rejectReason: string;
  @Output() readonly nzClick = new EventEmitter<ClickParams>();
  constructor() {}

  ngOnInit(): void {}
  auditClick() {
    if (this.isReject) {
      this.nzClick.emit({ rejectReason: this.rejectReason, audit: false });
    } else {
      this.nzClick.emit({ audit: true });
    }
  }
}
