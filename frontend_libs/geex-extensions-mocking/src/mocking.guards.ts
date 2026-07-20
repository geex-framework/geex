import { CanMatchFn } from "@angular/router";
import { geex } from "@geexcode/geex-angular";
import { GEEX_DEFAULT_SUPER_ADMIN_USER_ID } from "@geexcode/geex-extensions-identity";

export const mockingEnabledCanMatch: CanMatchFn = () =>
  geex.mocking.getCapabilities().then(capabilities => capabilities.enabled);

export const mockingSuperAdminCanMatch: CanMatchFn = () =>
  geex.mocking.getCapabilities().then(result => {
    if (!result.enabled || !result.management) {
      return false;
    }
    return geex["authentication"].user()?.id === GEEX_DEFAULT_SUPER_ADMIN_USER_ID;
  });
