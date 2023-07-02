import { ChangeDetectionStrategy, Component } from "@angular/core";
import { NzMessageService } from "ng-zorro-antd/message";
declare type FormHooks = "change" | "blur" | "submit";
@Component({
  selector: "st-widget-switch",

  template: ` <nz-switch [ngModel]="switchValue" (ngModelChange)="change($event)"></nz-switch> `,

  host: {
    // "(click)": "click()"
  },

  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class STSwitchWidget {
  static readonly KEY = "switch";

  switchValue: boolean;
  constructor(private msg: NzMessageService) {}

  change(value: boolean) {}
}
