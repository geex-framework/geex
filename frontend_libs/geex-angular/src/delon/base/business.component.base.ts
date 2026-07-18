import { Injectable, WritableSignal, inject } from "@angular/core";
import { Router } from "@angular/router";
import { ACLService, ACLCanType } from "@delon/acl";
import { ModalHelper } from "@delon/theme";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService } from "ng-zorro-antd/modal";

import { GEEX_APP_PERMISSION, GEEX_I18N, GEEX_I18N_SERVICE } from "../tokens";

@Injectable()
export abstract class BusinessComponentBase<TParam = any> {
  protected acl = inject(ACLService);
  protected apollo = inject(Apollo);
  protected i18n = inject(GEEX_I18N_SERVICE, { optional: true }) as any;
  protected modal = inject(ModalHelper);
  protected msgSrv = inject(NzMessageService);
  protected nzModalSrv = inject(NzModalService);
  protected router = inject(Router);
  public params!: WritableSignal<TParam>;
  I18N = inject(GEEX_I18N, { optional: true }) as any;
  AppPermission = inject(GEEX_APP_PERMISSION, { optional: true }) as any;

  can(permission: ACLCanType) {
    return this.acl.can(permission);
  }
}
