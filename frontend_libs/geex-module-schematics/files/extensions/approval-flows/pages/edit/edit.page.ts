import { ChangeDetectorRef, Component, inject, OnInit, signal } from "@angular/core";
import { FormArray, FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService } from "ng-zorro-antd/modal";

import { ApprovalFieldEditorComponent } from "../../components/dynamic-form/field-editor.component";
import { ApprovalUiField, cleanFields } from "../../components/dynamic-form/form-model";
import { ApprovalFlowNodesEditorComponent, ApprovalFlowNodeFormValue, createApprovalFlowNodeGroup } from "../../components/nodes-editor";
import {
  approvalFlowTemplateById,
  createApprovalFlowTemplate,
  editApprovalFlowTemplate,
  approvalFlowCategories,
} from "../../graphql/operations.gql";

import type { ApprovalFlowNodeTemplateDataInput } from "@/gql";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "app-approval-flow-template-edit",
  standalone: true,
  imports: [SharedModule, ApprovalFlowNodesEditorComponent, ApprovalFieldEditorComponent],
  templateUrl: "./edit.page.html",
})
export class ApprovalFlowTemplateEditPage implements OnInit {
  readonly I18N = inject(GEEX_I18N);
  private readonly apollo = inject(Apollo);
  private readonly route = inject(ActivatedRoute);
  private readonly message = inject(NzMessageService);
  private readonly modal = inject(NzModalService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly router = inject(Router);
  readonly id = this.route.snapshot.paramMap.get("id");
  readonly saving = signal(false);
  categories: string[] = [];
  readonly form = new FormGroup({
    name: new FormControl("", { nonNullable: true, validators: Validators.required }),
    description: new FormControl("", { nonNullable: true, validators: Validators.required }),
    orgCodes: new FormControl<string[]>([], {
      nonNullable: true,
    }),
    nodes: new FormArray<FormGroup>([]),
    categories: new FormControl<string[]>([], { nonNullable: true }),
    formFields: new FormControl<ApprovalUiField[]>([], { nonNullable: true }),
  });

  get nodes(): FormArray {
    return this.form.controls.nodes;
  }

  async ngOnInit(): Promise<void> {
    const categories = await this.apollo.query({ query: approvalFlowCategories, fetchPolicy: "no-cache" }).firstValuePromise();
    this.categories = categories.data.approvalFlowCategories;
    if (!this.id) {
      this.nodes.push(createApprovalFlowNodeGroup("template"));
      this.form.markAsPristine();
      this.cdr.markForCheck();
      return;
    }
    const result = await this.apollo
      .query({
        query: approvalFlowTemplateById,
        variables: { id: this.id },
        fetchPolicy: "no-cache",
      })
      .firstValuePromise();
    const template = result.data.approvalFlowTemplateById;
    if (!template) {
      this.message.error(this.I18N.ApprovalFlows.notFound);
      await this.router.navigate(["/approval-flows"]);
      return;
    }
    this.form.patchValue({
      name: template.name,
      description: template.description,
      orgCodes: template.orgCode ? [template.orgCode] : [],
      categories: template.categories,
      formFields: cleanFields(template.formFields, true),
    });
    this.nodes.clear();
    const sorted = [...(template.nodes ?? [])].sort((a, b) => a.index - b.index);
    if (!sorted.length) {
      this.nodes.push(createApprovalFlowNodeGroup("template"));
    } else {
      for (const node of sorted) {
        this.nodes.push(
          createApprovalFlowNodeGroup(
            "template",
            {
              id: node.id,
              name: node.name,
              auditRole: node.auditRole,
              index: node.index,
              carbonCopyUserIds: node.carbonCopyUserIds ?? [],
              auditUserId: node.auditUserId ?? "",
              description: node.description ?? "",
              formFields: cleanFields(node.formFields, false),
            },
            node.index,
          ),
        );
      }
    }
    this.form.markAsPristine();
    this.cdr.markForCheck();
  }

  async cancel(): Promise<void> {
    if (!(await this.confirmLeaveIfDirty())) {
      return;
    }
    await this.router.navigate(["/approval-flows"]);
  }

  async submit(): Promise<void> {
    if (this.saving() || this.form.invalid || !this.nodes.length) {
      this.form.markAllAsTouched();
      if (!this.saving()) {
        this.message.error(this.I18N.ApprovalFlows.formInvalid);
      }
      return;
    }
    const value = this.form.getRawValue();
    const orgCode = value.orgCodes?.[0] || undefined;
    const nodes: ApprovalFlowNodeTemplateDataInput[] = value.nodes.map((node: ApprovalFlowNodeFormValue, index: number) => ({
      id: node.id || "",
      name: node.name,
      auditRole: node.auditRole,
      index,
      carbonCopyUserIds: node.carbonCopyUserIds ?? [],
      auditUserId: node.auditUserId || null,
      description: node.description,
      formFields: cleanFields(node.formFields ?? [], false),
    }));
    this.saving.set(true);
    try {
      if (this.id) {
        await this.apollo
          .mutate({
            mutation: editApprovalFlowTemplate,
            variables: {
              request: {
                id: this.id,
                name: value.name,
                description: value.description,
                orgCode,
                approvalFlowNodeTemplates: nodes,
                categories: value.categories,
                formFields: cleanFields(value.formFields, true).map(field => ({ ...field, allowEdit: !!field.allowEdit })),
              },
            },
          })
          .firstValuePromise();
      } else {
        await this.apollo
          .mutate({
            mutation: createApprovalFlowTemplate,
            variables: {
              request: {
                name: value.name,
                description: value.description,
                orgCode,
                approvalFlowNodeTemplates: nodes,
                categories: value.categories,
                formFields: cleanFields(value.formFields, true).map(field => ({ ...field, allowEdit: !!field.allowEdit })),
              },
            },
          })
          .firstValuePromise();
      }
      this.form.markAsPristine();
      this.message.success(this.I18N.ApprovalFlows.saveSuccess);
      await this.router.navigate(["/approval-flows"]);
    } finally {
      this.saving.set(false);
    }
  }

  private confirmLeaveIfDirty(): Promise<boolean> {
    if (!this.form.dirty) {
      return Promise.resolve(true);
    }
    return new Promise(resolve => {
      this.modal.confirm({
        nzTitle: this.I18N.ApprovalFlows.unsavedLeaveConfirm,
        nzOnOk: () => resolve(true),
        nzOnCancel: () => resolve(false),
      });
    });
  }
}
