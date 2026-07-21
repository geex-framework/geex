import gql from "graphql-tag";

export const unreadMessages = gql`
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

export const messages = gql`
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

export const markMessagesRead = gql`
  mutation markMessagesRead($request: MarkMessagesReadRequest!) {
    markMessagesRead(request: $request)
  }
`;

export const createMessage = gql`
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

export const sendMessage = gql`
  mutation sendMessage($request: SendNotificationMessageRequest!) {
    sendMessage(request: $request)
  }
`;

export const deleteMessage = gql`
  mutation deleteMessage($request: DeleteMessageRequest!) {
    deleteMessage(request: $request)
  }
`;

export interface MessagingBrief {
  id: string;
  title?: string | null;
  messageType?: string | null;
  severity?: string | null;
  createdOn?: unknown;
}
