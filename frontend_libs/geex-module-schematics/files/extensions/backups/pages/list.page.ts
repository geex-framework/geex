import { Component, DestroyRef, inject, OnInit, signal, TemplateRef, ViewChild } from "@angular/core";
import { Router, NavigationEnd } from "@angular/router";
import { OAuthService } from "angular-oauth2-oidc";
import { Subscription } from "rxjs";
import { DatePipe } from "@angular/common";
import { STModule, type STChange, type STColumn, type STData } from "@delon/abc/st";
import { PageHeaderModule } from "@delon/abc/page-header";
import { ACLService } from "@delon/acl";
import { geex, GEEX_I18N, GeexSubscriptionConnection } from "@geexcode/geex-angular";
import type { MessagingNotify } from "@geexcode/geex-extensions-messaging";
import type { Backup, BackupSource, BackupStatus } from "@geexcode/geex-extensions-backups";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService, type NzModalRef } from "ng-zorro-antd/modal";
import { FormsModule } from "@angular/forms";
import { NzFormModule } from "ng-zorro-antd/form";
import { NzInputModule } from "ng-zorro-antd/input";
import { NzButtonModule } from "ng-zorro-antd/button";
import { NzCardModule } from "ng-zorro-antd/card";
import { NzSelectModule } from "ng-zorro-antd/select";
import { NzDatePickerModule } from "ng-zorro-antd/date-picker";
import { NzTagModule } from "ng-zorro-antd/tag";
import { NzAlertModule } from "ng-zorro-antd/alert";
import { NzEmptyModule } from "ng-zorro-antd/empty";
import type zhCN from "../i18n/zh-CN";

type BackupQuery = { filter: Record<string, unknown>; pageIndex: number; pageSize: number };
type BackupAction = { kind: "start" | "expire"; id?: string };

@Component({
  selector: "app-backups-list",
  standalone: true,
  imports: [DatePipe, FormsModule, NzFormModule, NzInputModule, NzButtonModule, PageHeaderModule,
    STModule, NzCardModule, NzSelectModule, NzDatePickerModule, NzTagModule, NzAlertModule, NzEmptyModule],
  templateUrl: "./list.page.html",
  styleUrl: "./list.page.less",
})
export class BackupsListPage implements OnInit {
  readonly text = (inject(GEEX_I18N) as { Backups: typeof zhCN }).Backups;
  readonly acl = inject(ACLService);
  private readonly message = inject(NzMessageService);
  private readonly modal = inject(NzModalService);
  private readonly destroy = inject(DestroyRef);
  readonly loading = signal(false);
  readonly pendingAction = signal<BackupAction | null>(null);
  readonly acting = (): boolean => this.pendingAction() !== null;
  readonly enabled = signal(false);
  readonly loaded = signal(false);
  readonly loadError = signal(false);
  readonly hasResult = signal(false);
  readonly data = signal<Backup[]>([]);
  readonly total = signal(0);
  readonly statuses: BackupStatus[] = ["Running", "Succeeded", "Failed", "Cancelled", "Expired"];
  readonly disconnected = signal(false);
  private readonly router = inject(Router);
  private readonly connection = inject(GeexSubscriptionConnection);
  private readonly oauth = inject(OAuthService, { optional: true });
  private privateNotifyWatch?: { unsubscribe(): void };
  private watch?: { unsubscribe(): void };
  private subscriptions = new Subscription();
  private eventTimer?: ReturnType<typeof setTimeout>;
  private refreshQueued = false;
  private subscriptionSetupFailed = false;
  private active = false;
  private generation = 0;
  private confirmation?: NzModalRef;
  @ViewChild("expireDetails", { static: true }) private expireDetails?: TemplateRef<{ $implicit: Backup }>;
  private resetToken: string | null = null;
  readonly trackBackup = (_: number, item: STData): string => item["id"];
  database = "";
  status = "";
  dateRange: Date[] = [];
  pageIndex = 1;
  pageSize = 10;
  private version = 0;
  private disposed = false;
  private readonly pending = new Set<number>();
  private appliedFilter: Record<string, unknown> = {};
  private displayedQuery?: string;
  private readonly unverifiedDownloads = signal<ReadonlySet<string>>(new Set());
  readonly statusColors: Record<BackupStatus, string> = {
    Running: "processing", Succeeded: "success", Failed: "error", Cancelled: "warning", Expired: "default",
  };
  readonly columns: STColumn<Backup>[] = [
    { title: this.text.database, render: "database", width: 160 },
    { title: this.text.source, render: "source", width: 120 },
    { title: this.text.status, render: "status", width: 180 },
    { title: this.text.scheduledAt, render: "scheduled", width: 175 },
    { title: this.text.execution, render: "execution", width: 220 },
    { title: this.text.archive, render: "archive", width: 150 },
    { title: this.text.actions, render: "actions", width: 200 },
  ];

  ngOnInit(): void {
    this.subscriptions.add(this.router.events.subscribe(event => {
      if (!(event instanceof NavigationEnd)) return;
      if (event.urlAfterRedirects.split(/[?#]/)[0] === "/backups") this.enter();
      else this.leave();
    }));
    this.subscriptions.add(this.connection.changes.subscribe(state => {
      this.disconnected.set(state !== "connected" || this.subscriptionSetupFailed);
    }));
    this.subscriptions.add(this.oauth?.events.subscribe(event => {
      if (["logout", "session_terminated", "session_error"].includes(event.type)) this.leave();
      if (event.type === "token_received" && this.router.url.split(/[?#]/)[0] === "/backups") this.enter();
    }));
    this.destroy.onDestroy(() => { this.disposed = true; this.leave(); this.subscriptions.unsubscribe(); });
    this.enter();
  }

  private enter(): void {
    const token = this.router.getCurrentNavigation()?.extras.state?.["backupsReset"] ?? history.state?.backupsReset ?? null;
    const reset = token !== null && token !== this.resetToken;
    this.resetToken = token;
    if (!this.active) {
      this.generation++;
      this.active = true;
      this.subscriptionSetupFailed = false;
      try {
        this.privateNotifyWatch = geex.messaging.watchPrivateNotifications(notify => this.onPrivateNotify(notify));
      } catch (error) {
        this.subscriptionSetupFailed = true;
        this.disconnected.set(true);
        console.error("Backup message listener setup failed", error);
      }
      try {
        this.watch = geex.backups.watch(() => this.queueRefresh(), () => this.disconnected.set(true));
      } catch (error) {
        this.subscriptionSetupFailed = true;
        this.disconnected.set(true);
        console.error("Backup change listener setup failed", error);
      }
    }
    if (reset) this.reset();
    else void this.load();
  }
  private leave(): void {
    this.active = false;
    this.generation++;
    this.version++;
    this.confirmation?.close(); this.confirmation = undefined;
    this.pendingAction.set(null);
    this.pending.clear();
    this.loading.set(false);
    this.watch?.unsubscribe(); this.watch = undefined;
    this.privateNotifyWatch?.unsubscribe(); this.privateNotifyWatch = undefined;
    clearTimeout(this.eventTimer); this.eventTimer = undefined;
    this.refreshQueued = false;
  }
  private onPrivateNotify(notify: MessagingNotify): void {
    if (notify.__typename === "SubscriptionReadyClientNotify") {
      this.queueRefresh();
      return;
    }
    if (notify.__typename !== "NewMessageClientNotify") return;
    const meta = notify.message?.["meta"] as { actions?: { key?: string }[]; status?: string } | null;
    if (Array.isArray(meta?.actions) && meta.actions.some(action => action?.key === "backups.view") &&
        ["Succeeded", "Failed", "Cancelled"].includes(meta.status ?? "")) this.queueRefresh();
  }
  private queueRefresh(): void {
    if (!this.active || this.disposed) return;
    this.refreshQueued = true;
    if (this.pending.size || this.eventTimer) return;
    this.eventTimer = setTimeout(() => {
      this.eventTimer = undefined;
      if (!this.active || this.pending.size) return;
      this.refreshQueued = false;
      void this.load(true);
    }, 100);
  }

  async load(silent = false): Promise<void> {
    if (!this.active || this.disposed) return;
    const version = ++this.version;
    const generation = this.generation;
    const query: BackupQuery = { filter: this.appliedFilter, pageIndex: this.pageIndex, pageSize: this.pageSize };
    let key = JSON.stringify(query);
    let preserve = silent && this.hasResult() && key === this.displayedQuery;
    this.pending.add(version);
    if (key !== this.displayedQuery) this.clearResult();
    if (!silent) this.loading.set(true);
    try {
      while (true) {
        const result = await geex.backups.list({
          skip: (query.pageIndex - 1) * query.pageSize, take: query.pageSize, filter: query.filter,
        });
        if (!this.isCurrent(generation) || version !== this.version) return;
        const total = result.backups?.totalCount ?? 0;
        const lastPage = Math.max(1, Math.ceil(total / query.pageSize));
        if (query.pageIndex > lastPage) {
          query.pageIndex = this.pageIndex = lastPage;
          key = JSON.stringify(query);
          preserve = false;
          this.clearResult();
          if (total > 0) continue;
        }
        const records = (result.backups?.items ?? []).filter((item): item is Backup => item != null);
        this.data.set(records);
        this.total.set(total);
        this.displayedQuery = key;
        this.hasResult.set(true);
        this.enabled.set(result.backupsEnabled);
        this.loaded.set(true);
        this.loadError.set(false);
        const blocked = new Set(this.unverifiedDownloads());
        for (const item of records) {
          if (!this.isCleaning(item)) blocked.delete(item.id);
        }
        this.unverifiedDownloads.set(blocked);
        break;
      }
    } catch {
      if (this.isCurrent(generation) && version === this.version) {
        if (!preserve) this.clearResult();
        this.loadError.set(true);
      }
    } finally {
      this.pending.delete(version);
      if (this.isCurrent(generation) && version === this.version) this.loading.set(false);
      if (!this.pending.size && this.refreshQueued) this.queueRefresh();
    }
  }

  private clearResult(): void {
    this.data.set([]);
    this.total.set(0);
    this.hasResult.set(false);
    this.displayedQuery = undefined;
  }

  private isCurrent(generation: number): boolean {
    return this.active && !this.disposed && generation === this.generation;
  }

  search(): void {
    const [from, to] = this.dateRange;
    if (this.dateRange.length && (this.dateRange.length !== 2 || !from || !to ||
      !Number.isFinite(from.getTime()) || !Number.isFinite(to.getTime()))) {
      this.message.warning(this.text.invalidRange);
      return;
    }
    const start = from ? new Date(from) : undefined;
    const end = to ? new Date(to) : undefined;
    start?.setSeconds(0, 0);
    end?.setSeconds(0, 0);
    if (start && end && start > end) { this.message.warning(this.text.invalidRange); return; }
    this.appliedFilter = {
      ...(this.database.trim() ? { databaseName: { contains: this.database.trim() } } : {}),
      ...(this.status ? { status: { eq: this.status } } : {}),
      ...(start && end ? { scheduledAt: { gte: start.toISOString(), lt: new Date(end.getTime() + 60_000).toISOString() } } : {}),
    };
    this.pageIndex = 1;
    void this.load();
  }
  reset(): void { this.database = this.status = ""; this.dateRange = []; this.search(); }
  emptyTitle(): string { return Object.keys(this.appliedFilter).length ? this.text.noMatches : this.text.empty; }
  statusColor(status: BackupStatus): string { return this.statusColors[status] ?? "default"; }
  statusLabel(status: BackupStatus): string { return this.text.statuses[status] ?? status; }
  sourceLabel(source: BackupSource): string { return this.text.sources[source] ?? this.text.sources.Unknown; }
  formatSize(bytes: number | null | undefined): string {
    if (bytes == null || !Number.isFinite(bytes) || bytes < 0) return "—";
    if (bytes === 0) return "0 B";
    const unit = Math.max(0, Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), 4));
    return `${Number((bytes / 1024 ** unit).toFixed(unit ? 1 : 0))} ${["B", "KB", "MB", "GB", "TB"][unit]}`;
  }
  onTableChange(change: STChange): void {
    if (change.type === "pi" || change.type === "ps") {
      const size = change.ps ?? this.pageSize;
      const index = change.type === "ps" ? 1 : change.pi ?? this.pageIndex;
      if (size === this.pageSize && index === this.pageIndex) return;
      this.pageIndex = index;
      this.pageSize = size;
      void this.load();
    }
  }

  isCleaning(item: Backup): boolean { return this.pendingAction()?.kind === "expire" && this.pendingAction()?.id === item.id; }
  canDownload(item: Backup): boolean {
    return item.status === "Succeeded" && !!item.file?.url && !item.errorMessage &&
      !this.isCleaning(item) && !this.unverifiedDownloads().has(item.id);
  }

  confirmStart(): void {
    if (!this.isCurrent(this.generation) || this.confirmation || this.acting() || !this.acl.can("Backups_mutation_startBackup")) return;
    this.openConfirmation({ kind: "start" });
  }
  confirmExpire(item: Backup): void {
    if (!this.isCurrent(this.generation) || this.confirmation || this.acting() || this.loadError() ||
      item.status !== "Succeeded" || !this.acl.can("Backups_mutation_expireBackup")) return;
    this.openConfirmation({ kind: "expire", id: item.id }, item);
  }

  private openConfirmation(action: BackupAction, item?: Backup): void {
    const generation = this.generation;
    const ref: NzModalRef = this.modal.confirm({
      nzClassName: "backup-confirm", nzTitle: action.kind === "start" ? this.text.startConfirm : this.text.expireConfirm,
      nzContent: action.kind === "start" ? this.text.startScope : this.expireDetails,
      nzData: item, nzOkDanger: action.kind === "expire", nzMaskClosable: false,
      nzOkText: action.kind === "start" ? this.text.startAction : this.text.expire,
      nzOnOk: () => this.perform(action, generation, ref),
    });
    this.confirmation = ref;
    ref.afterClose.subscribe(() => { if (this.confirmation === ref) this.confirmation = undefined; });
  }

  private async perform(action: BackupAction, generation: number, ref: NzModalRef): Promise<boolean> {
    if (!this.isCurrent(generation) || this.confirmation !== ref || this.acting()) return false;
    const permission = action.kind === "start" ? "Backups_mutation_startBackup" : "Backups_mutation_expireBackup";
    const current = this.data().find(item => item.id === action.id);
    if (!this.acl.can(permission) || (action.kind === "expire" && (this.loadError() || current?.status !== "Succeeded"))) {
      this.message.warning(this.text.selectionChanged);
      ref.close();
      return true;
    }
    this.pendingAction.set(action);
    ref.updateConfig({ nzCancelDisabled: true, nzKeyboard: false });
    if (action.id) this.unverifiedDownloads.set(new Set([...this.unverifiedDownloads(), action.id]));
    try {
      const accepted = action.kind === "start" ? (await geex.backups.startTracked()) !== null : await geex.backups.expire(action.id!);
      if (!this.isCurrent(generation)) return true;
      if (accepted) {
        if (action.kind === "start") {
          this.database = this.status = "";
          this.dateRange = [];
          this.appliedFilter = {};
          this.pageIndex = 1;
        }
        this.message.success(action.kind === "start" ? this.text.accepted : this.text.expired);
      } else this.message.warning(this.text.busy);
    } catch (error) {
      if (this.isCurrent(generation)) {
        const errors = (error as { errors?: unknown[] } | null)?.errors;
        this.message.error(Array.isArray(errors) && errors.length ? this.text.failed : this.text.resultUnknown);
      }
    } finally {
      if (this.isCurrent(generation)) {
        this.pendingAction.set(null);
        ref.close();
        void this.load(true);
      }
    }
    return true;
  }
}
