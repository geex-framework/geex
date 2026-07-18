import { Component, inject, OnInit, ViewEncapsulation } from "@angular/core";
import { ControlUIWidget, getData, SFSchemaEnum, SFSchemaEnumType, SFUISchemaItem, SFValue } from "@delon/form";
import { DelonFormModule } from "@delon/form";
import { SFTransferWidgetSchema } from "@delon/form/widgets/transfer";
import type { NzSafeAny } from "ng-zorro-antd/core/types";
import { TransferCanMove, TransferChange, TransferItem, TransferSearchChange, TransferSelectChange } from "ng-zorro-antd/transfer";
import { NzTransferModule } from "ng-zorro-antd/transfer";
import { Observable, of } from "rxjs";

import { GEEX_APP_PERMISSION, GEEX_I18N } from "@geexcode/geex-angular";
import { GEEX_PERMISSION_FILTER } from "@geexcode/geex-extensions-identity";

export type PermissionTransferWidgetSchema = SFTransferWidgetSchema & SFUISchemaItem;

@Component({
  selector: "sf-permission-transfer",
  template: `<sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
    <nz-transfer
      [nzDataSource]="$any(list)"
      [nzTitles]="$any(ui.titles)"
      [nzOperations]="$any(ui.operations)"
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
  standalone: true,
  imports: [DelonFormModule, NzTransferModule],
  preserveWhitespaces: false,
  encapsulation: ViewEncapsulation.None,
})
export class PermissionTransferWidget extends ControlUIWidget<PermissionTransferWidgetSchema> implements OnInit {
  static readonly KEY = "permission-transfer";

  private readonly appPermission = inject(GEEX_APP_PERMISSION);
  private readonly i18n = inject(GEEX_I18N);
  private readonly permissionFilter = inject(GEEX_PERMISSION_FILTER);

  list: SFSchemaEnum[] = [];
  private _data: SFSchemaEnum[] = [];

  private get permissionValues(): string[] {
    return this.permissionFilter(Object.values(this.appPermission));
  }

  async ngOnInit() {
    const { titles, operations, itemUnit, itemsUnit, showSearch } = this.ui;
    const permissionValues = this.permissionValues;
    this.list = permissionValues.map(x => ({ title: x, value: x }) as SFSchemaEnum);
    this.ui = {
      titles: titles || ["未拥有", "已拥有"],
      operations: operations || ["", ""],
      itemUnit: itemUnit || "项",
      itemsUnit: itemsUnit || "项",
      showSearch: showSearch || true,
      listStyle: { "min-height": "500px", "min-width": "calc(50% - 20px)" },
      asyncData: () => {
        return of(
          [...permissionValues]
            .reverse()
            .map(x => ({ title: this.i18n.permissions.get(x), value: x }) as SFSchemaEnumType),
        );
      },
    };
  }

  override reset(value: SFValue): void {
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
