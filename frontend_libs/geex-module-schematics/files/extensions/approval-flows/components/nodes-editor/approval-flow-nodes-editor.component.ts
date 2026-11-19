import { Component, ElementRef, inject, Input } from "@angular/core";
import { AbstractControl, FormArray, FormControl, FormGroup, Validators } from "@angular/forms";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { NzMessageService } from "ng-zorro-antd/message";

import { ApprovalFieldEditorComponent } from "../dynamic-form/field-editor.component";
import { ApprovalUiField } from "../dynamic-form/form-model";

import { SharedModule } from "@/shared/shared.module";

export type ApprovalFlowNodeEditorMode = "template" | "runtime";

export type ApprovalFlowNodeFormValue = {
  id: string;
  name: string;
  auditRole?: string;
  auditUserId?: string;
  index: number;
  carbonCopyUserIds: string[];
  description?: string;
  formFields?: ApprovalUiField[];
};

export function createApprovalFlowNodeGroup(
  mode: ApprovalFlowNodeEditorMode,
  value?: Partial<ApprovalFlowNodeFormValue>,
  index = 0,
): FormGroup {
  const base = {
    id: new FormControl(value?.id ?? "", { nonNullable: true }),
    name: new FormControl(value?.name ?? "", { nonNullable: true, validators: Validators.required }),
    index: new FormControl(value?.index ?? index, { nonNullable: true, validators: Validators.required }),
    carbonCopyUserIds: new FormControl<string[]>(value?.carbonCopyUserIds ?? [], { nonNullable: true }),
    description: new FormControl(value?.description ?? "", { nonNullable: true }),
    formFields: new FormControl<ApprovalUiField[]>(value?.formFields ?? [], { nonNullable: true }),
  };
  if (mode === "template") {
    return new FormGroup({
      ...base,
      auditRole: new FormControl(value?.auditRole ?? "", {
        nonNullable: true,
      }),
      auditUserId: new FormControl(value?.auditUserId ?? "", { nonNullable: true }),
    });
  }
  return new FormGroup({
    ...base,
    auditUserId: new FormControl(value?.auditUserId ?? "", {
      nonNullable: true,
      validators: Validators.required,
    }),
    auditRole: new FormControl(value?.auditRole ?? "", { nonNullable: true }),
  });
}

@Component({
  selector: "app-approval-flow-nodes-editor",
  standalone: true,
  imports: [SharedModule, ApprovalFieldEditorComponent],
  templateUrl: "./approval-flow-nodes-editor.component.html",
  styles: [
    `
      .node-card {
        margin-bottom: 12px;
      }
      .node-card-extra {
        display: inline-flex;
        gap: 4px;
        align-items: center;
      }
    `,
  ],
})
export class ApprovalFlowNodesEditorComponent {
  readonly I18N = inject(GEEX_I18N);
  private readonly message = inject(NzMessageService);
  private readonly host = inject(ElementRef<HTMLElement>);

  @Input({ required: true }) nodes!: FormArray;
  @Input() mode: ApprovalFlowNodeEditorMode = "template";

  asGroup(control: AbstractControl): FormGroup {
    return control as FormGroup;
  }

  setFields(node: AbstractControl, fields: ApprovalUiField[]): void {
    node.get("formFields")!.setValue(fields);
    node.markAsDirty();
  }

  nodeTitle(index: number): string {
    return `${this.I18N.ApprovalFlows.nodeTitle} #${index + 1} / ${this.nodes.length}`;
  }

  addNode(): void {
    this.nodes.push(createApprovalFlowNodeGroup(this.mode, undefined, this.nodes.length));
    this.reindex();
    this.nodes.markAsDirty();
    queueMicrotask(() => this.focusNodeName(this.nodes.length - 1));
  }

  removeNode(index: number): void {
    if (this.nodes.length <= 1) {
      this.message.warning(this.I18N.ApprovalFlows.atLeastOneNode);
      return;
    }
    this.nodes.removeAt(index);
    this.reindex();
    this.nodes.markAsDirty();
  }

  moveUp(index: number): void {
    if (index <= 0) {
      return;
    }
    this.swap(index, index - 1);
  }

  moveDown(index: number): void {
    if (index >= this.nodes.length - 1) {
      return;
    }
    this.swap(index, index + 1);
  }

  private swap(from: number, to: number): void {
    const current = this.nodes.at(from);
    this.nodes.removeAt(from);
    this.nodes.insert(to, current);
    this.reindex();
    this.nodes.markAsDirty();
  }

  private reindex(): void {
    this.nodes.controls.forEach((control, i) => {
      (control as FormGroup).controls["index"].setValue(i, { emitEvent: false });
    });
  }

  private focusNodeName(index: number): void {
    const card = this.host.nativeElement.querySelector(`[data-node-index="${index}"]`);
    const input = card?.querySelector("input") as HTMLInputElement | null;
    input?.focus();
    input?.scrollIntoView({ behavior: "smooth", block: "center" });
  }
}
