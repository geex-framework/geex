import { AfterViewInit, Component, inject, OnInit, signal, TemplateRef, viewChild } from "@angular/core";
import { Router } from "@angular/router";
import type { STChange, STColumn } from "@delon/abc/st";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { Apollo } from "apollo-angular";

import { StartApprovalFlowService } from "../../components/start-flow/start-flow.component";
import { approvalFlowCategories, approvalFlows, type ApprovalFlowBrief } from "../../graphql/operations.gql";

import { ApprovalFlowStatus, SortEnumType } from "@/gql";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "app-approval-flow-inbox-list",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./list.page.html",
})
export class InboxListPage implements OnInit, AfterViewInit {
  readonly I18N = inject(GEEX_I18N);
  private readonly apollo = inject(Apollo);
  private readonly router = inject(Router);
  private readonly startFlow = inject(StartApprovalFlowService);
  private readonly progressTpl = viewChild.required<TemplateRef<{ $implicit: ApprovalFlowBrief; index: number }>>("progressTpl");
  readonly loading = signal(false);
  readonly data = signal<ApprovalFlowBrief[]>([]);
  readonly total = signal(0);
  readonly columns = signal<Array<STColumn<ApprovalFlowBrief>>>([]);
  filterText = "";
  category: string | null = null;
  categories: string[] = [];
  filterOrgCodes: string[] = [];
  filterStatus: ApprovalFlowStatus | null = null;
  pageIndex = 1;
  pageSize = 10;
  readonly statusOptions = [
    { label: ApprovalFlowStatus.Processing, value: ApprovalFlowStatus.Processing },
    { label: ApprovalFlowStatus.Finished, value: ApprovalFlowStatus.Finished },
    { label: ApprovalFlowStatus.Canceled, value: ApprovalFlowStatus.Canceled },
    { label: ApprovalFlowStatus.Archived, value: ApprovalFlowStatus.Archived },
  ];

  ngOnInit(): void {
    void this.apollo
      .query({ query: approvalFlowCategories, fetchPolicy: "no-cache" })
      .firstValuePromise()
      .then(result => (this.categories = result.data.approvalFlowCategories));
    void this.load();
  }

  ngAfterViewInit(): void {
    this.columns.set([
      { title: this.I18N.ApprovalFlows.name, index: "name" },
      { title: this.I18N.ApprovalFlows.description, index: "description" },
      { title: "流程分类", format: item => item.categories.join(" / ") },
      { title: this.I18N.ApprovalFlows.orgCode, index: "orgCode" },
      {
        title: this.I18N.ApprovalFlows.inboxStatus,
        index: "status",
        format: item => this.statusLabel(item.status),
      },
      {
        title: this.I18N.ApprovalFlows.progress,
        render: this.progressTpl(),
        width: 180,
      },
      { title: this.I18N.ApprovalFlows.createdOn, index: "createdOn", type: "date" },
      {
        title: this.I18N.ApprovalFlows.actions,
        buttons: [
          {
            text: this.I18N.ApprovalFlows.inboxDetail,
            click: item => this.router.navigate(["/approval-flows/inbox", item.id]),
          },
          {
            text: this.I18N.Common.action.edit,
            iif: item => !!item.canEdit,
            click: item => this.router.navigate(["/approval-flows/inbox", item.id, "edit"]),
          },
        ],
      },
    ]);
  }

  progressPercent(item: ApprovalFlowBrief): number {
    if (item.status === ApprovalFlowStatus.Finished) {
      return 100;
    }
    const nodes = (item.nodes ?? []).filter((node): node is NonNullable<typeof node> => node != null);
    if (!nodes.length) {
      return 0;
    }
    const approved = nodes.filter(node => !!node.nodeStatus?.isApproved).length;
    return Math.round((approved / nodes.length) * 100);
  }

  progressStatus(item: ApprovalFlowBrief): "success" | "exception" | "active" | "normal" {
    if (item.status === ApprovalFlowStatus.Canceled) {
      return "exception";
    }
    if (item.status === ApprovalFlowStatus.Finished) {
      return "success";
    }
    return "active";
  }

  progressTooltip(item: ApprovalFlowBrief): string {
    const statusLabel = this.statusLabel(item.status);
    return `${this.I18N.ApprovalFlows.inboxStatus}: ${statusLabel} (${this.progressPercent(item)}%)`;
  }

  statusLabel(status: ApprovalFlowStatus | string): string {
    switch (status) {
      case ApprovalFlowStatus.Finished:
        return this.I18N.ApprovalFlows.statusFinished;
      case ApprovalFlowStatus.Canceled:
        return this.I18N.ApprovalFlows.statusCanceled;
      case ApprovalFlowStatus.Archived:
        return "已归档";
      case ApprovalFlowStatus.Processing:
        return this.I18N.ApprovalFlows.statusProcessing;
      default:
        return String(status);
    }
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
          query: approvalFlows,
          variables: {
            filter: Object.keys(filter).length ? filter : undefined,
            request: { status: this.filterStatus, category: this.category },
            skip: (this.pageIndex - 1) * this.pageSize,
            take: this.pageSize,
            sort: [{ createdOn: SortEnumType.descend }],
          },
          fetchPolicy: "no-cache",
        })
        .firstValuePromise();
      this.data.set((result.data.approvalFlow?.items ?? []).filter((item): item is ApprovalFlowBrief => item != null));
      this.total.set(result.data.approvalFlow?.totalCount ?? 0);
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

  start(): void {
    this.startFlow.open({ onCreated: () => void this.load() });
  }
}
