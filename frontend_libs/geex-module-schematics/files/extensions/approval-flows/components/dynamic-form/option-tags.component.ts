import { Component, EventEmitter, Input, Output, inject } from "@angular/core";
import { NzMessageService } from "ng-zorro-antd/message";

import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "approval-option-tags",
  standalone: true,
  imports: [SharedModule],
  template: `
    <div class="option-tags">
      @for (option of value; track $index; let i = $index) {
        <nz-tag nzMode="closeable" (nzOnClose)="remove(i, $event)">
          <input
            nz-input
            class="option-text"
            [attr.aria-label]="'选项 ' + (i + 1)"
            [style.width.em]="option.length + 1"
            [ngModel]="option"
            (ngModelChange)="edit(i, $event)"
            (blur)="trim(i)"
            (keydown.enter)="preventSubmit($event)"
            [ngModelOptions]="standalone"
          />
        </nz-tag>
      }
      <input
        nz-input
        class="new-option"
        aria-label="新增选项"
        placeholder="输入选项后按回车"
        [(ngModel)]="draft"
        [ngModelOptions]="standalone"
        (keydown.enter)="addOnEnter($event)"
        (blur)="add()"
      />
      <button nz-button nzType="dashed" type="button" (click)="add()"><i nz-icon nzType="plus"></i>添加选项</button>
    </div>
    <div class="ant-form-item-extra">点击标签文字可修改, 点击叉号可删除.</div>
    @if (invalid) {
      <div class="ant-form-item-explain-error" role="alert">选项不能为空或重复.</div>
    }
  `,
  styles: `
    .option-tags {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
    }
    nz-tag {
      display: inline-flex;
      align-items: center;
      max-width: 100%;
      margin: 0;
    }
    .option-text {
      min-width: 3ch;
      max-width: min(32ch, 100%);
      padding: 0;
      border: 0;
      background: transparent;
      color: inherit;
    }
    .new-option {
      width: 200px;
      max-width: 100%;
    }
  `,
})
export class ApprovalOptionTagsComponent {
  @Input() value: string[] = [];
  @Output() readonly valueChange = new EventEmitter<string[]>();
  private readonly message = inject(NzMessageService);
  readonly standalone = { standalone: true };
  draft = "";

  get invalid(): boolean {
    return this.value.some(value => !value.trim()) || new Set(this.value).size !== this.value.length;
  }

  preventSubmit(event: Event): void {
    if (!(event as KeyboardEvent).isComposing) event.preventDefault();
  }

  addOnEnter(event: Event): void {
    if ((event as KeyboardEvent).isComposing) return;
    event.preventDefault();
    this.add();
  }

  add(): void {
    const option = this.draft.trim();
    if (!option) return;
    if (this.value.includes(option)) {
      this.message.warning("选项已存在");
      return;
    }
    this.emit([...this.value, option]);
    this.draft = "";
  }

  edit(index: number, option: string): void {
    this.emit(this.value.map((value, i) => (i === index ? option : value)));
  }

  trim(index: number): void {
    if (this.value[index] !== undefined) this.edit(index, this.value[index].trim());
  }

  remove(index: number, event: MouseEvent): void {
    event.preventDefault();
    this.emit(this.value.filter((_, i) => i !== index));
  }

  private emit(value: string[]): void {
    this.value = value;
    this.valueChange.emit(value);
  }
}
