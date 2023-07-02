import { Component, ElementRef, forwardRef, Input, OnInit, ViewChild } from "@angular/core";
import { ControlValueAccessor, DefaultValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";
import * as _ from "lodash";
type NumberOption = {
  numberMin: number;
  /** 如果是数字格式 定制的允许最大值 */
  numberMax: number;
  /** 如果是数字格式 定制的允许每次进度值 */
  numberStep: number;
};
@Component({
  selector: "input-percent",
  templateUrl: "./input-percent.component.html",
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputPercentComponent),
      multi: true,
    },
  ],
  styles: [
    `
      :host ::ng-deep .ant-input {
        text-align: right;
      }
    `,
  ],
  preserveWhitespaces: false,
})
export class InputPercentComponent implements OnInit, ControlValueAccessor {
  @Input() disabled = false;
  @Input() option: NumberOption = {
    numberMin: -Infinity,
    numberMax: Infinity,
    numberStep: 1,
  };
  /**小数点位数，默认为4位，即0.0001% */
  @Input() decimalPlace: number = 4;
  @Input() placeholder: string = "";
  @Input() required = false;
  private _data: any;
  public get data(): any {
    return this._data;
  }
  public set data(v: any) {
    if (v !== this._data) {
      this._data = v;
      this.onChange(v);
    }
  }
  /** 百分比输入框的值 */
  percentageValue: string = "";
  private _valueAccessor: DefaultValueAccessor;
  @Input() maxLength = 2;

  @ViewChild(DefaultValueAccessor, { static: false })
  public get valueAccessor(): DefaultValueAccessor {
    return this._valueAccessor;
  }

  public set valueAccessor(value: DefaultValueAccessor) {
    this._valueAccessor = value;
  }
  @ViewChild("inputElement", { static: false }) inputElement!: ElementRef<HTMLInputElement>;
  constructor() {}
  /** 该方法用于将模型中的新值写入视图或 DOM 属性中 */
  writeValue(obj: any) {
    this.data = obj;
    this.percentageValue = obj ? _.trimEnd(_.trimEnd(((this.data || 0) * 100).toFixed(this.decimalPlace), "0"), ".") : undefined;
    this.valueAccessor && this.valueAccessor.writeValue(obj);
  }
  /** 设置当控件接收到 change 事件后，调用的函数 */
  registerOnChange(fn: (_: any) => void): void {
    this.onChange = fn;
  }
  /** 置当控件接收到 touched 事件后，调用的函数 */
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  /** 当控件状态变成 DISABLED 或从 DISABLED 状态变化成 ENABLE 状态时，会调用该函数。该函数会根据参数值，启用或禁用指定的 DOM 元素 */
  setDisabledState(isDisabled: boolean) {
    this.valueAccessor && this.valueAccessor.setDisabledState(isDisabled);
  }
  onChange = _ => {};
  onTouched = () => {};
  ngOnInit(): void {}
  onPercentageModelChange(value: string) {
    if (value === "") {
      this.data = null;
      return;
    }
    if (value === "-") {
      return;
    }

    if (value.length > 0 && value.charAt(value.length - 1) === "." && value.split(".").length == 2) {
      return;
    }

    // 如果输入有误，如：0.1.2
    if (value.split(".").length > 2) {
      value = `${value.split(".")[0]}.${value.split(".")[1]}`;
    }
    // 如果输入的小数位数超过了设置的精度，则自动清除
    if (value.split(".").length == 2 && value.split(".")[1].length > this.decimalPlace) {
      value = value.substr(0, value.length - 1);
    }

    /** 标记输入值是否超出允许的范围，若超出，才需要强制调整，否则保持原输入值；否则会导致0.01这样的值被自动替换成0，导致无法输入 */
    let isInRange = true;
    let percentage = parseFloat(value) || 0;
    if (percentage > this.option.numberMax) {
      percentage = this.option.numberMax;
      isInRange = false;
    }
    if (percentage < this.option.numberMin) {
      percentage = this.option.numberMin;
      isInRange = false;
    }
    this.inputElement.nativeElement.value = isInRange ? value : `${percentage}`;
    this.data = Number((percentage * 0.01).toPrecision(this.decimalPlace + 2)); // 如果不做toPrecision处理，*0.01浮点处理时，会形成非常多的小数点位数，输入7.2*0.01=0.0072000000001
    this.percentageValue = _.trimEnd(_.trimEnd(((this.data || 0) * 100).toFixed(this.decimalPlace), "0"), ".");
  }
}
