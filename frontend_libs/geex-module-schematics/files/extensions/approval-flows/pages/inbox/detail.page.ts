import { Component, computed, inject, OnInit, QueryList, signal, ViewChildren } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { Apollo } from "apollo-angular";
import { NzDescriptionsModule } from "ng-zorro-antd/descriptions";
import { NzEmptyModule } from "ng-zorro-antd/empty";
import { NzMessageService } from "ng-zorro-antd/message";

import { ApprovalDynamicFormComponent } from "../../components/dynamic-form/dynamic-form.component";
import { ApprovalUiValue, cleanValues } from "../../components/dynamic-form/form-model";
import {
  approvalFlowById,
  approveApprovalFlowNode,
  rejectApprovalFlowNode,
  saveApprovalFlowForm,
  saveApprovalFlowNodeForm,
  consultApprovalFlowNodeUsers,
  replyApprovalFlowNode,
  transferApprovalFlowNode,
  carbonCopyApprovalFlowNode,
  cancelApprovalFlow,
  markApprovalFlowNodeViewed,
  type ApprovalFlowDetail,
  type ApprovalFlowNodeDetail,
} from "../../graphql/operations.gql";

import { ApprovalFlowNodeLogType, ApprovalFlowStatus } from "@/gql";
import { SharedModule } from "@/shared/shared.module";

type NodeAction = "approve" | "reject" | "transfer" | "consult" | "reply" | "cc" | "cancel";

@Component({
  selector: "app-approval-flow-inbox-detail",
  standalone: true,
  imports: [SharedModule, NzDescriptionsModule, NzEmptyModule, ApprovalDynamicFormComponent],
  templateUrl: "./detail.page.html",
})
export class InboxDetailPage implements OnInit {
  @ViewChildren(ApprovalDynamicFormComponent) forms?: QueryList<ApprovalDynamicFormComponent>;
  readonly I18N = inject(GEEX_I18N);
  private readonly apollo = inject(Apollo);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly message = inject(NzMessageService);
  readonly loading = signal(false);
  readonly flow = signal<ApprovalFlowDetail | null>(null);
  readonly timelineItems = computed(() =>
    (this.flow()?.nodes ?? [])
      .flatMap(node => node.chatLogs.map(log => ({ ...log, nodeId: node.id, nodeName: node.name, nodeIndex: node.index })))
      .sort((a, b) => new Date(b.creationTime).getTime() - new Date(a.creationTime).getTime()),
  );
  readonly titles: Record<NodeAction, string> = {
    approve: "通过审批",
    reject: "驳回",
    transfer: "转交",
    consult: "征询",
    reply: "回复征询",
    cc: "添加抄送",
    cancel: "取消流程",
  };
  flowValues: ApprovalUiValue[] = [];
  nodeValues: Record<string, ApprovalUiValue[]> = {};
  expanded = new Set<string>();
  saving = false;
  nodeAction: NodeAction | null = null;
  actionNode: ApprovalFlowNodeDetail | null = null;
  actionMessage = "";
  selectedUser = "";
  selectedUsers: string[] = [];
  targetNodeId = "";

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    const id = this.route.snapshot.paramMap.get("id");
    if (!id) return;
    this.loading.set(true);
    try {
      const result = await this.apollo.query({ query: approvalFlowById, variables: { id }, fetchPolicy: "no-cache" }).firstValuePromise();
      const flow = result.data.approvalFlowById ?? null;
      if (flow?.activeNode?.canOperate && !flow.activeNode.nodeStatus?.isViewed) {
        await this.apollo.mutate({ mutation: markApprovalFlowNodeViewed, variables: { nodeId: flow.activeNode.id } }).firstValuePromise();
        await this.load();
        return;
      }
      const previousActive = this.flow()?.activeNode?.id;
      this.flow.set(flow);
      this.flowValues = cleanValues(flow?.formValues ?? []);
      this.nodeValues = Object.fromEntries((flow?.nodes ?? []).map(node => [node.id, cleanValues(node.formValues)]));
      if (flow?.activeNode && previousActive !== flow.activeNode.id) this.expanded.add(flow.activeNode.id);
    } finally {
      this.loading.set(false);
    }
  }

  statusLabel(status: ApprovalFlowStatus | string): string {
    return (
      ({ Processing: "审批中", Finished: "已完成", Canceled: "已取消", Archived: "已归档" } as Record<string, string>)[status] ?? status
    );
  }

  entityStatusLabel(status: string): string {
    return ({ Default: "待上报", Submitted: "已上报", Approved: "已审核" } as Record<string, string>)[status] ?? status;
  }

  nodeStatusLabel(node: ApprovalFlowNodeDetail): string {
    if (node.nodeStatus?.isTransferred) return "已转交";
    if (node.nodeStatus?.isRejected) return "已驳回";
    if (node.nodeStatus?.isApproved) return "已通过";
    if (node.nodeStatus?.isConsulting) return "征询中";
    return node.nodeStatus?.isStarted ? "待审批" : "未激活";
  }

  userLabel(user?: { username: string; nickname?: string | null } | null, id?: string | null): string {
    if (user) return user.nickname || user.username;
    for (const node of this.flow()?.nodes ?? []) {
      if (node.auditUser?.id === id) return node.auditUser.nickname || node.auditUser.username;
      for (const log of node.chatLogs) {
        if (log.from?.id === id) return log.from.nickname || log.from.username;
        if (log.to?.id === id) return log.to.nickname || log.to.username;
      }
    }
    return id || "—";
  }

  logTypeLabel(type: ApprovalFlowNodeLogType): string {
    return this.I18N.ApprovalFlows.logType.get(type, type);
  }
  logColor(type: ApprovalFlowNodeLogType): string {
    return type === ApprovalFlowNodeLogType.Approve ? "green" : type === ApprovalFlowNodeLogType.Reject ? "red" : "blue";
  }
  toggle(id: string, open: boolean): void {
    if (open) this.expanded.add(id);
    else this.expanded.delete(id);
  }
  rejectTargets(): ApprovalFlowNodeDetail[] {
    return (this.flow()?.nodes ?? []).filter(node => node.index <= (this.actionNode?.index ?? -1));
  }

  async saveFlow(form: ApprovalDynamicFormComponent): Promise<void> {
    if (!this.flow() || !form.validate() || this.saving || !this.filesReady()) return;
    this.saving = true;
    try {
      await this.apollo
        .mutate({ mutation: saveApprovalFlowForm, variables: { id: this.flow()!.id, values: form.readValues() } })
        .firstValuePromise();
      this.message.success("流程表单已保存");
      await this.load();
    } finally {
      this.saving = false;
    }
  }

  async saveNode(node: ApprovalFlowNodeDetail): Promise<void> {
    if (this.saving || !this.filesReady()) return;
    this.saving = true;
    try {
      await this.apollo
        .mutate({ mutation: saveApprovalFlowNodeForm, variables: { nodeId: node.id, values: cleanValues(this.nodeValues[node.id]) } })
        .firstValuePromise();
      this.message.success("节点表单已保存");
      await this.load();
    } finally {
      this.saving = false;
    }
  }

  openAction(action: NodeAction, node?: ApprovalFlowNodeDetail): void {
    this.nodeAction = action;
    this.actionNode = node ?? null;
    this.actionMessage = "";
    this.selectedUser = "";
    this.selectedUsers = [];
    this.targetNodeId = "";
  }

  async submitAction(): Promise<void> {
    if (!this.nodeAction || this.saving || !this.filesReady()) return;
    const action = this.nodeAction;
    const nodeId = this.actionNode?.id ?? "";
    const message = this.actionMessage.trim();
    if ((["reject", "consult", "reply"] as NodeAction[]).includes(action) && !message) {
      this.message.warning("请填写内容");
      return;
    }
    if ((action === "transfer" || action === "cc") && !this.selectedUser) {
      this.message.warning("请选择用户");
      return;
    }
    if (action === "consult" && !this.selectedUsers.length) {
      this.message.warning("请选择征询对象");
      return;
    }
    if (action === "reject" && !this.targetNodeId) {
      this.message.warning("请选择驳回节点");
      return;
    }
    this.saving = true;
    try {
      switch (action) {
        case "approve":
          if (this.actionNode?.canEditForm)
            await this.apollo
              .mutate({ mutation: saveApprovalFlowNodeForm, variables: { nodeId, values: cleanValues(this.nodeValues[nodeId]) } })
              .firstValuePromise();
          await this.apollo.mutate({ mutation: approveApprovalFlowNode, variables: { nodeId, message } }).firstValuePromise();
          break;
        case "reject":
          await this.apollo
            .mutate({ mutation: rejectApprovalFlowNode, variables: { nodeId, message, targetNodeId: this.targetNodeId } })
            .firstValuePromise();
          break;
        case "transfer":
          await this.apollo
            .mutate({ mutation: transferApprovalFlowNode, variables: { nodeId, userId: this.selectedUser } })
            .firstValuePromise();
          break;
        case "cc":
          await this.apollo
            .mutate({ mutation: carbonCopyApprovalFlowNode, variables: { nodeId, userId: this.selectedUser } })
            .firstValuePromise();
          break;
        case "consult":
          await this.apollo
            .mutate({ mutation: consultApprovalFlowNodeUsers, variables: { nodeId, userIds: this.selectedUsers, message } })
            .firstValuePromise();
          break;
        case "reply":
          await this.apollo.mutate({ mutation: replyApprovalFlowNode, variables: { nodeId, message } }).firstValuePromise();
          break;
        case "cancel":
          await this.apollo.mutate({ mutation: cancelApprovalFlow, variables: { id: this.flow()!.id } }).firstValuePromise();
          break;
      }
      this.nodeAction = null;
      this.message.success("操作成功");
      await this.load();
    } finally {
      this.saving = false;
    }
  }

  private filesReady(): boolean {
    return this.forms?.toArray().every(form => form.filesReady()) ?? true;
  }

  edit(): void {
    void this.router.navigate(["/approval-flows/inbox", this.flow()?.id, "edit"], { queryParamsHandling: "preserve" });
  }
  back(): void {
    const returnTo = this.route.snapshot.queryParamMap.get("returnTo");
    void this.router.navigateByUrl(returnTo?.startsWith("/") && !returnTo.startsWith("//") ? returnTo : "/approval-flows/inbox");
  }
}
