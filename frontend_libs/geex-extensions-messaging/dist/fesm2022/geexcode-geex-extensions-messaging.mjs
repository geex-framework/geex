import { signal, InjectionToken, makeEnvironmentProviders } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { OAuthService } from 'angular-oauth2-oidc';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { guardedSignal, provideGeexModuleContribution } from '@geexcode/geex-angular';
import gql from 'graphql-tag';

const GQL_ON_PUBLIC_NOTIFY = gql `
  subscription onPublicNotify {
    onPublicNotify {
      createdOn
      __typename
      ... on DataChangeClientNotify {
        dataChangeType
      }
      ... on NewMessageClientNotify {
        message {
          id
          title
          messageType
          severity
          createdOn
        }
      }
    }
  }
`;
const GQL_ON_PRIVATE_NOTIFY = gql `
  subscription onPrivateNotify {
    onPrivateNotify {
      createdOn
      __typename
      ... on DataChangeClientNotify {
        dataChangeType
      }
      ... on NewMessageClientNotify {
        message {
          id
          title
          messageType
          severity
          createdOn
        }
      }
    }
  }
`;
const GQL_UNREAD_MESSAGES = gql `
  query unreadMessages($skip: Int, $take: Int) {
    unreadMessages(skip: $skip, take: $take) {
      totalCount
      items {
        id
        title
        messageType
        severity
        createdOn
      }
    }
  }
`;
const GQL_MESSAGES = gql `
  query messages($skip: Int, $take: Int) {
    messages(skip: $skip, take: $take) {
      totalCount
      items {
        id
        title
        messageType
        severity
        createdOn
      }
    }
  }
`;
const GQL_MARK_MESSAGES_READ = gql `
  mutation markMessagesRead($request: MarkMessagesReadRequest!) {
    markMessagesRead(request: $request)
  }
`;
const GQL_CREATE_MESSAGE = gql `
  mutation createMessage($request: CreateMessageRequest!) {
    createMessage(request: $request) {
      id
      title
      messageType
      severity
      createdOn
    }
  }
`;
const GQL_SEND_MESSAGE = gql `
  mutation sendMessage($request: SendNotificationMessageRequest!) {
    sendMessage(request: $request)
  }
`;
const GQL_DELETE_MESSAGE = gql `
  mutation deleteMessage($request: DeleteMessageRequest!) {
    deleteMessage(request: $request)
  }
`;
const GQL_EDIT_MESSAGE = gql `
  mutation editMessage($request: EditMessageRequest!) {
    editMessage(request: $request)
  }
`;
const GQL_DELETE_MESSAGE_DISTRIBUTIONS = gql `
  mutation deleteMessageDistributions($request: DeleteMessageDistributionsRequest!) {
    deleteMessageDistributions(request: $request)
  }
`;

function createMessagingModule(injector, deps) {
    const unreadSignal = signal([], ...(ngDevMode ? [{ debugName: "unreadSignal" }] : []));
    let _initialized = false;
    let _initPromise = null;
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
    const module = {
        documents,
        unreadMessages: guardedSignal(unreadSignal, () => _initialized),
        onPublicNotify(notify) {
            console.log("Public notify", notify);
        },
        onPrivateNotify(notify) {
            if (notify?.__typename === "NewMessageClientNotify" && notify.message) {
                const incoming = notify.message;
                unreadSignal.update(list => list.some(item => item.id === incoming.id) ? list : [incoming, ...list]);
            }
        },
        async loadUnreadMessages() {
            const res = await firstValueFrom(injector.get(Apollo).query({ query: GQL_UNREAD_MESSAGES, variables: { skip: 0, take: 50 }, fetchPolicy: "no-cache" }));
            const items = res.data?.unreadMessages?.items ?? [];
            unreadSignal.set(items);
            return items;
        },
        async loadMessages(options = {}) {
            const res = await firstValueFrom(injector.get(Apollo).query({
                query: GQL_MESSAGES,
                variables: { skip: options.skip ?? 0, take: options.take ?? 20 },
                fetchPolicy: "no-cache",
            }));
            return {
                items: res.data?.messages?.items ?? [],
                totalCount: res.data?.messages?.totalCount ?? 0,
            };
        },
        async markMessagesRead(messageIds, userId) {
            const res = await firstValueFrom(injector.get(Apollo).mutate({
                mutation: GQL_MARK_MESSAGES_READ,
                variables: { request: { messageIds, userId } },
            }));
            if (res.data?.markMessagesRead) {
                unreadSignal.update(list => list.filter(item => !messageIds.includes(item.id)));
            }
            return !!res.data?.markMessagesRead;
        },
        async createMessage(input) {
            const res = await firstValueFrom(injector.get(Apollo).mutate({
                mutation: GQL_CREATE_MESSAGE,
                variables: { request: input },
            }));
            return res.data?.createMessage ?? null;
        },
        async sendMessage(input) {
            const res = await firstValueFrom(injector.get(Apollo).mutate({
                mutation: GQL_SEND_MESSAGE,
                variables: { request: input },
            }));
            return !!res.data?.sendMessage;
        },
        async editMessage(input) {
            const res = await firstValueFrom(injector.get(Apollo).mutate({
                mutation: GQL_EDIT_MESSAGE,
                variables: { request: input },
            }));
            return !!res.data?.editMessage;
        },
        async deleteMessage(messageId) {
            const res = await firstValueFrom(injector.get(Apollo).mutate({
                mutation: GQL_DELETE_MESSAGE,
                variables: { request: { messageId } },
            }));
            return !!res.data?.deleteMessage;
        },
        async deleteMessageDistributions(messageId, userIds) {
            const res = await firstValueFrom(injector.get(Apollo).mutate({
                mutation: GQL_DELETE_MESSAGE_DISTRIBUTIONS,
                variables: { request: { messageId, userIds } },
            }));
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
                                .subscribe({
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
                                .subscribe({
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
                    }
                    catch (err) {
                        console.error(err);
                    }
                })();
            }
            return _initPromise;
        },
    };
    return module;
}

const GEEX_MESSAGING_OPTIONS = new InjectionToken("GEEX_MESSAGING_OPTIONS");
function provideGeexMessaging(options = {}) {
    return makeEnvironmentProviders([
        { provide: GEEX_MESSAGING_OPTIONS, useValue: options },
        provideGeexModuleContribution({
            createModules: ({ injector, modules }) => {
                const authentication = modules["authentication"];
                if (!authentication) {
                    throw new Error("provideGeexMessaging() requires provideGeexAuthentication() to be registered first");
                }
                const messaging = (options.createMessagingModule ?? createMessagingModule)(injector, () => ({
                    init: authentication.init.bind(authentication),
                }));
                return { messaging };
            },
        }),
    ]);
}

/**
 * Generated bundle index. Do not edit.
 */

export { GEEX_MESSAGING_OPTIONS, GQL_CREATE_MESSAGE, GQL_DELETE_MESSAGE, GQL_DELETE_MESSAGE_DISTRIBUTIONS, GQL_EDIT_MESSAGE, GQL_MARK_MESSAGES_READ, GQL_MESSAGES, GQL_ON_PRIVATE_NOTIFY, GQL_ON_PUBLIC_NOTIFY, GQL_SEND_MESSAGE, GQL_UNREAD_MESSAGES, createMessagingModule, provideGeexMessaging };
//# sourceMappingURL=geexcode-geex-extensions-messaging.mjs.map
