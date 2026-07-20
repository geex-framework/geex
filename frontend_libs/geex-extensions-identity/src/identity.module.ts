import { Injector, signal } from "@angular/core";
import { Apollo, gql } from "apollo-angular";
import { deepCopy } from "@delon/util";
import { guardedSignal } from "@geexcode/geex-angular";
import { firstValueFrom } from "rxjs";
import { GEEX_DEFAULT_SUPER_ADMIN_USER_ID } from "./types";
import type { IdentityModule, IdentityModuleDepsFactory, Org } from "./types";

const GQL_ORGS_CACHE = gql`query orgsCache { orgs(take: 999) { items { id orgType code name parentOrgCode } } }`;

export function createIdentityModule(injector: Injector, deps: IdentityModuleDepsFactory): IdentityModule {
  const _orgsSignal = signal<Org[]>([]);
  const _userOwnedOrgsSignal = signal<Org[]>([]);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    orgs: guardedSignal(_orgsSignal, () => _initialized),
    userOwnedOrgs: guardedSignal(_userOwnedOrgsSignal, () => _initialized),
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            const { multiTenant, authentication } = deps();
            await multiTenant.init();
            await authentication.init();
            type OrgsCacheResponse = { data?: { orgs?: { items?: Org[] | null } | null } | null };
            const res = (await firstValueFrom(
              injector.get(Apollo).query<OrgsCacheResponse>({ query: GQL_ORGS_CACHE }),
            )) as unknown as OrgsCacheResponse;
            const orgs = deepCopy(res.data?.orgs?.items ?? []) as Org[];
            _orgsSignal.set(orgs);
            const userData = authentication.user();
            let allOwned: Org[] = [];
            if (orgs?.length && userData) {
              if (userData.id === GEEX_DEFAULT_SUPER_ADMIN_USER_ID) {
                allOwned = deepCopy(orgs);
              } else {
                const ownedCodes = userData.orgs.map(x => x.code);
                allOwned = orgs.filter(o => ownedCodes.some(code => o.code.startsWith(code)));
              }
            }
            _userOwnedOrgsSignal.set(allOwned);
            _initialized = true;
          } catch (error) {
            console.error(error);
          }
        })();
      }
      return _initPromise;
    },
  };
  return module as IdentityModule;
}
