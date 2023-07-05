import { Component, ContentChild, Input, OnInit, Optional, TemplateRef, ViewChild } from "@angular/core";
/**
 * @description 用来渲染template
 */
@Component({
  template: "<ng-template [ngTemplateOutlet]='template'></ng-template>",
  styles: [],
  host: {
    "(click)": "hostClick($event)",
  },
})
export class RenderTemplateComponent implements OnInit {
  @Input() template: TemplateRef<any>; // 接收需要渲染的template
  @Input() hostClick: ($event: MouseEvent) => any;
  constructor() {}
  @Input() public params: any;
  ngOnInit(): void {
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
    this.template = this.params?.data?.template;
    this.hostClick = this.params?.data.hostClick;
  }
}

/**
 * @description 用来提取template, 相比ng-template, 此组件可以扩展属性 泛型参数用于约束slot名称,
 */
@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: "template",
  template: "<ng-template #content><ng-content></ng-content></ng-template>",
  styles: [],
})
export class TemplateComponent<T = string> implements OnInit {
  @Input() slot: T;
  @ViewChild("content") content: TemplateRef<any>;
  constructor() {}
  ngOnInit(): void {}
}
