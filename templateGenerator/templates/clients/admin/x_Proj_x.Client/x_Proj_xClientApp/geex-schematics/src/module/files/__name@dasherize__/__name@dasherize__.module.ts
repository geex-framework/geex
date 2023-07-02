import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SharedModule } from "@shared";

import { <%= classify(name) %>RoutingModule } from './<%= dasherize(name) %>-routing.module';

import { <%= classify(name) %>ListPage } from './list/list.page';
import { <%= classify(name) %>EditPage } from './edit/edit.page';

@NgModule({
  imports: [
    SharedModule,
    CommonModule,
    FormsModule,
    <%= classify(name) %>RoutingModule
  ],
  declarations: [<%= classify(name) %>ListPage,<%= classify(name) %>EditPage]
})
export class <%= classify(name) %>Module {}
