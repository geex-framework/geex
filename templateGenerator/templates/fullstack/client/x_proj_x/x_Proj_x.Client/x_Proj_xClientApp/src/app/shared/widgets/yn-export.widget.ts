import { ChangeDetectionStrategy, Component } from "@angular/core";
import { NzMessageService } from "ng-zorro-antd/message";
@Component({
  selector: "st-widget-yn",
  template: `<span [innerHTML]="value === truth | yn"></span>`,
})
export class STYnExportWidget {
  static readonly KEY = "yn-export";
  value: boolean;
  truth: any = true;
}
