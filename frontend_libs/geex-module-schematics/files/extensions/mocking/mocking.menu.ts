import type { Menu } from "@delon/theme";

/** Static entry under 系统及配置; hidden until contribution unhides it. */
export const menuContribution: Menu[] = [
  {
    text: "模拟服务",
    i18n: "Mocking.title",
    link: "/mocking",
    icon: "anticon-experiment",
    group: false,
    children: [],
    hide: true,
  },
];
