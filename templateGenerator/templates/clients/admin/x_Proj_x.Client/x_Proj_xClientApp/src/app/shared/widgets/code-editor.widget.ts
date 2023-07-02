import { ChangeDetectionStrategy, Component } from "@angular/core";
import { ControlWidget } from "@delon/form";
import { NzMessageService } from "ng-zorro-antd/message";
declare type FormHooks = "change" | "blur" | "submit";
@Component({
  selector: "sf-widget-code-editor",

  template: `
    <sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
      <nz-code-editor
        class="editor"
        [ngModel]="value"
        (ngModelChange)="change($event)"
        [nzEditorOption]="{ language: ui.language, theme: 'vs-dark', wordWrap: 'bounded' }"
      ></nz-code-editor>
    </sf-item-wrap>
  `,

  host: {
    // "(click)": "click()"
  },

  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SFCodeEditorWidget extends ControlWidget {
  static readonly KEY = "code-editor";

  switchValue: boolean;

  ngOnInit(): void {}

  reset(value: string) {
    this.setValue(value);
  }
  change(value: string) {
    this.setValue(value);
    if (this.ui.change) this.ui.change(value);
  }
}
