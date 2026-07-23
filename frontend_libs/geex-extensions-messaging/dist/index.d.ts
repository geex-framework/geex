import { WritableSignal, Injector, InjectionToken, EnvironmentProviders } from '@angular/core';
import { GeexModule } from '@geexcode/geex-angular';
import * as graphql from 'graphql';
import { DocumentNode } from 'graphql';

interface MessagingMessage {
    id: string;
    title?: string | null;
    messageType?: string | null;
    severity?: string | null;
    createdOn?: unknown;
    [key: string]: unknown;
}
interface MessagingNotify {
    __typename?: string;
    createdOn?: unknown;
    dataChangeType?: string;
    message?: MessagingMessage | null;
    [key: string]: unknown;
}
interface CreateMessageInput {
    text: string;
    severity?: string | null;
    meta?: unknown;
}
interface SendMessageInput {
    messageId: string;
    toUserIds: string[];
}
interface EditMessageInput {
    id: string;
    text?: string | null;
    severity?: string | null;
}
interface MessagingModule extends GeexModule<{
    unreadMessages: WritableSignal<MessagingMessage[]>;
    onPublicNotify(notify: MessagingNotify): void;
    onPrivateNotify(notify: MessagingNotify): void;
    loadUnreadMessages(): Promise<MessagingMessage[]>;
    loadMessages(options?: {
        skip?: number;
        take?: number;
    }): Promise<{
        items: MessagingMessage[];
        totalCount: number;
    }>;
    markMessagesRead(messageIds: string[], userId: string): Promise<boolean>;
    createMessage(input: CreateMessageInput): Promise<MessagingMessage | null>;
    sendMessage(input: SendMessageInput): Promise<boolean>;
    editMessage(input: EditMessageInput): Promise<boolean>;
    deleteMessage(messageId: string): Promise<boolean>;
    deleteMessageDistributions(messageId: string, userIds: string[]): Promise<boolean>;
    readonly documents: {
        onPublicNotify: DocumentNode;
        onPrivateNotify: DocumentNode;
        unreadMessages: DocumentNode;
        messages: DocumentNode;
        markMessagesRead: DocumentNode;
        createMessage: DocumentNode;
        sendMessage: DocumentNode;
        deleteMessage: DocumentNode;
        editMessage: DocumentNode;
        deleteMessageDistributions: DocumentNode;
    };
}> {
}
declare module "@geexcode/geex-angular" {
    interface GeexModuleMap {
        messaging: MessagingModule;
    }
}

declare function createMessagingModule(injector: Injector, deps?: () => Pick<GeexModule, "init"> | undefined): MessagingModule;

declare const GQL_ON_PUBLIC_NOTIFY: graphql.DocumentNode;
declare const GQL_ON_PRIVATE_NOTIFY: graphql.DocumentNode;
declare const GQL_UNREAD_MESSAGES: graphql.DocumentNode;
declare const GQL_MESSAGES: graphql.DocumentNode;
declare const GQL_MARK_MESSAGES_READ: graphql.DocumentNode;
declare const GQL_CREATE_MESSAGE: graphql.DocumentNode;
declare const GQL_SEND_MESSAGE: graphql.DocumentNode;
declare const GQL_DELETE_MESSAGE: graphql.DocumentNode;
declare const GQL_EDIT_MESSAGE: graphql.DocumentNode;
declare const GQL_DELETE_MESSAGE_DISTRIBUTIONS: graphql.DocumentNode;

interface GeexMessagingOptions {
    readonly createMessagingModule?: (injector: Injector, deps: () => {
        init: (force?: boolean) => Promise<unknown>;
    } | undefined) => MessagingModule;
}
declare const GEEX_MESSAGING_OPTIONS: InjectionToken<Readonly<GeexMessagingOptions>>;
declare function provideGeexMessaging(options?: Readonly<GeexMessagingOptions>): EnvironmentProviders;

export { GEEX_MESSAGING_OPTIONS, GQL_CREATE_MESSAGE, GQL_DELETE_MESSAGE, GQL_DELETE_MESSAGE_DISTRIBUTIONS, GQL_EDIT_MESSAGE, GQL_MARK_MESSAGES_READ, GQL_MESSAGES, GQL_ON_PRIVATE_NOTIFY, GQL_ON_PUBLIC_NOTIFY, GQL_SEND_MESSAGE, GQL_UNREAD_MESSAGES, createMessagingModule, provideGeexMessaging };
export type { CreateMessageInput, EditMessageInput, GeexMessagingOptions, MessagingMessage, MessagingModule, MessagingNotify, SendMessageInput };
//# sourceMappingURL=index.d.ts.map
