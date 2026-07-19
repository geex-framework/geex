import type { Menu } from "@delon/theme";

export const menuContribution: Menu[] = [
  {
    children: [
      {
        text: "角色管理",
        icon: "anticon anticon-share-alt",
        link: "/identity/role",
        acl: "identity_query_roles",
      },
      {
        text: "用户管理",
        icon: "anticon anticon-user",
        link: "/identity/user",
        acl: "identity_query_users",
      },
      {
        text: "组织架构",
        icon: "anticon anticon-user",
        link: "/identity/org",
        acl: "identity_query_orgs",
      },
    ],
    text: "用户身份管理",
    icon: "anticon anticon-team",
    acl: ["identity_query_orgs", "identity_query_roles", "identity_query_users"],
  },
];

/** Hidden menu entry for current-user profile (Identity self APIs). */
export const meMenuContribution: Menu[] = [
  {
    text: "个人中心",
    icon: "anticon-user",
    link: "/identity/me",
  },
];
