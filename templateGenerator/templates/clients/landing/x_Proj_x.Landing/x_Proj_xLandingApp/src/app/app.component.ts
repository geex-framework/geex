/* eslint-disable no-irregular-whitespace */
import { Location } from "@angular/common";
import { Component, ElementRef, Inject, OnInit, Renderer2 } from "@angular/core";
import {
  ActivatedRouteSnapshot,
  ActivationEnd,
  Navigation,
  NavigationEnd,
  NavigationExtras,
  NavigationStart,
  ResolveEnd,
  Router,
} from "@angular/router";
import { ReuseTabMatchMode, ReuseTabService } from "@delon/abc/reuse-tab";
import { TitleService, VERSION as VERSION_ALAIN } from "@delon/theme";
import { NzModalService } from "ng-zorro-antd/modal";
import { VERSION as VERSION_ZORRO } from "ng-zorro-antd/version";
import { timer } from "rxjs";
import { debounceTime, filter, map, shareReplay } from "rxjs/operators";

@Component({
  selector: "app-root",
  template: ` <router-outlet></router-outlet> `,
})
export class AppComponent {
  constructor(
    el: ElementRef,
    renderer: Renderer2,
    private router: Router,
    private location: Location,
    private tabSrv: ReuseTabService,
    private titleSrv: TitleService,
    private modalSrv: NzModalService,
  ) {
    tabSrv.mode = ReuseTabMatchMode.URL;
    tabSrv.excludes = [/\/passport/, /\/exception/];
    this.titleSrv.default = "";
    this.titleSrv.suffix = "x_proj_x";
    renderer.setAttribute(el.nativeElement, "ng-alain-version", VERSION_ALAIN.full);
    renderer.setAttribute(el.nativeElement, "ng-zorro-version", VERSION_ZORRO.full);
    this.router.virtualNavigate = (commands: any[], extras?: NavigationExtras) => {
      // let url = router.createUrlTree(commands, extras);
      // const resultUrl = url.toString();
      // history.replaceState({}, "", resultUrl);
    };
    this.router.navigationStart$ = this.router.events.pipe(filter((e): e is NavigationStart => e instanceof NavigationStart));
    this.router.navigationEnd$ = this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd));
    this.router.events
      .pipe(
        filter((e): e is ActivationEnd => e instanceof ActivationEnd),
        debounceTime(25),
      )
      .subscribe(x => {
        this.modalSrv.closeAll();
        titleSrv.updateTitle(x.snapshot, this.tabSrv.getTitle(x.snapshot.getResolvedUrl(), x.snapshot).text);
      });
    let currentNavigation: Navigation = undefined;
    this.router.navigationStart$.subscribe(x => {
      console.debug(x);
      currentNavigation = router.getCurrentNavigation();
      // const segments = currentNavigation.extractedUrl.root.children.primary.segments;
      // if (segments.firstOrDefault().path == this.baseUrl) {
      //   segments.removeAt(0);
      // }
      if (currentNavigation.extras?.forceReload == true) {
        currentNavigation.extractedUrl.queryParams.reloadTs = new Date().getTime();
      }
    });
    this.router.navigationEnd$.subscribe(x => {
      console.debug(x);
      const initialUrl = currentNavigation?.initialUrl?.toString();
      const extractedUrl = currentNavigation?.extractedUrl?.toString();
      if (tabSrv.items.length > 0 && currentNavigation?.extras?.replaceUrl == true && extractedUrl != initialUrl) {
        tabSrv.close(currentNavigation.initialUrl.toString());
      }
      setTimeout(() => {
        window["__component"] = tabSrv.componentRef?.instance;
      });
    });
  }
}
