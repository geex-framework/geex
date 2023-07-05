import { Component, OnInit, ViewEncapsulation } from "@angular/core";
import { ControlUIWidget, SFTransferWidgetSchema, SFSchemaEnum, SFValue, getData, SFSchemaEnumType } from "@delon/form";
import { Apollo } from "apollo-angular";
import type { NzSafeAny } from "ng-zorro-antd/core/types";
import { TransferCanMove, TransferChange, TransferItem, TransferSearchChange, TransferSelectChange } from "ng-zorro-antd/transfer";
import { from, Observable, of } from "rxjs";
import { map } from "rxjs/operators";

import { I18N, I18NService } from "../../core";
import { AppPermission } from "../graphql/.generated/type";

export type PermissionTransferWidgetSchema = SFTransferWidgetSchema;

@Component({
  selector: "sf-permission-transfer",
  template: `<sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
    <nz-transfer
      [nzDataSource]="$any(list)"
      [nzTitles]="ui.titles"
      [nzOperations]="ui.operations"
      [nzListStyle]="ui.listStyle!"
      [nzItemUnit]="ui.itemUnit"
      [nzItemsUnit]="ui.itemsUnit"
      [nzShowSearch]="ui.showSearch"
      [nzFilterOption]="ui.filterOption"
      [nzSearchPlaceholder]="ui.searchPlaceholder"
      [nzNotFoundContent]="ui.notFoundContent"
      [nzCanMove]="_canMove"
      (nzChange)="_change($event)"
      (nzSearchChange)="_searchChange($event)"
      (nzSelectChange)="_selectChange($event)"
    >
    </nz-transfer>
  </sf-item-wrap>`,
  preserveWhitespaces: false,
  encapsulation: ViewEncapsulation.None,
})
export class PermissionTransferWidget extends ControlUIWidget<SFTransferWidgetSchema> implements OnInit {
  /* 用于注册小部件 KEY 值 */
  static readonly KEY = "permission-transfer";
  list: SFSchemaEnum[] = Object.values(AppPermission).map(x => ({ title: x, value: x } as SFSchemaEnum));
  private _data: SFSchemaEnum[] = [];
  I18N = I18N;
  async ngOnInit() {
    "a".charAt;
    const { titles, operations, itemUnit, itemsUnit, showSearch } = this.ui;
    this.ui = {
      titles: titles || ["未拥有", "已拥有"],
      operations: operations || ["", ""],
      itemUnit: itemUnit || "项",
      itemsUnit: itemsUnit || "项",
      showSearch: showSearch || true,
      listStyle: { "min-height": "500px", "min-width": "calc(50% - 20px)" },
      asyncData: () => {
        return of(
          Object.values(AppPermission)
            .reverse()
            .map(x => ({ title: this.I18N.Acl.get(x), value: x } as SFSchemaEnumType)),
        );
      },
    };
  }

  reset(value: SFValue): void {
    getData(this.schema, this.ui, null).subscribe(list => {
      let formData = value;
      if (!Array.isArray(formData)) {
        formData = [formData];
      }
      list.forEach((item: SFSchemaEnum) => {
        if (~(formData as NzSafeAny[]).indexOf(item.value)) {
          item.direction = "right";
        }
      });
      this.list = list;
      this._data = list.filter(w => w.direction === "right");
      this.notify();
      this.detectChanges();
    });
  }

  private notify(): void {
    this.formProperty.setValue(
      this._data.map(i => i.value),
      false,
    );
  }

  _canMove = (arg: TransferCanMove): Observable<TransferItem[]> => {
    return this.ui.canMove ? this.ui.canMove(arg) : of(arg.list);
  };

  _change(options: TransferChange): void {
    if (options.to === "right") {
      this._data = this._data.concat(...options.list);
    } else {
      this._data = this._data.filter((w: SFSchemaEnum) => options.list.indexOf(w as TransferItem) === -1);
    }
    if (this.ui.change) this.ui.change(options);
    this.notify();
  }

  _searchChange(options: TransferSearchChange): void {
    if (this.ui.searchChange) this.ui.searchChange(options);
    this.detectChanges();
  }

  _selectChange(options: TransferSelectChange): void {
    if (this.ui.selectChange) this.ui.selectChange(options);
    this.detectChanges();
  }
}
