import { Injector } from "@angular/core";
import { Apollo } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import { GQL_AUDIT_LOGS, GQL_DELETE_AUDIT_LOGS } from "./graphql";
import type { AuditLogItem, AuditLogsModule } from "./audit-logs.types";

export function createAuditLogsModule(injector: Injector): AuditLogsModule {
  const apollo = () => injector.get(Apollo);

  return {
    documents: {
      auditLogs: GQL_AUDIT_LOGS,
      deleteAuditLogs: GQL_DELETE_AUDIT_LOGS,
    },
    async loadAuditLogs(options = {}) {
      const res = await firstValueFrom(
        apollo().query<{
          auditLogs: { items: AuditLogItem[]; totalCount: number };
        }>({
          query: GQL_AUDIT_LOGS,
          variables: { skip: options.skip ?? 0, take: options.take ?? 20 },
          fetchPolicy: "no-cache",
        }),
      );
      return {
        items: res.data?.auditLogs?.items ?? [],
        totalCount: res.data?.auditLogs?.totalCount ?? 0,
      };
    },
    async deleteAuditLogs(ids: string[]) {
      const res = await firstValueFrom(
        apollo().mutate<{ deleteAuditLogs: number }>({
          mutation: GQL_DELETE_AUDIT_LOGS,
          variables: { request: { ids } },
        }),
      );
      return res.data?.deleteAuditLogs ?? 0;
    },
    init: async () => undefined,
  };
}
