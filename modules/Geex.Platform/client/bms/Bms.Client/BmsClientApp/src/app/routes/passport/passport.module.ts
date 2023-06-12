import { NgModule } from "@angular/core";
import { NgxsModule } from "@ngxs/store";
import { SharedModule } from "@shared";

import { UserDataState } from "../../shared/states/user-data.state";
import { UserLockComponent } from "./lock/lock.component";
import { UserLoginComponent } from "./login/login.component";
import { PassportRoutingModule } from "./passport-routing.module";
import { UserRegisterResultComponent } from "./register-result/register-result.component";
import { UserRegisterComponent } from "./register/register.component";
const COMPONENTS = [UserLoginComponent, UserRegisterResultComponent, UserRegisterComponent, UserLockComponent];

@NgModule({
  imports: [SharedModule, PassportRoutingModule],
  declarations: [...COMPONENTS],
})
export class PassportModule {}
