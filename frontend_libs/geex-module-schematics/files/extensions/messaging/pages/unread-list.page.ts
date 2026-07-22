import { Component, computed, inject, OnInit, signal } from "@angular/core";
import type { STChange, STColumn } from "@delon/abc/st";
import { geex, GEEX_I18N } from "@geexcode/geex-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { SharedModule } from "@/shared/shared.module";
import type { MessagingBrief } from "../graphql/operations.gql";

@Component({
  selector: "app-messaging-unread-list",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./unread-list.page.html",
})
export class MessagingUnreadListPage implements OnInit {
  readonly I18N = inject(GEEX_I18N) as any;
  private readonly message = inject(NzMessageService);
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
