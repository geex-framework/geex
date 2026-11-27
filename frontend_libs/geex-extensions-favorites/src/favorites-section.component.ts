import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from "@angular/core";
import { RouterLink } from "@angular/router";
import { STModule, type STChange, type STColumn } from "@delon/abc/st";
import { NzCardModule } from "ng-zorro-antd/card";
import { NzTabsModule } from "ng-zorro-antd/tabs";
import { NzAlertModule } from "ng-zorro-antd/alert";
import { NzButtonModule } from "ng-zorro-antd/button";
import { FavoritesService } from "./favorites.service";
import { FavoriteButtonComponent } from "./favorite-button.component";
import { injectFavoriteLabels } from "./favorites.i18n";
import { createFavoriteQueryState, favoriteListVariables, type FavoriteQueryState } from "./favorites-query";
import type { FavoriteItem } from "./favorites.types";

@Component({
  selector: "geex-favorites-table",
  standalone: true,
  imports: [STModule, RouterLink, FavoriteButtonComponent, NzAlertModule, NzButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `@if (failed()) {
    <nz-alert nzType="error" [nzMessage]="labels().failed" />
    <button nz-button type="button" (click)="load()">{{ labels().retry }}</button>
  }
  <st [data]="items()" [columns]="columns()" [loading]="loading()" [total]="total()"
    [pi]="queryState().pageIndex" [ps]="queryState().pageSize" [page]="page" [multiSort]="false"
    [noResult]="labels().empty" (change)="tableChange($event)">
    <ng-template st-row="name" let-item>
      @if (item.detailUrl) { <a [routerLink]="item.detailUrl">{{ item.displayValue }}</a> }
      @else { {{ item.displayValue }} }
    </ng-template>
    <ng-template st-row="actions" let-item>
      <geex-favorite-button [entityType]="entityType()" [entityId]="item.entityId" appearance="link" />
    </ng-template>
  </st>`,
})
export class FavoritesTableComponent {
  readonly entityType = input.required<string>();
  readonly queryState = input.required<FavoriteQueryState>();
  readonly favorites = inject(FavoritesService);
  readonly labels = injectFavoriteLabels();
  readonly items = signal<FavoriteItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly page = { front: false, showSize: true };
  private request = 0;
  readonly columns = computed<Array<STColumn<FavoriteItem>>>(() => [
    { title: this.labels().name, index: "displayValue", render: "name" },
    {
      title: this.labels().createdOn, index: "createdOn", type: "date",
      sort: { key: "createdOn", default: this.queryState().sort, compare: null, directions: ["descend", "ascend"] },
      filter: { type: "date", date: { range: true }, menus: [{ value: this.queryState().dates.length ? this.queryState().dates : undefined }], fn: null },
    },
    { title: this.labels().actions, render: "actions" },
  ]);

  constructor() {
    effect(() => {
      this.entityType();
      this.favorites.changes();
      untracked(() => { void this.load(); });
    });
  }

  async load(): Promise<void> {
    const request = ++this.request;
    this.loading.set(true);
    this.failed.set(false);
    try {
      const page = await this.favorites.list(favoriteListVariables(this.entityType(), this.queryState()));
      if (request !== this.request) return;
      if (!page.items.length && page.totalCount > 0 && this.queryState().pageIndex > 1) {
        this.queryState().pageIndex = Math.ceil(page.totalCount / this.queryState().pageSize);
        await this.load();
        return;
      }
      this.items.set(page.items);
      this.total.set(page.totalCount);
    } catch {
      if (request === this.request) { this.items.set([]); this.total.set(0); this.failed.set(true); }
    } finally { if (request === this.request) this.loading.set(false); }
  }

  tableChange(change: STChange): void {
    const state = this.queryState();
    if (change.type === "sort") { state.sort = change.sort?.value ?? "descend"; state.pageIndex = 1; }
    else if (change.type === "filter") {
      const value = change.filter?.filter?.menus?.[0]?.value as unknown;
      state.dates = Array.isArray(value) ? value.map(item => new Date(item)) : [];
      state.pageIndex = 1;
    } else if (change.type === "pi" || change.type === "ps") { state.pageIndex = change.pi; state.pageSize = change.ps; }
    else return;
    void this.load();
  }
}

@Component({
  selector: "geex-favorites-section",
  standalone: true,
  imports: [NzCardModule, NzTabsModule, FavoritesTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `@if (favorites && types().length) {
    <nz-card id="my-favorites" [nzTitle]="labels().title" style="margin-top:16px">
      <nz-tabset [nzSelectedIndex]="selectedIndex()" (nzSelectedIndexChange)="select($event)">
        @for (type of types(); track type.entityType) {
          <nz-tab [nzTitle]="labels().entityTypes[type.entityType] || type.entityType">
            <ng-template nz-tab>
              @if (selectedType() === type.entityType) {
                <geex-favorites-table [entityType]="type.entityType" [queryState]="stateFor(type.entityType)" />
              }
            </ng-template>
          </nz-tab>
        }
      </nz-tabset>
    </nz-card>
  }`,
})
export class FavoritesSectionComponent {
  readonly favorites = inject(FavoritesService, { optional: true });
  readonly labels = injectFavoriteLabels();
  readonly types = computed(() => this.favorites?.types() ?? []);
  private readonly selected = signal<string | undefined>(undefined);
  readonly selectedType = computed(() => this.types().some(type => type.entityType === this.selected())
    ? this.selected() : this.types()[0]?.entityType);
  readonly selectedIndex = computed(() => Math.max(0, this.types().findIndex(type => type.entityType === this.selectedType())));
  private readonly queries = new Map<string, FavoriteQueryState>();
  select(index: number): void { this.selected.set(this.types()[index]?.entityType); }
  stateFor(type: string): FavoriteQueryState {
    if (!this.queries.has(type)) this.queries.set(type, createFavoriteQueryState());
    return this.queries.get(type)!;
  }
}
