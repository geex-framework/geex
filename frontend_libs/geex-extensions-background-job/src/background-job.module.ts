import { Injector } from "@angular/core";
import { Apollo } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import { GQL_JOB_STATE } from "./graphql";
import type { BackgroundJobModule, JobStateItem } from "./background-job.types";

export function createBackgroundJobModule(injector: Injector): BackgroundJobModule {
  const apollo = () => injector.get(Apollo);

  return {
    documents: {
      jobState: GQL_JOB_STATE,
    },
    async loadJobStates(options = {}) {
      const filter = options.jobName ? { jobName: { eq: options.jobName } } : undefined;
      const res = await firstValueFrom(
        apollo().query<{
          jobState: { items: JobStateItem[]; totalCount: number };
        }>({
          query: GQL_JOB_STATE,
          variables: {
            skip: options.skip ?? 0,
            take: options.take ?? 20,
            filter,
          },
          fetchPolicy: "no-cache",
        }),
      );
      return {
        items: res.data?.jobState?.items ?? [],
        totalCount: res.data?.jobState?.totalCount ?? 0,
      };
    },
    init: async () => undefined,
  };
}
