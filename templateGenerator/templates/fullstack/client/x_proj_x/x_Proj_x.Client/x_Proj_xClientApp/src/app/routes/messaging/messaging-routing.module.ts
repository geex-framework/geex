import { NgModule } from "@angular/core";
import { RouterModule, GeexRoutes } from "@angular/router";

import { MessagingEditComponent } from "./messages/edit/edit.component";
import { MessagingMessagesComponent } from "./messages/messages.component";
import { MessagingViewComponent } from "./messages/view/view.component";

const routes: GeexRoutes = [
  { path: "", component: MessagingMessagesComponent },
  { path: "view/:id", component: MessagingViewComponent },
  { path: "edit", component: MessagingEditComponent, data: {} },
  { path: "edit/:id", component: MessagingEditComponent, data: {} },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class MessagingRoutingModule {}
