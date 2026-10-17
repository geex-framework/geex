import { ChangeDetectorRef, Component, inject, OnInit, signal } from "@angular/core";
import { Router } from "@angular/router";
import type { STChange, STColumn } from "@delon/abc/st";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";

import { StartApprovalFlowService } from "../../components/start-flow/start-flow.component";
import {
  approvalFlowCategories,
  approvalFlowTemplates,
  deleteApprovalFlowTemplate,
  type ApprovalFlowTemplateDetail,
} from "../../graphql/operations.gql";

import { SortEnumType } from "@/gql";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "app-approval-flow-template-list",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./list.page.html",
})
export class ApprovalFlowTemplateListPage implements OnInit {
  readonly I18N = inject(GEEX_I18N);
  private readonly apollo = inject(Apollo);
  private readonly message = inject(NzMessageService);
  private readonly router = inject(Router);
  private readonly startFlow = inject(StartApprovalFlowService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly loading = signal(false);
  readonly data = signal<ApprovalFlowTemplateDetail[]>([]);
  readonly total = signal(0);
  filterText = "";
  category: string | null = null;
  categories: string[] = [];
  filterOrgCodes: string[] = [];
  pageIndex = 1;
  pageSize = 10;
  readonly columns: Array<STColumn<ApprovalFlowTemplateDetail>> = [
    { title: this.I18N.ApprovalFlows.name, index: "name" },
    { title: this.I18N.ApprovalFlows.description, index: "description" },
    { title: "流程分类", format: item => item.categories.join(" / ") },
    { title: this.I18N.ApprovalFlows.orgCode, index: "orgCode" },
    { title: this.I18N.ApprovalFlows.nodes, index: "nodes.length", type: "number" },
    { title: this.I18N.ApprovalFlows.createdOn, index: "createdOn", type: "date" },
    {
      title: this.I18N.ApprovalFlows.actions,
      buttons: [
        { text: "发起审批", click: item => this.startFlow.open({ templateId: item.id }) },
        { text: this.I18N.Common.action.edit, click: item => this.router.navigate(["/approval-flows/edit", item.id]) },
        { text: this.I18N.Common.action.delete, type: "del", click: item => this.delete(item.id) },
      ],
    },
  ];

  ngOnInit(): void {
    void this.apollo
      .query({ query: approvalFlowCategories, fetchPolicy: "no-cache" })
      .firstValuePromise()
      .then(result => {
        this.categories = result.data.approvalFlowCategories;
        this.cdr.markForCheck();
      });
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const orgCode = this.filterOrgCodes?.[0];
      const filter: Record<string, unknown> = {};
      if (this.filterText) {
        filter["name"] = { contains: this.filterText };
      }
      if (orgCode) {
        filter["orgCode"] = { eq: orgCode };
      }
      const result = await this.apollo
        .query({
          query: approvalFlowTemplates,
          variables: {
            filter: Object.keys(filter).length ? filter : undefined,
            request: { orgCode, category: this.category },
            skip: (this.pageIndex - 1) * this.pageSize,
            take: this.pageSize,
            sort: [{ createdOn: SortEnumType.descend }],
          },
          fetchPolicy: "no-cache",
        })
        .firstValuePromise();
      this.data.set((result.data.approvalFlowTemplate?.items ?? []).filter((item): item is ApprovalFlowTemplateDetail => item != null));
      this.total.set(result.data.approvalFlowTemplate?.totalCount ?? 0);
    } finally {
      this.loading.set(false);
    }
  }

  onTableChange(change: STChange): void {
    if (change.type === "pi" || change.type === "ps") {
      this.pageIndex = change.pi ?? this.pageIndex;
      this.pageSize = change.ps ?? this.pageSize;
      void this.load();
    }
  }

  add(): void {
    void this.router.navigate(["/approval-flows/edit"]);
  }

  async delete(id: string): Promise<void> {
    await this.apollo.mutate({ mutation: deleteApprovalFlowTemplate, variables: { ids: [id] } }).firstValuePromise();
    this.message.success(this.I18N.ApprovalFlows.deleteSuccess);
    await this.load();
  }
}
