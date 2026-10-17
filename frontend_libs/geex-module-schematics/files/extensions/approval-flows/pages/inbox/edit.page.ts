import { Component, inject, OnInit, signal } from "@angular/core";
import { FormArray, FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService } from "ng-zorro-antd/modal";

import { ApprovalFlowNodesEditorComponent, ApprovalFlowNodeFormValue, createApprovalFlowNodeGroup } from "../../components/nodes-editor";
import { approvalFlowById, editApprovalFlow } from "../../graphql/operations.gql";

import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "app-approval-flow-inbox-edit",
  standalone: true,
  imports: [SharedModule, ApprovalFlowNodesEditorComponent],
  templateUrl: "./edit.page.html",
})
export class InboxEditPage implements OnInit {
  readonly I18N = inject(GEEX_I18N);
  private readonly apollo = inject(Apollo);
  private readonly route = inject(ActivatedRoute);
  private readonly message = inject(NzMessageService);
  private readonly modal = inject(NzModalService);
  readonly router = inject(Router);
  readonly id = this.route.snapshot.paramMap.get("id");
  readonly saving = signal(false);
  readonly form = new FormGroup({
    name: new FormControl("", { nonNullable: true, validators: Validators.required }),
    description: new FormControl("", { nonNullable: true, validators: Validators.required }),
    nodes: new FormArray<FormGroup>([]),
  });

  get nodes(): FormArray {
    return this.form.controls.nodes;
  }

  async ngOnInit(): Promise<void> {
    if (!this.id) {
      await this.router.navigate(["/approval-flows/inbox"]);
      return;
    }
    const result = await this.apollo
      .query({
        query: approvalFlowById,
        variables: { id: this.id },
        fetchPolicy: "no-cache",
      })
      .firstValuePromise();
    const flow = result.data.approvalFlowById;
    if (!flow) {
      this.message.error(this.I18N.ApprovalFlows.notFound);
      await this.router.navigate(["/approval-flows/inbox"]);
      return;
    }
    if (!flow.canEdit || !flow.canEditForm) {
      this.message.error(this.I18N.ApprovalFlows.cannotEdit);
      await this.router.navigate(["/approval-flows/inbox", flow.id], { queryParamsHandling: "preserve" });
      return;
    }
    this.form.patchValue({
      name: flow.name,
      description: flow.description ?? "",
    });
    this.nodes.clear();
    const sorted = [...(flow.nodes ?? [])].sort((a, b) => a.index - b.index);
    if (!sorted.length) {
      this.nodes.push(createApprovalFlowNodeGroup("runtime"));
    } else {
      for (const node of sorted) {
        this.nodes.push(
          createApprovalFlowNodeGroup(
            "runtime",
            {
              id: node.id,
              name: node.name,
              description: node.description ?? "",
              auditUserId: node.auditUserId ?? "",
              auditRole: node.auditRole ?? "",
              index: node.index,
              carbonCopyUserIds: node.carbonCopyUserIds ?? [],
            },
            node.index,
          ),
        );
      }
    }
    this.form.markAsPristine();
  }

  async cancel(): Promise<void> {
    if (!(await this.confirmLeaveIfDirty())) {
      return;
    }
    await this.router.navigate(["/approval-flows/inbox", this.id], { queryParamsHandling: "preserve" });
  }

  async submit(): Promise<void> {
    if (this.saving() || !this.id || this.form.invalid || !this.nodes.length) {
      this.form.markAllAsTouched();
      if (!this.saving()) {
        this.message.error(this.I18N.ApprovalFlows.formInvalid);
      }
      return;
    }
    const value = this.form.getRawValue();
    this.saving.set(true);
    try {
      await this.apollo
        .mutate({
          mutation: editApprovalFlow,
          variables: {
            request: {
              id: this.id,
              name: value.name,
              description: value.description,
              nodes: value.nodes.map((node: ApprovalFlowNodeFormValue, index: number) => ({
                id: node.id,
                name: node.name,
                description: node.description,
                auditUserId: node.auditUserId,
                auditRole: node.auditRole || null,
                index,
                carbonCopyUserIds: node.carbonCopyUserIds ?? [],
              })),
            },
          },
        })
        .firstValuePromise();
      this.form.markAsPristine();
      this.message.success(this.I18N.ApprovalFlows.saveSuccess);
      await this.router.navigate(["/approval-flows/inbox", this.id], { queryParamsHandling: "preserve" });
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
