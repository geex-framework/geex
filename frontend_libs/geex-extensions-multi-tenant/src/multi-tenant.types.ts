import type { WritableSignal } from "@angular/core";
import type { GeexModule } from "@geexcode/geex-angular";

export interface Tenant {
  id: string;
  code: string;
  name: string;
  isEnabled: boolean;
  createdOn: Date;
  [key: string]: any;
}

/** @deprecated Use `Tenant`; alias for host `ITenant` compatibility. */
export type ITenant = Tenant;

export interface MultiTenantModule extends GeexModule<{
  current: WritableSignal<Tenant | null>;
  loadTenantData(code: string): Promise<Tenant>;
  switchTenant(targetTenantCode: string): void;
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    multiTenant: MultiTenantModule;
  }
}
