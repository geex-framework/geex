import { Injectable, makeEnvironmentProviders } from "@angular/core";
import type { Menu } from "@delon/theme";
import {
  GEEX_MENU_CONTRIBUTIONS,
  geex,
  type GeexMenuContribution,
  type GeexMenuContributionContext,
  type GeexMenuItem,
} from "@geexcode/geex-angular";
import { GEEX_DEFAULT_SUPER_ADMIN_USER_ID } from "@geexcode/geex-extensions-identity";

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

@Injectable()
export class MockingMenuContribution implements GeexMenuContribution {
  async resolve(user: GeexMenuContributionContext): Promise<GeexMenuItem[]> {
    const capabilities = await geex.mocking.getCapabilities();
    if (!capabilities.enabled || !capabilities.management || user.id !== GEEX_DEFAULT_SUPER_ADMIN_USER_ID) {
      return [];
    }
    return [
      {
        text: "模拟服务",
        i18n: "Mocking.title",
        link: "/mocking",
        icon: "anticon-experiment",
        group: false,
        children: [],
        hide: false,
      },
    ];
  }
}

export function provideMockingNavigation() {
  return makeEnvironmentProviders([
    MockingMenuContribution,
    { provide: GEEX_MENU_CONTRIBUTIONS, multi: true, useExisting: MockingMenuContribution },
  ]);
}
