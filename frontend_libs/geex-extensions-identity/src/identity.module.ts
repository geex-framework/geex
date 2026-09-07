import { Injector, signal } from "@angular/core";
import { Apollo, gql } from "apollo-angular";
import { deepCopy } from "@delon/util";
import { guardedSignal } from "@geexcode/geex-angular";
import { firstValueFrom } from "rxjs";
import { GEEX_DEFAULT_SUPER_ADMIN_USER_ID } from "./types";
import type { IdentityAuthenticationDeps, IdentityModule, IdentityModuleDepsFactory, Org, User } from "./types";

const GQL_ORGS_CACHE = gql`query orgsCache { orgsCache { id orgType code name parentOrgCode } }`;
const GQL_USERS_CACHE = gql`
  query usersCache {
    usersCache {
      id
      username
      nickname
      phoneNumber
      email
      isEnable
      roleNames
      roleIds
      avatarFile {
        url
      }
    }
  }
`;

async function warmUsersCache(apollo: Apollo): Promise<void> {
  try {
    await firstValueFrom(apollo.query({ query: GQL_USERS_CACHE }));
  } catch (error) {
    console.error(error);
  }
}

function seedEmptyUsersCache(apollo: Apollo): void {
  apollo.client.writeQuery({
    query: GQL_USERS_CACHE,
    data: { usersCache: [] },
  });
}

function seedEmptyOrgsCache(apollo: Apollo): void {
  apollo.client.writeQuery({
    query: GQL_ORGS_CACHE,
    data: { orgsCache: [] },
  });
}

function readAuthUser(authentication: IdentityAuthenticationDeps): User | null {
  try {
    return authentication.user();
  } catch {
    return null;
  }
}

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
            const apollo = injector.get(Apollo);
            const userData = readAuthUser(authentication);
            if (!userData) {
              seedEmptyOrgsCache(apollo);
              seedEmptyUsersCache(apollo);
              _orgsSignal.set([]);
              _userOwnedOrgsSignal.set([]);
              _initialized = true;
              return;
            }
            type OrgsCacheResponse = { data?: { orgsCache?: Org[] | null } | null };
            const res = (await firstValueFrom(
              apollo.query<OrgsCacheResponse>({ query: GQL_ORGS_CACHE }),
            )) as unknown as OrgsCacheResponse;
            await warmUsersCache(apollo);
            const orgs = deepCopy(res.data?.orgsCache ?? []) as Org[];
            _orgsSignal.set(orgs);
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
