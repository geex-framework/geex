import type { WritableSignal } from "@angular/core";
import type { GeexModule } from "@geexcode/geex-angular";
import type { DocumentNode } from "graphql";

export interface MessagingMessage {
  id: string;
  title?: string | null;
  messageType?: string | null;
  severity?: string | null;
  createdOn?: unknown;
  [key: string]: unknown;
}

export interface MessagingNotify {
  __typename?: string;
  createdOn?: unknown;
  dataChangeType?: string;
  message?: MessagingMessage | null;
  [key: string]: unknown;
}

export interface CreateMessageInput {
  text: string;
  severity?: string | null;
  meta?: unknown;
}

export interface SendMessageInput {
  messageId: string;
  toUserIds: string[];
}

export interface MessagingModule extends GeexModule<{
  unreadMessages: WritableSignal<MessagingMessage[]>;
  onPublicNotify(notify: MessagingNotify): void;
  onPrivateNotify(notify: MessagingNotify): void;
  loadUnreadMessages(): Promise<MessagingMessage[]>;
  loadMessages(options?: { skip?: number; take?: number }): Promise<{
    items: MessagingMessage[];
    totalCount: number;
  }>;
  markMessagesRead(messageIds: string[], userId: string): Promise<boolean>;
  createMessage(input: CreateMessageInput): Promise<MessagingMessage | null>;
  sendMessage(input: SendMessageInput): Promise<boolean>;
  deleteMessage(messageId: string): Promise<boolean>;
  readonly documents: {
    onPublicNotify: DocumentNode;
    onPrivateNotify: DocumentNode;
    unreadMessages: DocumentNode;
    messages: DocumentNode;
    markMessagesRead: DocumentNode;
    createMessage: DocumentNode;
    sendMessage: DocumentNode;
    deleteMessage: DocumentNode;
  };
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    messaging: MessagingModule;
  }
}
