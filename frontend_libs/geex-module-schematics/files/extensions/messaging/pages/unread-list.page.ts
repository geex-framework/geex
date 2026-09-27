import { Router, NavigationEnd } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { toSignal } from "@angular/core/rxjs-interop";
import { GeexSubscriptionConnection } from "@geexcode/geex-angular";
import { Component, computed, inject, OnInit, signal } from "@angular/core";
import { GeexMessageActions } from "@geexcode/geex-extensions-messaging";
import type { STChange, STColumn } from "@delon/abc/st";
import { geex, GEEX_I18N } from "@geexcode/geex-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { SharedModule } from "@/shared/shared.module";
import { type MessagingBrief } from "../graphql/operations.gql";

@Component({
  selector: "app-messaging-unread-list",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./unread-list.page.html",
})
export class MessagingUnreadListPage implements OnInit {
  readonly I18N = inject(GEEX_I18N);
  private readonly message = inject(NzMessageService);
  private readonly actions = inject(GeexMessageActions);
  readonly subscriptionState = toSignal(inject(GeexSubscriptionConnection).changes);
  private readonly navigationRefresh = inject(Router).events.pipe(takeUntilDestroyed()).subscribe(event => {
    if (event instanceof NavigationEnd && event.urlAfterRedirects.split(/[?#]/)[0] === "/messaging/unread") void this.load();
  });
  readonly loading = signal(false);
  readonly selectedIds = signal<string[]>([]);
  readonly data = computed(() => {
    try {
      return (geex.messaging.unreadMessages() ?? []) as MessagingBrief[];
    } catch {
      return [] as MessagingBrief[];
    }
  });
  readonly total = computed(() => this.data().length);
  readonly columns: Array<STColumn<MessagingBrief>> = [
    {
      title: "",
      width: 30,
      type: "checkbox",
      index: "checked",
      fixed: "left",
      className: ["text-center"],
    },
    { title: this.I18N.Messaging.columnText, index: "title" },
    { title: this.I18N.Messaging.columnType, index: "messageType" },
    { title: this.I18N.Messaging.columnSeverity, index: "severity" },
    { title: this.I18N.Messaging.columnCreatedOn, index: "createdOn", type: "date" },
    {
      title: this.I18N.Messaging.columnActions,
      buttons: this.actions.registered.map(action => ({
        text: this.actions.label(action),
        iif: item => this.actions.available(action, item),
        click: item => this.executeAction(action.key, item),
      })),
    },
  ];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      await geex.messaging.loadUnreadMessages();
    } finally {
      this.loading.set(false);
    }
  }

  onTableChange(change: STChange): void {
    if (change.type === "checkbox") {
      this.selectedIds.set((change.checkbox ?? []).map(item => item.id));
    }
  }

  async executeAction(key: string, item: MessagingBrief): Promise<void> {
    try {
      if (await this.actions.execute(key, item, geex.authentication.user()?.id)) return;
    } catch { }
    this.message.error(this.I18N.Messaging.actionFailed);
  }

  async markRead(): Promise<void> {
    const ids = this.selectedIds();
    if (!ids.length) {
      return;
    }
    const userId = geex.authentication.user()?.id;
    if (!userId) {
      return;
    }
    await geex.messaging.markMessagesRead(ids, userId);
    this.message.success(this.I18N.Messaging.markReadSuccess);
    this.selectedIds.set([]);
  }
}
