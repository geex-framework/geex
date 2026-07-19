import { Component, inject, OnInit } from "@angular/core";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import type { ApprovalFlowNodeTemplateDataInput } from "@/gql";
import { SharedModule } from "@/shared/shared.module";
import {
  approvalFlowTemplateById,
  createApprovalFlowTemplate,
  editApprovalFlowTemplate,
} from "../../graphql/operations.gql";

@Component({
  selector: "app-approval-flow-template-edit",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./edit.page.html",
})
export class ApprovalFlowTemplateEditPage implements OnInit {
  private readonly apollo = inject(Apollo);
  private readonly route = inject(ActivatedRoute);
  private readonly message = inject(NzMessageService);
  readonly router = inject(Router);
  readonly id = this.route.snapshot.paramMap.get("id");
  readonly form = new FormGroup({
    name: new FormControl("", { nonNullable: true, validators: Validators.required }),
    description: new FormControl("", { nonNullable: true, validators: Validators.required }),
    orgCode: new FormControl("", { nonNullable: true, validators: Validators.required }),
    nodesJson: new FormControl("[]", { nonNullable: true, validators: Validators.required }),
  });

  async ngOnInit(): Promise<void> {
    if (!this.id) return;
    const result = await this.apollo.query({
      query: approvalFlowTemplateById,
      variables: { id: this.id },
      fetchPolicy: "no-cache",
    }).firstValuePromise();
    const template = result.data.approvalFlowTemplateById;
    if (!template) return;
    this.form.setValue({
      name: template.name,
      description: template.description,
      orgCode: template.orgCode,
      nodesJson: JSON.stringify(template.nodes, null, 2),
    });
  }

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    let nodes: ApprovalFlowNodeTemplateDataInput[];
    try {
      nodes = JSON.parse(this.form.controls.nodesJson.value) as ApprovalFlowNodeTemplateDataInput[];
      if (!Array.isArray(nodes)) throw new Error("Nodes must be an array");
    } catch {
      this.message.error("Approval nodes must be a valid JSON array");
      return;
    }
    const value = this.form.getRawValue();
    if (this.id) {
      await this.apollo.mutate({
        mutation: editApprovalFlowTemplate,
        variables: { request: { id: this.id, name: value.name, description: value.description, orgCode: value.orgCode, approvalFlowNodeTemplates: nodes } },
      }).firstValuePromise();
    } else {
      await this.apollo.mutate({
        mutation: createApprovalFlowTemplate,
        variables: { request: { name: value.name, description: value.description, orgCode: value.orgCode, approvalFlowNodeTemplates: nodes } },
      }).firstValuePromise();
    }
    this.message.success("Approval flow template saved");
    await this.router.navigate(["/approval-flows"]);
  }
}
