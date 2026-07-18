import { Injector, signal } from "@angular/core";
import { Apollo } from "apollo-angular";
import { CookieService } from "@delon/util";
import gql from "graphql-tag";
import { firstValueFrom } from "rxjs";
import { guardedSignal } from "./guarded-signal";
import type { Tenant, TenantModule } from "./types";

const GQL_CHECK_TENANT = gql`mutation checkTenant($code: String!) { checkTenant(code: $code) { id code name isEnabled createdOn } }`;

export function createTenantModule(injector: Injector): TenantModule {
  const _current = signal<Tenant | null>(null);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            const tenantCode = injector.get(CookieService).get("__tenant");
            if (tenantCode) {
              const tenantData = await module.loadTenantData(tenantCode);
              _current.set(tenantData ?? null);
            }
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
    current: guardedSignal(_current, () => _initialized),
    async loadTenantData(code: string): Promise<Tenant> {
      type CheckTenantResponse = { data: { checkTenant: Tenant } };
      const res = (await firstValueFrom(
        injector.get(Apollo).mutate<CheckTenantResponse>({ mutation: GQL_CHECK_TENANT, variables: { code } }),
      )) as unknown as CheckTenantResponse;
      return res.data.checkTenant;
    },
    switchTenant(targetTenantCode: string) {
      const domainParts = location.hostname.split(".");
      if (domainParts.length > 2) {
        domainParts.shift();
      }
      const rootDomain = domainParts.join(".");
      injector.get(CookieService).put("__tenant", targetTenantCode, {
        secure: false,
        SameSite: "lax",
        HttpOnly: false,
        domain: rootDomain,
      });
    },
  };
  return module as TenantModule;
}
