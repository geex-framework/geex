import { Component, ViewEncapsulation, OnInit } from "@angular/core";
import { ControlUIWidget, SFNumberWidgetSchema } from "@delon/form";

@Component({
  selector: "sf-number",
  template: ` <sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
    <nz-input-number
      [nzId]="id"
      [ngModel]="value"
      (ngModelChange)="_setValue($event)"
      [nzDisabled]="disabled"
      [nzSize]="ui.size!"
      [nzMin]="min"
      [nzMax]="max"
      [nzStep]="step"
      [nzFormatter]="formatter"
      [nzParser]="parser"
      [nzPrecision]="ui.precision"
      [nzPlaceHolder]="ui.placeholder || ''"
      [style.width.px]="ui.widgetWidth || 90"
      [ngClass]="{ 'ant-input-number__hide-step': ui.hideStep }"
    >
    </nz-input-number>
    {{ ui.unit }}
  </sf-item-wrap>`,
  preserveWhitespaces: false,
  encapsulation: ViewEncapsulation.None,
})
export class NumberWidget extends ControlUIWidget<SFNumberWidgetSchema> implements OnInit {
  min: number;
  max: number;
  step: number;
  formatter: (value: number) => string | number = value => value;
  parser: (value: string) => string = value => value;
  static KEY: string = "number";

  ngOnInit(): void {
    const { minimum, exclusiveMinimum, maximum, exclusiveMaximum, multipleOf, type } = this.schema;
    if (typeof minimum !== "undefined") {
      this.min = exclusiveMinimum ? minimum + 1 : minimum;
    }
    if (typeof maximum !== "undefined") {
      this.max = exclusiveMaximum ? maximum - 1 : maximum;
    }
    this.step = multipleOf || 1;
    if (type === "integer") {
      this.min = Math.trunc(this.min);
      this.max = Math.trunc(this.max);
      this.step = Math.trunc(this.step);
    }

    const ui = this.ui;
    ui.width = this.ui.width;
    if (ui.prefix != null) {
      ui.formatter = value => (value == null ? "" : `${ui.prefix} ${value}`);
      ui.parser = value => value.replace(`${ui.prefix} `, "");
    }
    // if (ui.unit != null) {
    //   ui.formatter = value => (value == null ? "" : `${value}${ui.unit}`);
    //   ui.parser = value => value.replace(` ${ui.unit}`, "");
    // }
    if (ui.formatter) this.formatter = ui.formatter;
    if (ui.parser) this.parser = ui.parser;
  }

  _setValue(val: number): void {
    let value = this.schema.type === "integer" ? Math.floor(val) : val;
    this.setValue(value);
    if (this.ui.change) {
      this.ui.change(value);
    }
  }
}
