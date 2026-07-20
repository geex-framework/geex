import type { Menu } from "@delon/theme";

export const menuContribution: Menu[] = [
  {
    text: "消息",
    i18n: "Messaging.title",
    icon: "anticon-notification",
    children: [
      { text: "未读消息", i18n: "Messaging.unreadTitle", link: "/messaging/unread" },
      { text: "消息管理", i18n: "Messaging.adminTitle", link: "/messaging/admin" },
    ],
  },
];
