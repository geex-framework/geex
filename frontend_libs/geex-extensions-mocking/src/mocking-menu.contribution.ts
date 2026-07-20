import { Injectable } from "@angular/core";
import {
  geex,
  type GeexMenuContribution,
  type GeexMenuContributionContext,
  type GeexMenuItem,
} from "@geexcode/geex-angular";
import { GEEX_DEFAULT_SUPER_ADMIN_USER_ID } from "@geexcode/geex-extensions-identity";

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
