import { Component } from "@angular/core";

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
      if (this.params().id) {
        await this.router.navigate(["../../"], { relativeTo: this.route, replaceUrl: true, forceReload: reload } as any);
      } else {
        await this.router.navigate(["../"], { relativeTo: this.route, replaceUrl: true, forceReload: reload } as any);
      }
    } else {
      this.location.back();
    }
  }
}
