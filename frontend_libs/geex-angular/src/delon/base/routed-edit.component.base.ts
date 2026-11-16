import { Component, inject } from "@angular/core";
import { GeexTabService } from "../router/geex-tab.service";

import { GeexHint, GeexTypedFormGroup } from "../types";
import { RoutedComponent } from "./routed.component.base";

function geexIsEqual(a: unknown, b: unknown): boolean {
  if (Object.is(a, b)) {
    return true;
  }
  try {
    return JSON.stringify(a) === JSON.stringify(b);
  } catch {
    return false;
  }
}

@Component({
  template: "",
  standalone: true,
})
export abstract class RoutedEditComponent<
  TParams extends GeexHint<{ id?: string }>,
  TEntity extends GeexHint<{ id?: string }>,
  TEditSchema extends GeexHint<Partial<TEntity>>,
> extends RoutedComponent<TParams> {
  private readonly tabs = inject(GeexTabService);
  entity?: GeexHint<TEntity>;
  entityForm?: GeexTypedFormGroup<TEditSchema>;
  originalValue?: TEditSchema;

  async close() {
    if (await this.closableCheck()) {
      await this.back();
    }
  }

  closableCheck() {
    if (!this.isEntityDirty()) {
      return Promise.resolve(true);
    }
    return new Promise<boolean>((resolve) => {
      this.nzModalSrv.confirm({
        nzTitle: this.unsavedConfirmTitle(),
        nzOnOk: async () => {
          this.entityForm?.reset(this.originalValue);
          this.entityForm?.markAsPristine();
          resolve(true);
        },
        nzOnCancel: () => {
          resolve(false);
        },
      });
    });
  }

  protected isEntityDirty(): boolean {
    return !geexIsEqual(this.entityForm?.value, this.originalValue);
  }

  protected unsavedConfirmTitle(): string {
    return "当前页面内容未保存，确定离开？";
  }

  async back(reload: boolean = false) {
    if (reload) {
      const fallback = this.router.createUrlTree([this.params().id ? "../../" : "../"], { relativeTo: this.route });
      await this.tabs.closeCurrent({ fallbackUrl: this.router.serializeUrl(fallback), refreshOrigin: true });
    } else {
      this.location.back();
    }
  }
}
