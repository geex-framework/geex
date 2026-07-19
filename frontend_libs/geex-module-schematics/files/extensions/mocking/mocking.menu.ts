import { Injectable, makeEnvironmentProviders } from "@angular/core";
import {
  GEEX_MENU_CONTRIBUTIONS,
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
    return [{
      group: true,
      hideInBreadcrumb: true,
      text: "Mocking",
      children: [{ text: "Mocking", link: "/mocking", icon: "experiment" }],
    }];
  }
}

export function provideMockingNavigation() {
  return makeEnvironmentProviders([
    MockingMenuContribution,
    { provide: GEEX_MENU_CONTRIBUTIONS, multi: true, useExisting: MockingMenuContribution },
  ]);
}
