import { Component, inject, OnInit } from "@angular/core";
import { Apollo } from "apollo-angular";
import { NzModalRef } from "ng-zorro-antd/modal";
import { SharedModule } from "@/shared/shared.module";
import { Geex, GEEX_I18N } from "@geexcode/geex-angular";

@Component({
  selector: "app-tenant-switcher",
  templateUrl: "./tenant-switcher.component.html",
  styles: [],
  standalone: true,
  imports: [SharedModule],
})
export class TenantSwitcherComponent implements OnInit {
    readonly I18N = inject(GEEX_I18N) as any;
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
    this.geex.multiTenant.switchTenant(this.tenantCode ?? "");
    location.reload();
  }
}
