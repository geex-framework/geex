import { Component, EventEmitter, inject, Input, OnChanges, OnDestroy, Output, QueryList, ViewChildren } from "@angular/core";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { NzEmptyModule } from "ng-zorro-antd/empty";
import { NzMessageService } from "ng-zorro-antd/message";
import { Subscription } from "rxjs";

import { ApprovalUiField, ApprovalUiValue } from "./form-model";

import { ApprovalFormFieldType } from "@/gql";
import { GeexUploadComponent } from "@/modules/blob-storage/components/upload";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "approval-dynamic-form",
  standalone: true,
  imports: [SharedModule, NzEmptyModule],
  template: `
    <div nz-form nzLayout="vertical" [formGroup]="form">
      @for (field of fields; track field.key) {
        <nz-form-item
          ><nz-form-label [nzRequired]="field.required">{{ field.name }}</nz-form-label>
          <nz-form-control [nzErrorTip]="'请填写' + field.name">
            @switch (field.type) {
              @case (kind.MultilineText) {
                <textarea nz-input rows="3" [formControlName]="field.key"></textarea>
              }
              @case (kind.Select) {
                <nz-select [formControlName]="field.key" nzAllowClear style="width:100%">
                  @for (option of field.options; track option) {
                    <nz-option [nzValue]="option" [nzLabel]="option" />
                  }
                </nz-select>
              }
              @case (kind.MultiSelect) {
                <nz-select [formControlName]="field.key" nzMode="multiple" style="width:100%">
                  @for (option of field.options; track option) {
                    <nz-option [nzValue]="option" [nzLabel]="option" />
                  }
                </nz-select>
              }
              @case (kind.Date) {
                <nz-date-picker [formControlName]="field.key" nzFormat="yyyy-MM-dd" style="width:100%" />
              }
              @case (kind.User) {
                <nz-select [formControlName]="field.key" user-select nzAllowClear style="width:100%" />
              }
              @case (kind.File) {
                <geex-upload
                  [formControlName]="field.key"
                  [nzLimit]="0"
                  [nzMultiple]="true"
                  [nzShowButton]="form.controls[field.key].enabled"
                >
                  <ng-template #uploadButton>
                    <button nz-button type="button" [disabled]="form.controls[field.key].disabled"
                      ><nz-icon nzType="upload" />选择文件</button
                    >
                  </ng-template>
                </geex-upload>
              }
              @default {
                <input nz-input [formControlName]="field.key" />
              }
            }
          </nz-form-control>
        </nz-form-item>
      } @empty {
        <nz-empty nzNotFoundContent="暂无表单字段" />
      }
    </div>
  `,
})
export class ApprovalDynamicFormComponent implements OnChanges, OnDestroy {
  @ViewChildren(GeexUploadComponent) uploads?: QueryList<GeexUploadComponent>;
  private readonly message = inject(NzMessageService);
  @Input() fields: ApprovalUiField[] = [];
  @Input() values: ApprovalUiValue[] = [];
  @Input() editable = false;
  @Input() creating = false;
  @Input() flow = false;
  @Output() readonly valuesChange = new EventEmitter<ApprovalUiValue[]>();
  readonly kind = ApprovalFormFieldType;
  form = new FormGroup<Record<string, FormControl>>({});
  private subscription?: Subscription;
  private emitted?: ApprovalUiValue[];
  private lastFields?: ApprovalUiField[];
  private lastAccess = "";

  ngOnChanges(): void {
    const access = `${this.editable}/${this.creating}/${this.flow}`;
    if (this.values === this.emitted && this.fields === this.lastFields && access === this.lastAccess) return;
    this.subscription?.unsubscribe();
    this.lastFields = this.fields;
    this.lastAccess = access;
    const controls: Record<string, FormControl> = {};
    for (const field of this.fields) {
      const values = this.values.find(value => value.key === field.key)?.values ?? [];
      const value =
        field.type === this.kind.Date
          ? values[0]
            ? new Date(`${values[0]}T00:00:00`)
            : null
          : field.type === this.kind.MultiSelect || field.type === this.kind.File
            ? [...values]
            : (values[0] ?? "");
      const disabled = !this.editable || (this.flow && !this.creating && !field.allowEdit);
      controls[field.key] = new FormControl({ value, disabled }, field.required ? Validators.required : null);
    }
    this.form = new FormGroup(controls);
    this.subscription = this.form.valueChanges.subscribe(() => {
      this.emitted = this.readValues();
      this.valuesChange.emit(this.emitted);
    });
  }

  readValues(): ApprovalUiValue[] {
    const data = this.form.getRawValue();
    return this.fields.map(field => {
      const value = data[field.key];
      let values: string[];
      if (value instanceof Date) {
        values = [`${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, "0")}-${String(value.getDate()).padStart(2, "0")}`];
      } else values = Array.isArray(value) ? [...value] : value == null || value === "" ? [] : [String(value)];
      return { key: field.key, values };
    });
  }

  filesReady(): boolean {
    const files = this.uploads?.toArray().flatMap(upload => upload.fileList) ?? [];
    if (files.some(file => file.status === "uploading")) {
      this.message.warning("文件仍在上传, 请等待完成");
      return false;
    }
    if (files.some(file => file.status === "error")) {
      this.message.warning("请重新上传或移除失败的文件");
      return false;
    }
    return true;
  }
  validate(requireComplete = true): boolean {
    this.form.markAllAsTouched();
    return this.filesReady() && (!requireComplete || this.form.valid);
  }
  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }
}
