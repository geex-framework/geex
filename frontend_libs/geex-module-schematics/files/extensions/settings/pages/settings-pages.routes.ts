import { inject } from "@angular/core";
import { Routes } from "@angular/router";
import { WidgetRegistry } from "@delon/form";

import { SFCodeEditorWidget } from "@/shared/ui/widgets/code-editor.widget";
import { SettingEditComponent } from "./setting-edit.page";
import { SettingListComponent } from "./setting-list.page";

const initSfCodeEditorWidget = {
  init: () => {
    inject(WidgetRegistry).register(SFCodeEditorWidget.KEY, SFCodeEditorWidget);
  },
};

export const settingsPagesRoutes: Routes = [
  {
    path: "",
    component: SettingListComponent,
  },
  {
    path: "edit",
    component: SettingEditComponent,
    resolve: initSfCodeEditorWidget,
  },
  {
    path: "edit/:name",
    component: SettingEditComponent,
    resolve: initSfCodeEditorWidget,
  },
];
