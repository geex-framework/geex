

import { Directive, effect, ElementRef, inject, Input, OnInit, Signal } from "@angular/core";
import { ArrayService } from "@delon/util";
import { NzTreeSelectComponent } from "ng-zorro-antd/tree-select";
import { merge, Subscription } from "rxjs";
import { filter, tap } from "rxjs/operators";

import { geex } from "@geexcode/geex-angular";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { OrgTypeEnum, type Org, type User } from "@geexcode/geex-extensions-identity";
import { GEEX_ORG_OWNERSHIP_FILTER } from "@geexcode/geex-extensions-identity";
import { OrgPickerLevel } from "./org-picker-level";

type OrgCacheItem = Org & {
  pCode?: string;
};

@Directive({
  selector: "[org-tree-select-picker]",
  standalone: true,
  host: {
    "[style.min-width]": '"90px"',
  },
})
export class OrgTreeSelectPickerDirective implements OnInit {
  @Input() directSelection = false;

  private readonly i18n = inject(GEEX_I18N) as any;

  private readonly ownershipFilter = inject(GEEX_ORG_OWNERSHIP_FILTER);

  private _canSelectLevel: OrgPickerLevel = OrgPickerLevel.All;
  orgs$: Signal<OrgCacheItem[]>;
  userData$: Signal<User | null>;

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

  constructor(
    private host: NzTreeSelectComponent,
    private arrSrv: ArrayService,
    private el: ElementRef,
  ) {
    this.userData$ = geex.authentication.user as Signal<User | null>;
    this.orgs$ = geex.identity.orgs;

    const nativeElement = this.el.nativeElement;
    if (!nativeElement.hasAttribute("nzPlaceHolder")) {
      this.host.nzPlaceHolder = this.i18n.Identity.widget.selectOrg;
    }
    if (!nativeElement.hasAttribute("nzDropdownMatchSelectWidth")) {
      this.host.nzDropdownMatchSelectWidth = false;
    }
    if (!nativeElement.hasAttribute("nzAllowClear")) {
      this.host.nzAllowClear = true;
    }
    if (!nativeElement.hasAttribute("nzMultiple")) {
      this.host.nzMultiple = true;
    }
    if (!nativeElement.hasAttribute("nzCheckable")) {
      this.host.nzCheckable = true;
    }
    effect(() => {
      this.reloadNodes();
    });
  }

  ngOnInit(): void {
    (this.host as any)["selectionChangeSubscription"] = this.subscribeSelectionChange();
  }

  private reloadNodes() {
    const [orgs, userData] = [this.orgs$(), this.userData$()];
    const allOwnedOrgs = this.ownershipFilter(orgs, userData!);
    const data = allOwnedOrgs
      .orderBy(x => x.code)
      .where(x => x?.orgType == OrgTypeEnum.Default)
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
    const rootCode = data.find(x => !x.pCode)?.code;
    this.host.expandedKeys = rootCode ? [rootCode] : [];
    setTimeout(() => {
      this.host.updateSelectedNodes(true);
    });
  }

  subscribeSelectionChange(): Subscription {
    const _this = this;
    return merge(
      this.host.nzTreeClick.pipe(
        tap(function (event) {
          const node = event.node;
          if (!node) {
            return;
          }
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
        }),
        filter(function (event) {
          const node = event.node;
          if (!node) {
            return false;
          }
          return _this.host.nzCheckable ? !node.isDisabled && !node.isDisableCheckbox : !node.isDisabled && node.isSelectable;
        }),
      ),
      this.host.nzCleared,
      this.host.nzRemoved,
      this.host.nzTreeCheckboxChange,
    ).subscribe(function () {
      _this.host.updateSelectedNodes();
      const value = _this.host.selectedNodes.flatMapDeep(x => x.children).map(x => x.key);
      _this.host.value = [...value];
      if (_this.host.isMultiple) {
        _this.host.onChange(_this.host.value);
        _this.host.focusOnInput();
        _this.host.updatePosition();
      } else {
        _this.host.closeDropDown();
        _this.host.onChange(_this.host.value.length ? _this.host.value : null);
      }
    });
  }
}
