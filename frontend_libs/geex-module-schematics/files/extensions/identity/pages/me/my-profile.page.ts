import { Component, Signal, computed, inject, signal } from "@angular/core";
import { FormGroup } from "@angular/forms";
import { SettingsService } from "@delon/theme";
import { ArrayService } from "@delon/util";
import type { STChange, STColumn } from "@delon/abc/st";
import { NzFormatEmitEvent, NzTreeNode } from "ng-zorro-antd/tree";
import { NzMessageService } from "ng-zorro-antd/message";

import { RoutedComponent, geex } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import type { ChangePasswordRequest } from "@/gql";
import { changePassword } from "../../graphql/user.operations.gql";
import type { User, Org } from "@geexcode/geex-extensions-identity";
import { generatePersonalAccessToken } from "../../graphql/me.operations.gql";

type UnreadMessageBrief = {
  id: string;
  title?: string | null;
  messageType?: string | null;
  severity?: string | null;
  createdOn?: unknown;
};

@Component({
  selector: "app-my-profile",
  templateUrl: "./my-profile.page.html",
  standalone: true,
  imports: [SharedModule],
})
export class MyProfilePage extends RoutedComponent<{}> {
  override routeParamsMappings: {} = {};
  override onRouted(params: {}): void | Promise<void> {
    return;
  }
  userData$: Signal<User>;
  orgs$: Signal<Org[]>;
  validateForm!: FormGroup;
  id: string;
  pageNo = 0;
  pageSize = 10;
  isVisible = false;
  confirmPassword: string;
  data: ChangePasswordRequest = {
    originPassword: undefined,
    newPassword: undefined,
  };
  nodes$: Signal<NzTreeNode[]>;
  activatedNode$ = signal<NzTreeNode>(undefined);

  isTokenModalVisible$ = signal<boolean>(false);
  generatedToken$ = signal<string>("");
  tokenExpireDays$ = signal<number>(30);
  readonly unreadLoading = signal(false);
  readonly unreadSelectedIds = signal<string[]>([]);
  readonly messagingEnabled = computed(() => this.messaging != null);
  readonly unreadMessages = computed(() => {
    const messaging = this.messaging;
    if (!messaging) {
      return [] as UnreadMessageBrief[];
    }
    try {
      return (messaging.unreadMessages() ?? []) as UnreadMessageBrief[];
    } catch {
      return [] as UnreadMessageBrief[];
    }
  });
  readonly unreadColumns: Array<STColumn<UnreadMessageBrief>>;

  get user(): User {
    return this.settings.user;
  }
  private settings = inject(SettingsService);
  private arrService = inject(ArrayService);
  private readonly notifyMessage = inject(NzMessageService);

  private get messaging():
    | {
        unreadMessages: () => UnreadMessageBrief[];
        loadUnreadMessages: () => Promise<UnreadMessageBrief[]>;
        markMessagesRead: (messageIds: string[], userId: string) => Promise<boolean>;
      }
    | undefined {
    return (geex as Record<string, unknown>)["messaging"] as
      | {
          unreadMessages: () => UnreadMessageBrief[];
          loadUnreadMessages: () => Promise<UnreadMessageBrief[]>;
          markMessagesRead: (messageIds: string[], userId: string) => Promise<boolean>;
        }
      | undefined;
  }

  constructor() {
    super();
    this.unreadColumns = [
      {
        title: "",
        width: 30,
        type: "checkbox",
        index: "checked",
        fixed: "left",
        className: ["text-center"],
      },
      { title: this.I18N.Messaging?.columnText ?? this.I18N.Identity.profile.unreadMessagesTitle, index: "title" },
      { title: this.I18N.Messaging?.columnType ?? "Type", index: "messageType" },
      { title: this.I18N.Messaging?.columnSeverity ?? "Severity", index: "severity" },
      { title: this.I18N.Messaging?.columnCreatedOn ?? "Created", index: "createdOn", type: "date" },
    ];
    this.orgs$ = geex.identity.orgs;
    this.userData$ = geex.authentication.user;
    this.nodes$ = computed(() => {
      let userData = this.userData$();
      if (userData == undefined) {
        return [];
      }
      let orgs = userData.orgs.concat(userData.orgs.selectMany(x => x.allParentOrgs as any)).distinctBy(x => x.code);
      let data = orgs.map(x => ({
        code: x.code,
        name: x.name,
        expanded: x.code.lastIndexOf(".") == -1,
        pCode: x.code.substring(0, x.code.lastIndexOf(".")),
      }));

      return this.arrService.arrToTreeNode(data, {
        parentIdMapName: "pCode",
        idMapName: "code",
        titleMapName: "name",
      });
    });

    this.route.fragment.subscribe(fragment => {
      if (fragment === "unread-messages") {
        queueMicrotask(() =>
          document.getElementById("unread-messages")?.scrollIntoView({ behavior: "smooth", block: "start" }),
        );
      }
    });

    if (this.messagingEnabled()) {
      void this.loadUnreadMessages();
    }
  }

  activeNode(data: NzFormatEmitEvent): void {
    if (data.node.isSelected) {
      this.pageNo = 0;
      this.pageSize = 10;
      this.activatedNode$.set(data.node!);
    } else {
      this.activatedNode$.set(undefined);
    }
  }

  showModal() {
    this.isVisible = true;
  }

  async handleOk() {
    await this.apollo
      .mutate({
        mutation: changePassword,
        variables: {
          request: {
            originPassword: this.data.originPassword,
            newPassword: this.data.newPassword,
          },
        },
      })
      .firstValuePromise();
    this.msgSrv.success(this.I18N.Identity.profile.updateSuccess);
    this.confirmPassword = undefined;
    this.data.originPassword = undefined;
    this.data.newPassword = undefined;

    this.isVisible = false;
  }

  handleCancel(): void {
    this.confirmPassword = undefined;
    this.data.originPassword = undefined;
    this.data.newPassword = undefined;
    this.isVisible = false;
  }

  showTokenModal() {
    this.isTokenModalVisible$.set(true);
    this.generatedToken$.set("");
  }

  async handleGenerateToken() {
    try {
      const result = await this.apollo
        .mutate({
          mutation: generatePersonalAccessToken,
          variables: {
            req: {
              expireInSeconds: this.tokenExpireDays$() * 24 * 60 * 60,
            },
          },
        })
        .firstValuePromise();

      this.generatedToken$.set(result?.data?.generatePersonalAccessToken?.token || "");
      this.msgSrv.success(this.I18N.Identity.profile.tokenGenerateSuccess);
    } catch (error) {
      this.msgSrv.error(this.I18N.Identity.profile.tokenGenerateFailed);
      console.error(error);
    }
  }

  handleTokenModalCancel(): void {
    this.isTokenModalVisible$.set(false);
    this.generatedToken$.set("");
    this.tokenExpireDays$.set(30);
  }

  copyToken() {
    const token = this.generatedToken$();
    if (token) {
      navigator.clipboard.writeText(token);
      this.msgSrv.success(this.I18N.Identity.profile.tokenCopied);
    }
  }

  async loadUnreadMessages(): Promise<void> {
    const messaging = this.messaging;
    if (!messaging) {
      return;
    }
    this.unreadLoading.set(true);
    try {
      await messaging.loadUnreadMessages();
    } finally {
      this.unreadLoading.set(false);
    }
  }

  onUnreadTableChange(change: STChange): void {
    if (change.type === "checkbox") {
      this.unreadSelectedIds.set((change.checkbox ?? []).map(item => item.id));
    }
  }

  async markUnreadRead(): Promise<void> {
    const messaging = this.messaging;
    const ids = this.unreadSelectedIds();
    const userId = geex.authentication.user()?.id;
    if (!messaging || !ids.length || !userId) {
      return;
    }
    await messaging.markMessagesRead(ids, userId);
    this.notifyMessage.success(this.I18N.Messaging?.markReadSuccess ?? "已标记为已读");
    this.unreadSelectedIds.set([]);
  }
}
