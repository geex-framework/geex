import { NgModule, Type } from "@angular/core";
import { SharedModule } from "@shared";

import { RoleEditComponent } from "./edit/edit.component";
import { RoleListComponent } from "./list.component";
import { RolesRoutingModule } from "./role-routing.module";

const COMPONENTS: Array<Type<void>> = [RoleListComponent, RoleEditComponent];

@NgModule({
  imports: [SharedModule, RolesRoutingModule],
  declarations: COMPONENTS,
})
export class RoleModule {}
