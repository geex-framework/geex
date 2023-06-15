import { NgModule, Type } from "@angular/core";
import { SharedModule } from "@shared";

import { TenantEditComponent } from "./components/tenant-edit/tenant-edit.component";
import { TenantSwitcherComponent } from "./components/tenant-switcher/tenant-switcher.component";
import { TenantListComponent } from "./list/list.component";
import { SaasRoutingModule } from "./saas-routing.module";

const COMPONENTS: Array<Type<void>> = [TenantSwitcherComponent, TenantListComponent, TenantEditComponent];

@NgModule({
  imports: [SharedModule, SaasRoutingModule],
  declarations: COMPONENTS,
})
export class SaasModule {}
