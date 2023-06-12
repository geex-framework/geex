import { NgModule, Type } from "@angular/core";
import { SharedModule } from "@shared";

import { UserEditComponent } from "./edit/edit.component";
import { UserListComponent } from "./list.component";
import { UsersRoutingModule } from "./user-routing.module";

const COMPONENTS: Array<Type<void>> = [UserListComponent, UserEditComponent];

@NgModule({
  imports: [SharedModule, UsersRoutingModule],
  declarations: COMPONENTS,
})
export class UserModule {}
