import type { GeexModule } from "@geexcode/geex-angular";
import type { DocumentNode } from "graphql";

export interface JobExecutionHistory {
  id?: string | null;
  jobName?: string | null;
  isSuccess?: boolean | null;
  executionStartTime?: unknown;
  executionEndTime?: unknown;
  message?: string | null;
  [key: string]: unknown;
}

export interface JobStateItem {
  id: string;
  jobName?: string | null;
  cron?: string | null;
  lastExecutionTime?: unknown;
  nextExecutionTime?: unknown;
  executionHistories?: {
    items?: JobExecutionHistory[] | null;
    totalCount?: number;
  } | null;
  [key: string]: unknown;
}

export interface BackgroundJobModule extends GeexModule<{
  loadJobStates(options?: {
    skip?: number;
    take?: number;
    jobName?: string | null;
  }): Promise<{
    items: JobStateItem[];
    totalCount: number;
  }>;
  readonly documents: {
    jobState: DocumentNode;
  };
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    backgroundJob: BackgroundJobModule;
  }
}
