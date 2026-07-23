import { Injector, signal } from "@angular/core";
import { Apollo } from "apollo-angular";
import { OAuthService } from "angular-oauth2-oidc";
import { firstValueFrom } from "rxjs";
import { map } from "rxjs/operators";
import { guardedSignal, type GeexModule } from "@geexcode/geex-angular";
import {
  GQL_CREATE_MESSAGE,
  GQL_DELETE_MESSAGE,
  GQL_DELETE_MESSAGE_DISTRIBUTIONS,
  GQL_EDIT_MESSAGE,
  GQL_MARK_MESSAGES_READ,
  GQL_MESSAGES,
  GQL_ON_PRIVATE_NOTIFY,
  GQL_ON_PUBLIC_NOTIFY,
  GQL_SEND_MESSAGE,
  GQL_UNREAD_MESSAGES,
} from "./graphql";
import type {
  CreateMessageInput,
  EditMessageInput,
  MessagingMessage,
  MessagingModule,
  MessagingNotify,
  SendMessageInput,
} from "./messaging.types";

export function createMessagingModule(
  injector: Injector,
  deps?: () => Pick<GeexModule, "init"> | undefined,
): MessagingModule {
  const unreadSignal = signal<MessagingMessage[]>([]);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;

  const documents = {
    onPublicNotify: GQL_ON_PUBLIC_NOTIFY,
    onPrivateNotify: GQL_ON_PRIVATE_NOTIFY,
    unreadMessages: GQL_UNREAD_MESSAGES,
    messages: GQL_MESSAGES,
    markMessagesRead: GQL_MARK_MESSAGES_READ,
    createMessage: GQL_CREATE_MESSAGE,
    sendMessage: GQL_SEND_MESSAGE,
    deleteMessage: GQL_DELETE_MESSAGE,
    editMessage: GQL_EDIT_MESSAGE,
    deleteMessageDistributions: GQL_DELETE_MESSAGE_DISTRIBUTIONS,
  };

  const module: MessagingModule = {
    documents,
    unreadMessages: guardedSignal(unreadSignal, () => _initialized),
    onPublicNotify(notify: MessagingNotify) {
      console.log("Public notify", notify);
    },
    onPrivateNotify(notify: MessagingNotify) {
      if (notify?.__typename === "NewMessageClientNotify" && notify.message) {
        const incoming = notify.message as MessagingMessage;
        unreadSignal.update(list =>
          list.some(item => item.id === incoming.id) ? list : [incoming, ...list],
        );
      }
    },
    async loadUnreadMessages() {
      const res = await firstValueFrom(
        injector.get(Apollo).query<{
          unreadMessages: { items: MessagingMessage[]; totalCount: number };
        }>({ query: GQL_UNREAD_MESSAGES, variables: { skip: 0, take: 50 }, fetchPolicy: "no-cache" }),
      );
      const items = res.data?.unreadMessages?.items ?? [];
      unreadSignal.set(items);
      return items;
    },
    async loadMessages(options = {}) {
      const res = await firstValueFrom(
        injector.get(Apollo).query<{
          messages: { items: MessagingMessage[]; totalCount: number };
        }>({
          query: GQL_MESSAGES,
          variables: { skip: options.skip ?? 0, take: options.take ?? 20 },
          fetchPolicy: "no-cache",
        }),
      );
      return {
        items: res.data?.messages?.items ?? [],
        totalCount: res.data?.messages?.totalCount ?? 0,
      };
    },
    async markMessagesRead(messageIds: string[], userId: string) {
      const res = await firstValueFrom(
        injector.get(Apollo).mutate<{ markMessagesRead: boolean }>({
          mutation: GQL_MARK_MESSAGES_READ,
          variables: { request: { messageIds, userId } },
        }),
      );
      if (res.data?.markMessagesRead) {
        unreadSignal.update(list => list.filter(item => !messageIds.includes(item.id)));
      }
      return !!res.data?.markMessagesRead;
    },
    async createMessage(input: CreateMessageInput) {
      const res = await firstValueFrom(
        injector.get(Apollo).mutate<{ createMessage: MessagingMessage }>({
          mutation: GQL_CREATE_MESSAGE,
          variables: { request: input },
        }),
      );
      return res.data?.createMessage ?? null;
    },
    async sendMessage(input: SendMessageInput) {
      const res = await firstValueFrom(
        injector.get(Apollo).mutate<{ sendMessage: boolean }>({
          mutation: GQL_SEND_MESSAGE,
          variables: { request: input },
        }),
      );
      return !!res.data?.sendMessage;
    },
    async editMessage(input: EditMessageInput) {
      const res = await firstValueFrom(
        injector.get(Apollo).mutate<{ editMessage: boolean }>({
          mutation: GQL_EDIT_MESSAGE,
          variables: { request: input },
        }),
      );
      return !!res.data?.editMessage;
    },
    async deleteMessage(messageId: string) {
      const res = await firstValueFrom(
        injector.get(Apollo).mutate<{ deleteMessage: boolean }>({
          mutation: GQL_DELETE_MESSAGE,
          variables: { request: { messageId } },
        }),
      );
      return !!res.data?.deleteMessage;
    },
    async deleteMessageDistributions(messageId: string, userIds: string[]) {
      const res = await firstValueFrom(
        injector.get(Apollo).mutate<{ deleteMessageDistributions: boolean }>({
          mutation: GQL_DELETE_MESSAGE_DISTRIBUTIONS,
          variables: { request: { messageId, userIds } },
        }),
      );
      return !!res.data?.deleteMessageDistributions;
    },
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            await deps?.()?.init();
            if (injector.get(OAuthService).hasValidAccessToken()) {
              const subClient = injector.get(Apollo).use("subscription");
              subClient
                .subscribe<{ onPublicNotify: MessagingNotify }>({
                  query: GQL_ON_PUBLIC_NOTIFY,
                  fetchPolicy: "no-cache",
                })
                .pipe(map(res => res?.data?.onPublicNotify))
                .subscribe({
                  next: notify => {
                    if (notify) {
                      module.onPublicNotify(notify);
                    }
                  },
                  error: err => console.error("onPublicNotify subscription error", err),
                });
              subClient
                .subscribe<{ onPrivateNotify: MessagingNotify }>({
                  query: GQL_ON_PRIVATE_NOTIFY,
                  fetchPolicy: "no-cache",
                })
                .pipe(map(res => res?.data?.onPrivateNotify))
                .subscribe({
                  next: notify => {
                    if (notify) {
                      module.onPrivateNotify(notify);
                    }
                  },
                  error: err => console.error("onPrivateNotify subscription error", err),
                });
              await module.loadUnreadMessages();
            }
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
  };

  return module;
}
