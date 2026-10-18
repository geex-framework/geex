import type { Menu } from "@delon/theme";

// ACL: schema has no ApprovalFlows_* permission enum yet.
export const menuContribution: Menu[] = [
  {
    text: "审批流",
    i18n: "ApprovalFlows.title",
    icon: "anticon-audit",
    children: [
      { text: "审批事项", i18n: "ApprovalFlows.menuInbox", link: "/approval-flows/inbox" },
      { text: "流程模板", i18n: "ApprovalFlows.menuTemplates", link: "/approval-flows" },
      { text: "流程分类配置", link: "/approval-flows/tags" },
    ],
  },
];
