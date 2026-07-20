import { Component, inject, OnInit, signal } from "@angular/core";
import type { STChange, STColumn } from "@delon/abc/st";
import { geex, GEEX_I18N } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import type { JobExecutionHistoryBrief, JobStateBrief } from "../graphql/operations.gql";

@Component({
  selector: "app-background-job-list",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./job-state-list.page.html",
})
export class BackgroundJobListPage implements OnInit {
  readonly I18N = inject(GEEX_I18N) as any;
  readonly loading = signal(false);
  readonly data = signal<JobStateBrief[]>([]);
  readonly total = signal(0);
  readonly expandedJob = signal<JobStateBrief | null>(null);
  pageIndex = 1;
  pageSize = 10;
  readonly columns: Array<STColumn<JobStateBrief>> = [
    { title: this.I18N.BackgroundJob.columnJobName, index: "jobName" },
    { title: this.I18N.BackgroundJob.columnCron, index: "cron" },
    { title: this.I18N.BackgroundJob.columnLastExecution, index: "lastExecutionTime", type: "date" },
    { title: this.I18N.BackgroundJob.columnNextExecution, index: "nextExecutionTime", type: "date" },
    {
      title: this.I18N.BackgroundJob.historiesTitle,
      buttons: [{ text: this.I18N.Common.action.view, click: item => this.expandedJob.set(item) }],
    },
  ];
  readonly historyColumns: Array<STColumn<JobExecutionHistoryBrief>> = [
    { title: this.I18N.BackgroundJob.columnJobName, index: "jobName" },
    { title: this.I18N.BackgroundJob.columnSuccess, index: "isSuccess", type: "yn" },
    { title: this.I18N.BackgroundJob.columnStartedOn, index: "startedOn", type: "date" },
    { title: this.I18N.BackgroundJob.columnFinishedOn, index: "finishedOn", type: "date" },
    { title: this.I18N.BackgroundJob.columnError, index: "errorMessage" },
  ];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await geex.backgroundJob.loadJobStates({
        skip: (this.pageIndex - 1) * this.pageSize,
        take: this.pageSize,
      });
      this.data.set(result.items);
      this.total.set(result.totalCount);
    } finally {
      this.loading.set(false);
    }
  }

  onTableChange(change: STChange): void {
    if (change.type === "pi" || change.type === "ps") {
      this.pageIndex = change.pi ?? this.pageIndex;
      this.pageSize = change.ps ?? this.pageSize;
      void this.load();
    }
  }

  histories(): JobExecutionHistoryBrief[] {
    return this.expandedJob()?.executionHistories?.items ?? [];
  }
}
