import { Component, EventEmitter, Input, Output } from "@angular/core";

import { approvalFieldTypes, ApprovalUiField } from "./form-model";

import { ApprovalFormFieldType } from "@/gql";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "approval-field-editor",
  standalone: true,
  imports: [SharedModule],
  template: `
    @for (field of value; track field.key; let i = $index) {
      <nz-card class="mb-sm" nzSize="small" [nzTitle]="'字段 ' + (i + 1)" [nzExtra]="actions">
        <ng-template #actions>
          <button nz-button nzType="text" type="button" [disabled]="i === 0" (click)="move(i, -1)" aria-label="上移字段"
            ><i nz-icon nzType="arrow-up"></i
          ></button>
          <button nz-button nzType="text" type="button" [disabled]="i === value.length - 1" (click)="move(i, 1)" aria-label="下移字段"
            ><i nz-icon nzType="arrow-down"></i
          ></button>
          <button nz-button nzType="text" nzDanger type="button" (click)="remove(i)">删除</button>
        </ng-template>
        <div nz-row [nzGutter]="16">
          <div nz-col [nzXs]="24" [nzMd]="12">
            <nz-form-item
              ><nz-form-label nzRequired>字段名称</nz-form-label
              ><nz-form-control>
                <input
                  nz-input
                  [ngModel]="field.name"
                  (ngModelChange)="update(i, 'name', $event)"
                  [ngModelOptions]="standalone"
                /> </nz-form-control
            ></nz-form-item>
          </div>
          <div nz-col [nzXs]="24" [nzMd]="12">
            <nz-form-item
              ><nz-form-label>控件类型</nz-form-label
              ><nz-form-control>
                <nz-select
                  [ngModel]="field.type"
                  (ngModelChange)="update(i, 'type', $event)"
                  [ngModelOptions]="standalone"
                  style="width:100%"
                >
                  @for (type of types; track type.value) {
                    <nz-option [nzValue]="type.value" [nzLabel]="type.label" />
                  }
                </nz-select> </nz-form-control
            ></nz-form-item>
          </div>
          <div nz-col [nzSpan]="24" class="mb-sm">
            <label nz-checkbox [ngModel]="field.required" (ngModelChange)="update(i, 'required', $event)" [ngModelOptions]="standalone"
              >必填</label
            >
            @if (flow) {
              <label nz-checkbox [ngModel]="field.allowEdit" (ngModelChange)="update(i, 'allowEdit', $event)" [ngModelOptions]="standalone"
                >允许修改</label
              >
            }
          </div>
          @if (field.type === kind.Select || field.type === kind.MultiSelect) {
            <div nz-col [nzSpan]="24"
              ><nz-form-item
                ><nz-form-label nzRequired>选项 (每行一项)</nz-form-label
                ><nz-form-control>
                  <textarea
                    nz-input
                    rows="3"
                    [ngModel]="optionText(field)"
                    (ngModelChange)="options(i, $event)"
                    [ngModelOptions]="standalone"
                  ></textarea></nz-form-control></nz-form-item
            ></div>
          }
        </div>
      </nz-card>
    }
    <button nz-button type="button" (click)="add()"><i nz-icon nzType="plus"></i>添加字段</button>
  `,
})
export class ApprovalFieldEditorComponent {
  @Input() value: ApprovalUiField[] = [];
  @Input() flow = false;
  @Output() readonly valueChange = new EventEmitter<ApprovalUiField[]>();
  readonly types = approvalFieldTypes;
  readonly kind = ApprovalFormFieldType;
  readonly standalone = { standalone: true };

  add(): void {
    this.emit([
      ...this.value,
      {
        key: crypto.randomUUID(),
        name: "",
        type: this.kind.Text,
        required: false,
        options: [],
        ...(this.flow ? { allowEdit: false } : {}),
      },
    ]);
  }
  remove(index: number): void {
    this.emit(this.value.filter((_, i) => i !== index));
  }
  move(index: number, direction: number): void {
    const fields = [...this.value];
    [fields[index], fields[index + direction]] = [fields[index + direction], fields[index]];
    this.emit(fields);
  }
  update(index: number, key: string, value: unknown): void {
    this.emit(this.value.map((field, i) => (i === index ? { ...field, [key]: value } : field)));
  }
  options(index: number, text: string): void {
    this.update(index, "options", text.split("\n"));
  }
  optionText(field: ApprovalUiField): string {
    return field.options.join("\n");
  }
  private emit(value: ApprovalUiField[]): void {
    this.value = value;
    this.valueChange.emit(value);
  }
}
