import { NgModule, Type } from "@angular/core";
import { SharedModule } from "@shared";
import { NzListModule } from "ng-zorro-antd/list";
import { NzTreeModule } from "ng-zorro-antd/tree";

import { PersonalCenterListComponent } from "./list.component";
import { PersonalCenterRoutingModule } from "./personal-center-routing.module";
const COMPONENTS: Array<Type<void>> = [PersonalCenterListComponent];

@NgModule({
  imports: [SharedModule, PersonalCenterRoutingModule, NzTreeModule, NzListModule],
  declarations: COMPONENTS,
})
export class PersonalCenterModule {}
