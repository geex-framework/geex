import { Component, OnInit } from "@angular/core";
import { ControlWidget } from "@delon/form";

@Component({
  selector: "sf-ueditor",
  template: ` <sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
    <!-- 开始自定义控件区域 -->
    <tinymce [ngModel]="value" (ngModelChange)="change($event)" [config]="config"> </tinymce>
    <!-- 结束自定义控件区域 -->
  </sf-item-wrap>`,
  styles: [
    `
      :host ueditor {
        line-height: normal;
      }
    `,
  ],
})
export class UeditorWidget extends ControlWidget implements OnInit {
  /* 用于注册小部件 KEY 值 */
  static readonly KEY = "tinymce";

  // 组件所需要的参数，建议使用 `ngOnInit` 获取
  config: any;
  loadingTip: string;

  ngOnInit(): void {
    this.loadingTip = this.ui.loadingTip || "加载中……";
    this.config = this.ui.config || {
      menu: {
        file: { title: "文件", items: "newdocument" },
        edit: { title: "编辑", items: "undo redo | cut copy paste pastetext | selectall" },
        insert: { title: "插入", items: "link media | template hr" },
        view: { title: "查看", items: "visualaid" },
        format: { title: "格式", items: "bold italic underline strikethrough superscript subscript | formats | removeformat" },
        table: { title: "表格", items: "inserttable tableprops deletetable | cell row column" },
        tools: { title: "工具", items: "spellchecker code" },
      },
    };
  }

  // reset 可以更好的解决表单重置过程中所需要的新数据问题
  reset(value: string) {}

  change(value: string) {
    if (this.ui.change) this.ui.change(value);
    this.setValue(value);
  }
}
