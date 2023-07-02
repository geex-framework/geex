import { AfterViewInit, ChangeDetectorRef, Directive, Host, Inject, Injector, Input, NgModule, OnInit } from "@angular/core";
import { NgModel } from "@angular/forms";
import { ArrayService, deepCopy } from "@delon/util";
import { Select, Store } from "@ngxs/store";
import { Apollo } from "apollo-angular";
import _, { clone, startsWith } from "lodash";
import { NzTreeNode, NzTreeNodeOptions } from "ng-zorro-antd/tree";
import { NzTreeSelectComponent } from "ng-zorro-antd/tree-select";
import { combineLatest, merge, Observable, of, Subscription } from "rxjs";
import { debounceTime, distinctUntilChanged, filter, map, take, takeWhile, tap } from "rxjs/operators";

import { OrgsQuery, OrgsQueryVariables, OrgsGql, OrgCacheItemFragment as OrgBriefFragment1, OrgTypeEnum } from "../graphql/.generated/type";
import { CacheDataStateModel, CacheDataState$, CacheDataState } from "../states/cache-data.state";
import { UserDataState, UserDataStateModel } from "../states/user-data.state";
import { OrganizationUnitDto as SingleOrgPickerItem, OrgPickerLevel } from "../utils/type.extenstion";
type OrgBriefFragment = OrgBriefFragment1 & {
  pCode?: string;
};
@Directive({
  selector: "[org-tree-select-picker]",
})
export class OrgTreeSelectPickerDirective implements OnInit {
  @Input() personalOnly = true;
  @Input() directSelection = false;
  private _canSelectLevel: OrgPickerLevel = OrgPickerLevel.All;
  orgs$: Observable<OrgBriefFragment[]>;
  @Select(UserDataState)
  userData$: Observable<UserDataStateModel>;
  @Input()
  public get canSelectLevel(): OrgPickerLevel {
    return this._canSelectLevel;
  }
  public set canSelectLevel(value: OrgPickerLevel) {
    this._canSelectLevel = value;
    this.reloadNodes();
  }
  get flattenItems() {
    return this.host.getCheckedNodeList().flatMapDeep(x => x.children);
  }
  @Input() filterLevel: [number, number] = [0, 999];

  constructor(private host: NzTreeSelectComponent, private arrSrv: ArrayService, private injector: Injector) {
    this.host.nzDropdownMatchSelectWidth = false;
    this.orgs$ = injector
      .get(Store)
      .select(CacheDataState)
      .pipe(map(x => x.orgs));
  }
  async ngOnInit(): Promise<void> {
    this.host["selectionChangeSubscription"] = this.subscribeSelectionChange();
    await this.reloadNodes();
  }
  async reloadNodes(): Promise<void> {
    await combineLatest([
      this.orgs$,
      this.userData$.pipe(
        filter(x => x != undefined),
        take(1),
      ),
    ])
      .pipe(
        map(([orgs, userData]) => {
          let allOwnedOrgs: OrgBriefFragment[] = [];
          //超级管理员忽视过滤
          if (userData.id == "000000000000000000000001") {
            allOwnedOrgs = deepCopy(orgs);
          } else {
            // 找到所有父组织
            let ownedOrgCodes = userData.orgs.map(x => x.code);
            // 找到所有开头和父组织名匹配的组织(包含祖先组织)
            allOwnedOrgs = orgs.where(x => ownedOrgCodes.any(y => y.startsWith(x.code)));
          }
          let data = allOwnedOrgs
            .orderBy(x => x.code)
            .where(x => x.orgType == OrgTypeEnum.Default)
            .map(x => ({
              code: x.code,
              name: x.name,
              expanded: x.code.lastIndexOf(".") == -1,
              pCode: x.code.substring(0, x.code.lastIndexOf(".")),
            }));
          this.host.nzNodes = this.arrSrv.arrToTreeNode(data, {
            parentIdMapName: "pCode",
            idMapName: "code",
            titleMapName: "name",
          });
          this.host.expandedKeys = [data.find(x => !x.pCode)?.code];
          // if (Array.isArray(this.host.value) && !this.host.value.any()) {
          // let value = this.host.nzNodes.flatMapDeep((x: NzTreeNodeOptions) => x.children).map(y => y.key);
          // bug:指令方案存在时序问题, 暂时settimeout修复
          // setTimeout(() => {
          //   this.host.writeValue([...value]);
          //   this.host.onChange([...value]);
          // });
          // }
          this.host.updateSelectedNodes(true);
        }),
      )
      .toPromise();
  }
  subscribeSelectionChange(): Subscription {
    // eslint-disable-next-line @typescript-eslint/no-this-alias
    let _this = this;
    return merge(
      this.host.nzTreeClick.pipe(
        tap(
          /**
           * @param {?} event
           * @return {?}
           */
          function (event) {
            /** @type {?} */
            let node = /** @type {?} */ event.node;
            if (_this.host.nzCheckable && !node.isDisabled && !node.isDisableCheckbox) {
              node.isChecked = !node.isChecked;
              node.isHalfChecked = false;
              if (!_this.host.nzCheckStrictly) {
                _this.host.nzTreeService.conduct(node);
              }
            }
            if (_this.host.nzCheckable) {
              node.isSelected = false;
            }
          },
        ),
        filter(
          /**
           * @param {?} event
           * @return {?}
           */
          function (event) {
            /** @type {?} */
            let node = /** @type {?} */ event.node;
            return _this.host.nzCheckable ? !node.isDisabled && !node.isDisableCheckbox : !node.isDisabled && node.isSelectable;
          },
        ),
      ),
      this.host.nzCleared,
      this.host.nzRemoved,
      this.host.nzTreeCheckBoxChange,
    ).subscribe(
      /**
       * @return {?}
       */
      function () {
        _this.host.updateSelectedNodes();
        /** @type {?} */
        let value = _this.host.selectedNodes
          .flatMapDeep(
            x => x.children,
            /**
             * @param {?} node
             * @return {?}
             */ function (node) {
              return /** @type {?} */ node;
            },
          )
          .map(x => x.key);
        _this.host.value = [...value];
        if (_this.host.isMultiple) {
          _this.host.onChange(_this.host.value);
          _this.host.focusOnInput();
          _this.host.updatePosition();
        } else {
          _this.host.closeDropDown();
          _this.host.onChange(_this.host.value.length ? _this.host.value : null);
        }
      },
    );
  }
}
