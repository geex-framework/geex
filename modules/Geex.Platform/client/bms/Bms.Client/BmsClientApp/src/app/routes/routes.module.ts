import { NgModule, Type } from "@angular/core";
import { SharedModule } from "@shared";

import { RouteRoutingModule } from "./routes-routing.module";

@NgModule({
  imports: [SharedModule, RouteRoutingModule],
})
export class RoutesModule {}
