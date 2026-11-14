import { Directive, inject, OnDestroy, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { ReuseTabComponent } from "@delon/abc/reuse-tab";
import { firstValueFrom, from } from "rxjs";
import { GeexTabService } from "./geex-tab.service";

@Directive({
  selector: "layout-default-nav[geexTabMenu]",
  standalone: true,
  providers: [{ provide: Router, useFactory: () => {
    const router = inject(Router, { skipSelf: true });
    const tabs = inject(GeexTabService);
    return new Proxy(router, { get(target, property) {
      if (property === "navigateByUrl") return tabs.openMenu.bind(tabs);
      const value = Reflect.get(target, property, target);
      return typeof value === "function" ? value.bind(target) : value;
    } });
  } }],
})
export class GeexTabMenuDirective {}

@Directive({ selector: "reuse-tab[geexTabs]", standalone: true })
export class GeexTabsDirective implements OnInit, OnDestroy {
  private readonly host = inject(ReuseTabComponent);
  private readonly tabs = inject(GeexTabService);
  private originalClose?: ReuseTabComponent["canClose"];
  private readonly originalContextMenu = this.host.contextMenuChange;
  private readonly originalTo = this.host._to;

  ngOnInit(): void {
    this.originalClose = this.host.canClose;
    this.host.canClose = options => from((async () => {
      if (this.originalClose && !await firstValueFrom(this.originalClose(options))) return false;
      if (await this.tabs.close(options.item.url, options.includeNonCloseable)) this.host.close.emit(options.item);
      return false;
    })());
    this.host._to = (index, callback) => {
      const item = this.host.list[index];
      if (!item) return;
      void this.tabs.activate(item.url).then(success => {
        if (!success) return;
        this.host.item = item;
        this.host.change.emit(item);
        callback?.();
      });
    };
    this.host.contextMenuChange = event => {
      if (event.type !== "closeOther" && event.type !== "closeRight") {
        this.originalContextMenu.call(this.host, event);
        return;
      }
      void this.tabs.closeOthers(event.item.url, event.type === "closeRight", event.includeNonCloseable)
        .then(success => { if (success) this.host.close.emit(null); });
    };
  }

  ngOnDestroy(): void {
    this.host.canClose = this.originalClose;
    this.host.contextMenuChange = this.originalContextMenu;
    this.host._to = this.originalTo;
  }
}
