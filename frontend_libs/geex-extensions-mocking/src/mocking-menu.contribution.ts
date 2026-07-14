import { inject, Injectable } from "@angular/core";
import type { GeexMenuContribution, GeexMenuContributionContext, GeexMenuItem } from "@geexcode/geex-angular";
import { GeexMockingCapabilitiesService } from "./mocking-capabilities.service";
import { mockingNavigation } from "./mocking.routes";
import { GEEX_SUPER_ADMIN_ID } from "./types";

@Injectable()
export class MockingMenuContribution implements GeexMenuContribution {
  private readonly capabilities = inject(GeexMockingCapabilitiesService);

  async resolve(user: GeexMenuContributionContext): Promise<GeexMenuItem[]> {
    const caps = await this.capabilities.getCapabilities();
    if (!caps.enabled || !caps.management || user.id !== GEEX_SUPER_ADMIN_ID) {
      return [];
    }
    return [
      {
        group: true,
        hideInBreadcrumb: true,
        text: "Mocking",
        children: [mockingNavigation],
      },
    ];
  }
}
