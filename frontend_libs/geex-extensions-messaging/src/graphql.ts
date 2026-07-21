import gql from "graphql-tag";

export const GQL_ON_PUBLIC_NOTIFY = gql`
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

export const GQL_ON_PRIVATE_NOTIFY = gql`
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

export const GQL_UNREAD_MESSAGES = gql`
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

export const GQL_MESSAGES = gql`
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

export const GQL_MARK_MESSAGES_READ = gql`
  mutation markMessagesRead($request: MarkMessagesReadRequest!) {
    markMessagesRead(request: $request)
  }
`;

export const GQL_CREATE_MESSAGE = gql`
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

export const GQL_SEND_MESSAGE = gql`
  mutation sendMessage($request: SendNotificationMessageRequest!) {
    sendMessage(request: $request)
  }
`;

export const GQL_DELETE_MESSAGE = gql`
  mutation deleteMessage($request: DeleteMessageRequest!) {
    deleteMessage(request: $request)
  }
`;
