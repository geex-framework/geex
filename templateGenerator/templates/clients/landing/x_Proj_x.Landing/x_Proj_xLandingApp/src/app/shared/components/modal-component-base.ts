import { Injector } from "@angular/core";
import { NzModalRef } from "ng-zorro-antd/modal";

/**
 *
 *基于模态框弹出 Form表单提交的基类信息
 */

export abstract class ModalComponentBase {
  title = "新增";
  nzModalRef: NzModalRef;
  loading = false; // 点击保存后保存按钮的loading状态
  /**
   * 构造函数
   *
   * @param injector 注入器
   * @param _nzModalRef nzModal 模态框关闭、销毁等处理帮助，只能在modal中打开的组件中注入，非modal打开的组件注入null即可，否则报错。因为nzModalRef是建立在nzModalComponent之上的。
   */
  constructor(injector: Injector) {
    this.nzModalRef = injector.get(NzModalRef);
  }
  /**
   * 带参数回传关闭
   *
   * @param result 回传参数
   */
  success(result: any = true): void {
    if (result) {
      this.nzModalRef.close(result);
    } else {
      this.close();
    }
  }

  close($event?: MouseEvent): void {
    this.nzModalRef.close();
  }
}
