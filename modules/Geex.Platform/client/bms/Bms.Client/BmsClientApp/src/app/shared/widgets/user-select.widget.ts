import { Component, OnInit, ViewEncapsulation } from "@angular/core";
import { ControlUIWidget, getData, SFSchemaEnum, SFSelectWidgetSchema, SFValue, toBool } from "@delon/form";
import { ArrayService } from "@delon/util/array";
import { Apollo } from "apollo-angular";
import type { NzSafeAny } from "ng-zorro-antd/core/types";
import { Observable, Subject } from "rxjs";
import { catchError, debounceTime, distinctUntilChanged, map, switchMap, takeUntil } from "rxjs/operators";

import { UserMenusGql, UserMenusQuery, UserMenusQueryVariables, UserMinimalFragment } from "../graphql/.generated/type";

@Component({
  selector: "sf-user-select",
  template: `
    <sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
      <nz-select
        [nzId]="id"
        [nzDisabled]="disabled"
        [(ngModel)]="_value"
        (ngModelChange)="change($event)"
        [nzSize]="ui.size!"
        [nzPlaceHolder]="ui.placeholder!"
        [nzNotFoundContent]="ui.notFoundContent"
        [nzDropdownClassName]="ui.dropdownClassName!"
        [nzAllowClear]="i.allowClear"
        [nzDropdownStyle]="ui.dropdownStyle!"
        [nzCustomTemplate]="ui.customTemplate!"
        [nzSuffixIcon]="ui.suffixIcon!"
        [nzRemoveIcon]="ui.removeIcon!"
        [nzClearIcon]="ui.clearIcon!"
        [nzMenuItemSelectedIcon]="ui.menuItemSelectedIcon!"
        [nzMaxTagPlaceholder]="ui.maxTagPlaceholder!"
        [nzDropdownRender]="ui.dropdownRender!"
        [nzAutoClearSearchValue]="i.autoClearSearchValue"
        [nzBorderless]="i.borderless"
        [nzAutoFocus]="i.autoFocus"
        [nzDropdownMatchSelectWidth]="i.dropdownMatchSelectWidth!"
        [nzServerSearch]="i.serverSearch"
        [nzMaxMultipleCount]="i.maxMultipleCount!"
        [nzMode]="i.mode!"
        [nzShowSearch]="i.showSearch"
        [nzShowArrow]="i.showArrow!"
        [nzTokenSeparators]="i.tokenSeparators!"
        [nzMaxTagCount]="i.maxTagCount!"
        [compareWith]="i.compareWith!"
        [nzOptionHeightPx]="i.optionHeightPx!"
        [nzOptionOverflowSize]="i.optionOverflowSize!"
        (nzOpenChange)="openChange($event)"
        (nzOnSearch)="onSearch($event)"
        (nzScrollToBottom)="scrollToBottom()"
      >
        <ng-container *ngIf="!loading && !hasGroup">
          <nz-option *ngFor="let o of data" [nzLabel]="o.label" [nzValue]="o.value" [nzDisabled]="o.disabled"></nz-option>
        </ng-container>
        <ng-container *ngIf="!loading && hasGroup">
          <nz-option-group *ngFor="let i of data" [nzLabel]="i.label">
            <nz-option *ngFor="let o of i.children" [nzLabel]="o.label" [nzValue]="o.value" [nzDisabled]="o.disabled"></nz-option>
          </nz-option-group>
        </ng-container>
        <nz-option *ngIf="loading" nzDisabled nzCustomContent>
          <i nz-icon nzType="loading"></i>
          {{ ui.searchLoadingText }}
        </nz-option>
      </nz-select>
    </sf-item-wrap>
  `,
  preserveWhitespaces: false,
  encapsulation: ViewEncapsulation.None,
})
export class UserSelectWidget extends ControlUIWidget<SFSelectWidgetSchema> implements OnInit {
  static KEY = "user-select";
  private search$ = new Subject<string>();
  i: SFSelectWidgetSchema;
  data: SFSchemaEnum[];
  _value: NzSafeAny;
  hasGroup = false;
  loading = false;
  users: UserMinimalFragment[];
  $users: Observable<UserMinimalFragment[]>;
  private checkGroup(list: SFSchemaEnum[]): void {
    this.hasGroup = (list || []).filter(w => w.group === true).length > 0;
  }

  ngOnInit(): void {
    const {
      autoClearSearchValue,
      borderless,
      autoFocus,
      dropdownMatchSelectWidth,
      serverSearch,
      maxMultipleCount,
      mode,
      showSearch,
      tokenSeparators,
      maxTagCount,
      compareWith,
      optionHeightPx,
      optionOverflowSize,
      showArrow,
    } = this.ui;
    this.i = {
      autoClearSearchValue: toBool(autoClearSearchValue, true),
      borderless: toBool(borderless, false),
      autoFocus: toBool(autoFocus, false),
      dropdownMatchSelectWidth: toBool(dropdownMatchSelectWidth, true),
      serverSearch: toBool(serverSearch, false),
      maxMultipleCount: maxMultipleCount || Infinity,
      mode: mode || "default",
      showSearch: toBool(showSearch, true),
      tokenSeparators: tokenSeparators || [],
      maxTagCount: maxTagCount || undefined,
      optionHeightPx: optionHeightPx || 32,
      optionOverflowSize: optionOverflowSize || 8,
      showArrow: typeof showArrow !== "boolean" ? undefined : showArrow,
      allowClear: this.ui.allowClear || true,
      compareWith: compareWith || ((o1: NzSafeAny, o2: NzSafeAny) => o1 === o2),
    };
    this.ui.asyncData = () => {
      return this.injector
        .get(Apollo)
        .use("silent")
        .query<UserMenusQuery, UserMenusQueryVariables>({
          query: UserMenusGql,
          variables: {},
        })
        .pipe(map(x => x.data.users.items.map(y => ({ label: y.username + (y.nickname !== null ? y.nickname : ""), value: y.id }))));
    };
    const onSearch = this.ui.onSearch!;
    if (onSearch) {
      this.search$
        .pipe(
          takeUntil(this.sfItemComp!.destroy$),
          distinctUntilChanged(),
          debounceTime(this.ui.searchDebounceTime || 300),
          switchMap(text => onSearch(text)),
          catchError(() => []),
        )
        .subscribe(list => {
          this.data = list;
          this.checkGroup(list);
          this.loading = false;
          this.detectChanges();
        });
    }
  }

  reset(value: SFValue): void {
    getData(this.schema, this.ui, value).subscribe(list => {
      this._value = value;
      this.data = list;
      this.checkGroup(list);
      this.detectChanges();
    });
  }

  change(values: SFValue): void {
    if (this.ui.change) {
      this.ui.change(values, this.getOrgData(values));
    }
    this.setValue(values == null ? undefined : values);
  }

  private getOrgData(values: SFValue): SFSchemaEnum | SFSchemaEnum[] {
    const srv = this.injector.get(ArrayService);
    if (!Array.isArray(values)) {
      return srv.findTree(this.data, (item: SFSchemaEnum) => item.value === values)!;
    }
    return values.map(value => srv.findTree(this.data, (item: SFSchemaEnum) => item.value === value));
  }

  openChange(status: boolean): void {
    if (this.ui.openChange) {
      this.ui.openChange(status);
    }
  }

  scrollToBottom(): void {
    if (this.ui.scrollToBottom) {
      this.ui.scrollToBottom();
    }
  }

  onSearch(value: string): void {
    if (this.ui.onSearch) {
      this.loading = true;
      this.search$.next(value);
    }
  }
}
