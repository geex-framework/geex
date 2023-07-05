import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Injector, OnInit, ViewChild, ViewEncapsulation } from "@angular/core";
import { ApolloQueryResult } from "@apollo/client/core";
import {
  ControlUIWidget,
  SFTreeSelectWidgetSchema,
  SFSchemaEnum,
  toBool,
  SFValue,
  getData,
  SFSchemaEnumType,
  SFComponent,
  SFItemComponent,
} from "@delon/form";
import { ArrayService, deepCopy } from "@delon/util";
import { Select, Store } from "@ngxs/store";
import { Apollo } from "apollo-angular";
import { isArray } from "lodash-es";
import { NzFormatEmitEvent, NzTreeNode } from "ng-zorro-antd/core/tree";
import { NzTreeSelectComponent } from "ng-zorro-antd/tree-select";
import { combineLatest, Observable, of } from "rxjs";
import { map, take } from "rxjs/operators";

import {
  OrgsQuery,
  OrgsQueryVariables,
  OrgsGql,
  OrgBriefFragment,
  Org,
  OrgTypeEnum,
  OrgFilterInput,
  OrgCacheItemFragment,
} from "../graphql/.generated/type";
import { CacheDataState, CacheDataState$ } from "../states/cache-data.state";
import { UserDataState, UserDataStateModel } from "../states/user-data.state";

export type OrgTreeSelectWidgetSchema = SFTreeSelectWidgetSchema & {
  filter?: (x: OrgCacheItemFragment) => boolean;
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  preserveWhitespaces: false,
  encapsulation: ViewEncapsulation.None,
})
export class OrgTreeSelectWidget extends ControlUIWidget<OrgTreeSelectWidgetSchema> implements OnInit {
  /**
   *
   */
  constructor(cd: ChangeDetectorRef, injector: Injector, sfItemComp?: SFItemComponent, sfComp?: SFComponent) {
    super(cd, injector, sfItemComp, sfComp);
    this.orgs$ = injector
      .get(Store)
      .select(CacheDataState)
      .pipe(map(x => x.orgs));
  }
  /* 用于注册小部件 KEY 值 */
  areaOrganizationUnitId: string;
  static readonly KEY = "org-tree-select";
  nodes: SFSchemaEnum[] = [];
  asyncData = false;
  orgs$: Observable<OrgCacheItemFragment[]>;
  @Select(UserDataState)
  userData$: Observable<UserDataStateModel>;
  @ViewChild(NzTreeSelectComponent)
  nzTreeSelect: NzTreeSelectComponent;
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
      checkable: toBool(ui.checkable, ui.multiple),
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
      virtualHeight: ui.virtualHeight || "300px",
    };
    this.asyncData = typeof ui.expandChange === "function";
    ui.orgType ??= [OrgTypeEnum.Default];
    combineLatest([this.orgs$, this.userData$])
      .pipe(
        map(([orgs, userData]) => {
          const arrSrv = this.injector.get(ArrayService);
          let allOwnedOrgs: OrgCacheItemFragment[] = [];
          //超级管理员忽视过滤
          if (userData.id == "000000000000000000000001") {
            allOwnedOrgs = deepCopy(orgs);
          } else {
            // 默认过滤没有权限的组织
            if (this.ui.filter == undefined) {
              // 找到所有父组织
              let ownedOrgCodes = userData.orgs?.map(x => x.code);
              // 找到所有开头和父组织名匹配的组织(包含祖先组织)
              this.ui.filter = x => ownedOrgCodes?.any(y => y.startsWith(x.code));
            }
            allOwnedOrgs = orgs.where(this.ui.filter);
          }

          let data = allOwnedOrgs
            .orderBy(x => x.code)
            .where(x => ui.orgType.contains(x.orgType))
            .map(x => ({
              code: x.code,
              name: x.name,
              expanded: x.code.lastIndexOf(".") == -1,
              pCode: x.code.substring(0, x.code.lastIndexOf(".")),
            }));
          return arrSrv.arrToTreeNode(data, {
            parentIdMapName: "pCode",
            idMapName: "code",
            titleMapName: "name",
          });
        }),
      )
      .subscribe(x => {
        this.schema.enum = this.nodes = x;
        window["orgLevel0Code"] = this.nodes.firstOrDefault()?.key;
        if (this.schema.default == undefined && (this.value == undefined || this.value == null || this.value == "")) {
          if (this.ui.multiple) {
            let value = this.schema.enum.flatMapDeep((x: SFSchemaEnum) => x.children).map((x: SFSchemaEnum) => x.key);
            this.schema.default = value;
          } else {
            let value = this.schema.enum
              .flatMapDeep((x: SFSchemaEnum) => x.children)
              .map((x: SFSchemaEnum) => x.key)
              .firstOrDefault();

            this.schema.default = value;
          }
        }
        this.detectChanges(true);
      });
  }
  reset(value: SFValue): void {
    getData(this.schema, this.ui, value).subscribe(list => {
      this.nodes = list;
      this.setValue(value);
      this.detectChanges(true);
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
      this.detectChanges();
    });
  }
}
