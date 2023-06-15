import { Injectable } from "@angular/core";
import { Resolve, Routes, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { ReuseTabCached, ReuseTabService } from "@delon/abc/reuse-tab";
import { MenuService } from "@delon/theme";

@Injectable({ providedIn: "root" })
export class PreserveQueryParamsResolver implements Resolve<void> {
  constructor(private router: Router, private reuseTabService: ReuseTabService, private menuSrv: MenuService) {}

  async resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    let root = state.root;
    let child = root.firstChild;
    let tail = root;
    // 递归寻找终点route
    while (child) {
      tail = child;
      child = child.firstChild;
    }
    /**
     *判断单例页面是否允许reuseTab 出现重复标签,
     *优先采用路由上singleton的配置；
     *默认：菜单上路由不允许重复出现
     */
    if (tail.data.singleton) {
      this.redirectUrl(state);
    } else {
      // url  和路由匹配
      let url = tail["_routerState"].url;
      if (tail["_routerState"].url.includes("?")) {
        let index = tail["_routerState"].url.indexOf("?");
        url = tail["_routerState"].url.substring(0, index);
      }
      let menu = this.menuSrv.menus.flatMapDeep(x => x.children).find(item => item.link === url);
      if (menu) {
        await this.redirectUrl(state);
      }
    }
  }
  async redirectUrl(state: RouterStateSnapshot) {
    // !重新导航后页面tab会存在没有及时更新的情况，这里手动刷新，后续处理
    // todo: 修复当前tab顺序自动切到最后的问题
    setTimeout(() => {
      this.reuseTabService.refresh();
    }, 1000);
    let url = state.url;
    if (state.url.includes("?")) {
      let index = state.url.indexOf("?");
      url = state.url.substring(0, index);
      // 判断地址在缓存中是否已存在(/xxx,/xxx?id=1,/xxx?hello="word")
      let existNoParams = this.reuseTabService.exists(url);
      let existParams = this.reuseTabService.items.findIndex(x => x.url.startsWith(`${url}?`) && x.url !== state.url) > -1;

      if (existNoParams) {
        let targetRouteIndex = this.reuseTabService.items.findIndex(x => x.url == url);
        this.reuseTabService["_cached"].splice(targetRouteIndex, 1);
        return;
      }
      if (existParams) {
        let targetRouteIndex = this.reuseTabService.items.findIndex(x => x.url.startsWith(`${url}?`));
        this.reuseTabService["_cached"].splice(targetRouteIndex, 1);
        return;
      }
    }

    // 如果即将去到的url,是当前激活reuseTab 那么直接定向到当前激活reuseTab
    if (this.reuseTabService.curUrl.startsWith(`${state.url}?`)) {
      await this.router.navigateByUrl(this.reuseTabService.curUrl);
      return;
    }

    // 根据/path匹配到/path?p=1的缓存
    // 如果即将去到的是reuse缓存中的url,就定向到对应reuseTab缓存的url，否则不做干预
    var cache = (this.reuseTabService["_cached"] as ReuseTabCached[]).firstOrDefault(x => x.url.startsWith(`${state.url}?`));
    // 如果当前url和缓存不一致则重定向至缓存url
    if (cache && cache.url != state.url) {
      await this.router.navigateByUrl(cache.url);
    }
  }
}
