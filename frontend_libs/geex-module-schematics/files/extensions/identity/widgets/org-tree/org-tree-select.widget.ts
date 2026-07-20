

import { Component, Injector, OnInit, Signal, ViewChild, ViewEncapsulation } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ControlUIWidget, getData, SFSchemaEnum, SFUISchemaItem, SFValue, toBool } from "@delon/form";
import { DelonFormModule } from "@delon/form";
import { SFTreeSelectWidgetSchema } from "@delon/form/widgets/tree-select";
import { ArrayService, deepCopy } from "@delon/util";
import { NzFormatEmitEvent, NzTreeNode } from "ng-zorro-antd/core/tree";
import { NzTreeSelectComponent } from "ng-zorro-antd/tree-select";
import { NzTreeSelectModule } from "ng-zorro-antd/tree-select";

import { geex } from "@geexcode/geex-angular";
import { OrgTypeEnum, type Org, type User } from "@geexcode/geex-extensions-identity";
import { GEEX_ORG_OWNERSHIP_FILTER } from "@geexcode/geex-extensions-identity";
import { GEEX_SUPER_ADMIN_USER_ID } from "@geexcode/geex-angular";

export type OrgTreeSelectWidgetSchema = SFTreeSelectWidgetSchema &
  SFUISchemaItem & {
    filter?: (x: Org) => boolean;
    orgType?: OrgTypeEnum[];
  };

@Component({
  selector: "sf-org-tree-select",
  template: ` <sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
    <nz-tree-select
      [nzId]="id"
      [nzAllowClear]="ui.allowClear"
      [nzPlaceHolder]="ui.placeholder!"
      [nzDropdownStyle]="ui.dropdownStyle!"
      [nzDropdownClassName]="ui.dropdownClassName"
      [nzSize]="ui.size!"
      [nzExpandedKeys]="ui.expandedKeys!"
      [nzNotFoundContent]="ui.notFoundContent"
      [nzMaxTagCount]="ui.maxTagCount!"
      [nzMaxTagPlaceholder]="ui.maxTagPlaceholder || null"
      [nzTreeTemplate]="ui.treeTemplate!"
      [nzDisabled]="disabled"
      [nzShowSearch]="ui.showSearch"
      [nzShowIcon]="ui.showIcon"
      [nzDropdownMatchSelectWidth]="ui.dropdownMatchSelectWidth"
      [nzMultiple]="ui.multiple"
      [nzHideUnMatched]="ui.hideUnMatched"
      [nzCheckable]="ui.checkable"
      [nzShowExpand]="ui.showExpand"
      [nzShowLine]="ui.showLine"
      [nzCheckStrictly]="ui.checkStrictly"
      [nzAsyncData]="asyncData"
      [nzNodes]="$any(nodes)"
      [nzDefaultExpandAll]="ui.defaultExpandAll"
      [nzDisplayWith]="ui.displayWith!"
      [ngModel]="value"
      [nzVirtualHeight]="ui.virtualHeight!"
      [nzVirtualItemSize]="ui.virtualItemSize || 24"
      [nzVirtualMaxBufferPx]="ui.virtualMaxBufferPx || 500"
      [nzVirtualMinBufferPx]="ui.virtualMinBufferPx || 28"
      (ngModelChange)="change($event)"
      (nzExpandChange)="expandChange($event)"
    >
    </nz-tree-select>
  </sf-item-wrap>`,
  standalone: true,
  imports: [FormsModule, DelonFormModule, NzTreeSelectModule],
  preserveWhitespaces: false,
  encapsulation: ViewEncapsulation.None,
})
export class OrgTreeSelectWidget extends ControlUIWidget<OrgTreeSelectWidgetSchema> implements OnInit {
  constructor(injector: Injector) {
    super();
    this.orgs$ = geex.identity.orgs as unknown as Signal<Org[]>;
    this.userData$ = geex.authentication.user as unknown as Signal<User>;
  }

  static readonly KEY = "org-tree-select";

  nodes: SFSchemaEnum[] = [];
  asyncData = false;
  orgs$: Signal<Org[]>;
  userData$: Signal<User>;

  @ViewChild(NzTreeSelectComponent)
  nzTreeSelect!: NzTreeSelectComponent;

  async ngOnInit(): Promise<void> {
    const { ui } = this;
    this.ui = {
      mergeChecked: false,
      allowClear: ui.allowClear ?? false,
      filter: ui.filter,
      change: ui.change,
      showSearch: toBool(ui.showSearch, true),
      dropdownMatchSelectWidth: toBool(ui.dropdownMatchSelectWidth, false),
      multiple: toBool(ui.multiple, false),
      checkable: toBool(ui.checkable, ui.multiple ?? false),
      showIcon: toBool(ui.showIcon, false),
      showExpand: toBool(ui.showExpand, true),
      showLine: toBool(ui.showLine, false),
      checkStrictly: toBool(ui.checkStrictly, false),
      hideUnMatched: toBool(ui.hideUnMatched, false),
      defaultExpandAll: toBool(ui.defaultExpandAll, false),
      displayWith: ui.displayWith || ((node: NzTreeNode) => node.title),
      spanControl: ui.spanControl || 19,
      spanLabel: ui.spanLabel || 5,
      width: ui.width,
      maxTagCount: ui.maxTagCount || 5,
      virtualHeight: ui.virtualHeight || "300px",
    };
    this.asyncData = typeof ui.expandChange === "function";
    ui.orgType ??= [OrgTypeEnum.Default];
    const [orgs, userData] = [this.orgs$(), this.userData$()];
    const ownershipFilter = this.injector.get(GEEX_ORG_OWNERSHIP_FILTER);
    const superAdminId = this.injector.get(GEEX_SUPER_ADMIN_USER_ID);
    const arrSrv = this.injector.get(ArrayService);
    let allOwnedOrgs: Org[];
    if (userData.id === superAdminId) {
      allOwnedOrgs = deepCopy(orgs);
    } else if (this.ui.filter == undefined) {
      allOwnedOrgs = ownershipFilter(orgs, userData);
    } else {
      allOwnedOrgs = orgs.where(x => x != null && this.ui.filter!(x));
    }

    const data = allOwnedOrgs
      .orderBy(x => x.code)
      .where(x => x != null && ui.orgType!.contains(x.orgType))
      .map(x => ({
        code: x.code,
        name: x.name,
        expanded: x.code.lastIndexOf(".") == -1,
        pCode: x.code.substring(0, x.code.lastIndexOf(".")),
      }));
    const treeNodes = arrSrv.arrToTreeNode(data, {
      parentIdMapName: "pCode",
      idMapName: "code",
      titleMapName: "name",
    });

    this.schema.enum = this.nodes = treeNodes;
    if (this.schema.default == undefined && (this.value == undefined || this.value == null || this.value == "")) {
      if (this.ui.multiple) {
        const value = this.schema.enum
          .flatMapDeep((x) => ((x as SFSchemaEnum).children ?? []) as SFSchemaEnum[])
          .map((x: SFSchemaEnum) => x.key);
        this.schema.default = value;
      } else {
        const value = this.schema.enum
          .flatMapDeep((x) => ((x as SFSchemaEnum).children ?? []) as SFSchemaEnum[])
          .map((x: SFSchemaEnum) => x.key)
          .firstOrDefault();
        this.schema.default = value;
      }
    }
  }

  override reset(value: SFValue): void {
    getData(this.schema, this.ui, value).subscribe(list => {
      this.nodes = list;
      this.setValue(value);
    });
  }

  change(value: string[] | string): void {
    if (this.ui.multiple) {
      value = this.nzTreeSelect.nzTreeService.checkedNodeList.map(x => x.key);
    }
    if (this.ui.change) {
      this.ui.change(value);
    }
    this.setValue(value);
  }

  expandChange(e: NzFormatEmitEvent): void {
    const { ui } = this;
    if (typeof ui.expandChange !== "function") return;
    ui.expandChange(e).subscribe(res => {
      e.node!.clearChildren();
      e.node!.addChildren(res);
    });
  }
}
