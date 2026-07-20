import type { GeexModule } from "@geexcode/geex-angular";
import type { DocumentNode } from "graphql";

export interface AuditLogItem {
  id: string;
  operationName?: string | null;
  operationType?: string | null;
  isSuccess?: boolean | null;
  operatorId?: string | null;
  clientIp?: string | null;
  createdOn?: unknown;
  [key: string]: unknown;
}

export interface AuditLogsModule extends GeexModule<{
  loadAuditLogs(options?: { skip?: number; take?: number }): Promise<{
    items: AuditLogItem[];
    totalCount: number;
  }>;
  deleteAuditLogs(ids: string[]): Promise<number>;
  readonly documents: {
    auditLogs: DocumentNode;
    deleteAuditLogs: DocumentNode;
  };
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    auditLogs: AuditLogsModule;
  }
}
