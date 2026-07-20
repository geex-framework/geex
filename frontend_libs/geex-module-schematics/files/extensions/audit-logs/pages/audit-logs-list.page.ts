import { Component, Injector } from "@angular/core";
import { STColumn } from "@delon/abc/st";
import { geex, ListPageLayoutComponent, RoutedListComponent, RouteParamsMappings } from "@geexcode/geex-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { SharedModule } from "@/shared/shared.module";
import type { AuditLogBrief } from "../graphql/operations.gql";

export interface AuditLogsListParams {
  pi: number;
  ps: number;
}

@Component({
  selector: "app-audit-logs-list",
  standalone: true,
  imports: [SharedModule, ListPageLayoutComponent],
  templateUrl: "./audit-logs-list.page.html",
})
export class AuditLogsListPage extends RoutedListComponent<AuditLogsListParams, AuditLogBrief> {
  private readonly message = this.injector.get(NzMessageService);

  override columns?: Array<STColumn<AuditLogBrief>> = [
    {
      title: "",
      width: 30,
      type: "checkbox",
      index: "checked",
      fixed: "left",
      className: ["text-center"],
    },
    { title: this.I18N.AuditLogs.columnOperation, index: "operationName" },
    { title: this.I18N.AuditLogs.columnType, index: "operationType" },
    { title: this.I18N.AuditLogs.columnSuccess, index: "isSuccess", type: "yn" },
    { title: this.I18N.AuditLogs.columnOperator, index: "operatorId" },
    { title: this.I18N.AuditLogs.columnClientIp, index: "clientIp" },
    { title: this.I18N.AuditLogs.columnCreatedOn, index: "createdOn", type: "date" },
  ];

  override routeParamsMappings: RouteParamsMappings<AuditLogsListParams> = {
    pi: { position: "queryParams", default: 1 },
    ps: { position: "queryParams", default: 10 },
  };

  constructor(injector: Injector) {
    super(injector);
  }

  override async onRouted(params: AuditLogsListParams) {
    this.title.set(this.I18N.AuditLogs.listTitle);
    const result = await geex.auditLogs.loadAuditLogs({
      skip: (params.pi - 1) * params.ps,
      take: params.ps,
    });
    this.selectedData.set([]);
    this.data.set(result.items);
    this.total.set(result.totalCount);
  }

  async batchDelete(): Promise<void> {
    const ids = this.selectedData().map(item => item.id);
    if (!ids.length) {
      return;
    }
    await geex.auditLogs.deleteAuditLogs(ids);
    this.message.success(this.I18N.AuditLogs.deleteSuccess);
    await this.refresh();
  }
}
