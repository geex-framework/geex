import { NgModule, Type } from "@angular/core";
import { STWidgetRegistry } from "@delon/abc/st";
import { WidgetRegistry } from "@delon/form";
import { SharedModule } from "@shared";
import { NzCodeEditorModule } from "ng-zorro-antd/code-editor";

import { SFCodeEditorWidget } from "../../shared/widgets/code-editor.widget";
import { SettingEditComponent } from "./edit/edit.component";
import { SettingListComponent } from "./list.component";
import { SettingsRoutingModule } from "./setting-routing.module";

const COMPONENTS: Array<Type<void>> = [SettingListComponent, SettingEditComponent, SFCodeEditorWidget];

@NgModule({
  imports: [SharedModule, NzCodeEditorModule, SettingsRoutingModule],
  declarations: COMPONENTS,
})
export class SettingModule {
  constructor(widgetRegistry: WidgetRegistry, stWidgetRegistry: STWidgetRegistry) {
    widgetRegistry.register(SFCodeEditorWidget.KEY, SFCodeEditorWidget);
  }
}
