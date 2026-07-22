import gql from "graphql-tag";

export const jobState = gql`
  query jobState($skip: Int, $take: Int) {
    jobState(skip: $skip, take: $take) {
      totalCount
      items {
        id
        jobName
        cron
        lastExecutionTime
        nextExecutionTime
        executionHistories {
          totalCount
          items {
            id
            jobName
            isSuccess
            executionStartTime
            executionEndTime
            message
          }
        }
      }
    }
  }
`;

export interface JobExecutionHistoryBrief {
  id?: string | null;
  jobName?: string | null;
  isSuccess?: boolean | null;
  executionStartTime?: unknown;
  executionEndTime?: unknown;
  message?: string | null;
}

export interface JobStateBrief {
  id: string;
  jobName?: string | null;
  cron?: string | null;
  lastExecutionTime?: unknown;
  nextExecutionTime?: unknown;
  executionHistories?: {
    items?: JobExecutionHistoryBrief[] | null;
    totalCount?: number;
  } | null;
}
