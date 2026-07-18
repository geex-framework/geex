import { inject, Injectable, makeEnvironmentProviders } from "@angular/core";
import {
  GEEX_MENU_CONTRIBUTIONS,
  type GeexMenuContribution,
  type GeexMenuContributionContext,
  type GeexMenuItem,
} from "@geexcode/geex-angular";
import {
  GEEX_SUPER_ADMIN_ID,
  GeexMockingCapabilitiesService,
} from "@geexcode/geex-extensions-mocking";

@Injectable()
export class MockingMenuContribution implements GeexMenuContribution {
  private readonly capabilities = inject(GeexMockingCapabilitiesService);

  async resolve(user: GeexMenuContributionContext): Promise<GeexMenuItem[]> {
    const capabilities = await this.capabilities.getCapabilities();
    if (!capabilities.enabled || !capabilities.management || user.id !== GEEX_SUPER_ADMIN_ID) {
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
