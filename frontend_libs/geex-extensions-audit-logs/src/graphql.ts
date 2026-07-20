import gql from "graphql-tag";

export const GQL_AUDIT_LOGS = gql`
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

export const GQL_DELETE_AUDIT_LOGS = gql`
  mutation deleteAuditLogs($request: DeleteAuditLogsRequest!) {
    deleteAuditLogs(request: $request)
  }
`;
