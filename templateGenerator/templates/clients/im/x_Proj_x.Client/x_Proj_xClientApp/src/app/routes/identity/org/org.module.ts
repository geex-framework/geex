import { NgModule, Type } from "@angular/core";
import { SharedModule } from "@shared";
import { NzAlertModule } from "ng-zorro-antd/alert";
import { NzTreeModule } from "ng-zorro-antd/tree";

import { OrgEditComponent } from "./edit/edit.component";
import { OrgListComponent } from "./list.component";
import { OrgsRoutingModule } from "./org-routing.module";
import { OrgUserListComponent } from "./user-list/list.component";
const COMPONENTS: Array<Type<void>> = [OrgListComponent, OrgUserListComponent, OrgEditComponent];

@NgModule({
  imports: [SharedModule, OrgsRoutingModule, NzTreeModule, NzAlertModule],
  declarations: COMPONENTS,
})
export class OrgModule {}
