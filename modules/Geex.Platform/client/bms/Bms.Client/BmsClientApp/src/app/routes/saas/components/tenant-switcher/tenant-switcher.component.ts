import { Component, OnInit } from "@angular/core";
import { CookieService } from "@delon/util";
import { Store } from "@ngxs/store";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalRef } from "ng-zorro-antd/modal";

import { TenantsGql, TenantsQuery, TenantsQueryVariables } from "../../../../shared/graphql/.generated/type";
import { AppActions } from "../../../../shared/states/AppActions";
import { TenantState } from "../../../../shared/states/tenant.state";

@Component({
  selector: "app-tenant-switcher",
  templateUrl: "./tenant-switcher.component.html",
  styles: [],
})
export class TenantSwitcherComponent implements OnInit {
  constructor(private store: Store, private apollo: Apollo, private modalRef: NzModalRef, private notify: NzMessageService) {}

  tenantCode?: string;
  ngOnInit(): void {}
  async submit() {
    let srcTenant = this.store.selectSnapshot(TenantState).code;
    await this.store.dispatch(new AppActions.TenantChanged(this.tenantCode)).toPromise();
    location.assign(location.href.replace(srcTenant, this.tenantCode ?? ""));
  }
}
