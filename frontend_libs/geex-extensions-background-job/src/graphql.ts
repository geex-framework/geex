import gql from "graphql-tag";

export const GQL_JOB_STATE = gql`
  query jobState($skip: Int, $take: Int, $filter: JobStateFilterInput) {
    jobState(skip: $skip, take: $take, filter: $filter) {
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
            jobName
            isSuccess
            startedOn
            finishedOn
            errorMessage
          }
        }
      }
    }
  }
`;
