import { inject } from "@angular/core";
import { NzModalRef } from "ng-zorro-antd/modal";

/**
 * Base for components opened inside nz-modal.
 * NzModalRef is required; only use for modal-hosted components.
 */
export abstract class ModalComponentBase {
  title = "新增";
  loading = false;
  protected nzModalRef = inject(NzModalRef);

  success(result: any = true): void {
    if (result) {
      this.nzModalRef.close(result);
      this.afterClose(result);
    } else {
      this.close();
    }
  }

  close(_$event?: MouseEvent): void {
    this.nzModalRef.close();
    this.afterClose(undefined);
  }

  /** Hook after modal closes; override for cleanup / analytics. */
  protected afterClose(_result?: unknown): void {}
}
