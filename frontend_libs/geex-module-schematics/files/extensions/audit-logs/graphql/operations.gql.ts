import gql from "graphql-tag";

export const auditLogs = gql`
  query auditLogs($skip: Int, $take: Int) {
    auditLogs(skip: $skip, take: $take) {
      totalCount
      items {
        id
        operationName
        operationType
        isSuccess
        operatorId
        clientIp
        createdOn
      }
    }
  }
`;

export const deleteAuditLogs = gql`
  mutation deleteAuditLogs($request: DeleteAuditLogsRequest!) {
    deleteAuditLogs(request: $request)
  }
`;

export interface AuditLogBrief {
  id: string;
  operationName?: string | null;
  operationType?: string | null;
  isSuccess?: boolean | null;
  operatorId?: string | null;
  clientIp?: string | null;
  createdOn?: unknown;
}
