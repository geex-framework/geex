import { Component, inject, OnInit } from "@angular/core";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalRef } from "ng-zorro-antd/modal";
import { SharedModule } from "@/shared/shared.module";
import { Geex } from "@geexcode/geex-angular";

@Component({
  selector: "app-tenant-switcher",
  templateUrl: "./tenant-switcher.component.html",
  styles: [],
  standalone: true,
  imports: [SharedModule],
})
export class TenantSwitcherComponent implements OnInit {
    private geex = inject(Geex);
    constructor(
    private apollo: Apollo,
    private modalRef: NzModalRef,
  ) {}

  tenantCode?: string;
  ngOnInit(): void {}
  async submit() {
    // let url = new URL(location.href);
    // url.searchParams.set("__tenant", this.tenantCode ?? "");
    // location.assign(url.toString());
    this.geex.tenant.switchTenant(this.tenantCode ?? "");
    location.reload();
  }
}
