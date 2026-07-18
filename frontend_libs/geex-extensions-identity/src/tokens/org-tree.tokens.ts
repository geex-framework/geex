import { InjectionToken } from "@angular/core";
import { inject } from "@angular/core";
import { deepCopy } from "@delon/util";
import { GEEX_SUPER_ADMIN_USER_ID } from "@geexcode/geex-angular";
import type { User } from "../types";

export type GeexOrgOwnershipFilterFn = <T extends { code: string }>(orgs: T[], user: User | null) => T[];

export function createDefaultGeexOrgOwnershipFilter(superAdminUserId: string): GeexOrgOwnershipFilterFn {
  return (orgs, user) => {
    if (!user) {
      return [];
    }
    if (user.id === superAdminUserId) {
      return deepCopy(orgs);
    }
    const ownedOrgCodes = user.orgs?.map(x => x.code) ?? [];
    return orgs.where(x => ownedOrgCodes.any(y => y!.startsWith(x!.code)));
  };
}

export const GEEX_ORG_OWNERSHIP_FILTER = new InjectionToken<GeexOrgOwnershipFilterFn>(
  "GEEX_ORG_OWNERSHIP_FILTER",
  {
    providedIn: "root",
    factory: () => createDefaultGeexOrgOwnershipFilter(inject(GEEX_SUPER_ADMIN_USER_ID)),
  },
);
