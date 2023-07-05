import { AfterViewInit, Component, EventEmitter, forwardRef, Input, OnInit, Output, SimpleChanges, ViewChild } from "@angular/core";
import { ControlValueAccessor, DefaultValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";
import { ControlWidget } from "@delon/form";
import { Observable, of } from "rxjs";
import { debounceTime } from "rxjs/operators";
import E from "wangeditor";
// [id]="id ? id : 'test'"
@Component({
  selector: "wang-editor",
  template: ` <div id="wangTest"> </div> <input style="display:none;" ngDefaultControl />`,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => WangEditorComponent),
      multi: true,
    },
  ],
})
export class WangEditorComponent implements OnInit, ControlValueAccessor {
  private _valueAccessor: DefaultValueAccessor;
  @ViewChild(DefaultValueAccessor, { static: false })
  public get valueAccessor(): DefaultValueAccessor {
    return this._valueAccessor;
  }

  public set valueAccessor(value: DefaultValueAccessor) {
    this._valueAccessor = value;
  }
  writeValue(obj: any): void {
    this.editor?.txt.html(obj);
    setTimeout(() => {}, 100);
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
  @Input() id: string;
  @Input() $text: Observable<string>;
  @Output() readonly textChange = new EventEmitter();
  editor: E;
  ngOnInit(): void {
    this.editor = new E("#wangTest");
    this.editor.config.zIndex = 1;
    // 隐藏插入网络图片的功能，即只保留上传本地图片
    this.editor.config.showLinkImg = false;
    this.editor.create();
    this.editor.txt.eventHooks.changeEvents.push(() => {
      this.onChange(this.editor.txt.html());
    });
  }
  onChange = _ => {};
  onTouched = () => {};
}
