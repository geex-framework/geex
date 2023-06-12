import { NgModule, Type } from "@angular/core";
import { SharedModule } from "@shared";

import { MessagingEditComponent } from "./messages/edit/edit.component";
import { MessagingMessagesComponent } from "./messages/messages.component";
import { MessagingViewComponent } from "./messages/view/view.component";
import { MessagingRoutingModule } from "./messaging-routing.module";

const COMPONENTS: Array<Type<void>> = [MessagingMessagesComponent, MessagingViewComponent, MessagingEditComponent];

@NgModule({
  imports: [SharedModule, MessagingRoutingModule],
  declarations: COMPONENTS,
})
export class MessagingModule {}
