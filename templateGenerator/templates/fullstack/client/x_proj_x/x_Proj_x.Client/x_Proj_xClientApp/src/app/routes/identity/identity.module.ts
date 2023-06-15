import { NgModule, Type } from "@angular/core";
import { SharedModule } from "@shared";

import { IdentityRoutingModule } from "./identity-routing.module";

const COMPONENTS: Array<Type<void>> = [];

@NgModule({
  imports: [SharedModule, IdentityRoutingModule],
  declarations: COMPONENTS,
})
export class IdentityModule {}
