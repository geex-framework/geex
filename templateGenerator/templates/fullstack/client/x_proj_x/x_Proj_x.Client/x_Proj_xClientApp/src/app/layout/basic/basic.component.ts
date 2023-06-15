/* eslint-disable no-irregular-whitespace */
import { ChangeDetectorRef, Component } from "@angular/core";
import { ActivatedRouteSnapshot } from "@angular/router";
import { ReuseTabMatchMode, ReuseTabService } from "@delon/abc/reuse-tab";
import { SettingsService, User } from "@delon/theme";
import { LayoutDefaultOptions } from "@delon/theme/layout-default";
import { environment } from "@env/environment";
import { timer } from "rxjs";

const DefaultWatermarkSettings = {
  watermark_txt: "",
  watermark_x: 0,
  watermark_y: 0,
  watermark_rows: 0,
  watermark_cols: 0,
  watermark_x_space: 0,
  watermark_y_space: 0,
  watermark_color: "#d9d9d9",
  watermark_alpha: "0.3",
  watermark_fontsize: "15px",
  watermark_font: "微软雅黑",
  watermark_width: 300,
  watermark_height: 250,
  watermark_angle: 20,
};
@Component({
  selector: "layout-basic",
  templateUrl: "./basic.component.html",
})
export class LayoutBasicComponent {
  options: LayoutDefaultOptions = {
    logoExpanded: `./assets/logo-txt-white-shield.png`,
    logoCollapsed: `./assets/logo.png`,
    // hideAside: window.isMobile,
  };
  searchToggleStatus = false;
  showSettingDrawer = !environment.production;
  isMobile = window.isMobile;
  drawMenuVisitable = false;
  get user(): User {
    return this.settings.user;
  }
  get fullScreen() {
    return window.fullScreen;
  }
  constructor(private settings: SettingsService, private cdr: ChangeDetectorRef) {
    //#region 重写reuseTab行为
    let getTitle = ReuseTabService.prototype.getTitle;
    ReuseTabService.prototype.getTitle = function (url: string, route?: ActivatedRouteSnapshot) {
      var path = url.split("?")[0];
      if (path.length > 1) {
        url = path;
      }
      return getTitle.bind(this)(url.split("?")[0], route);
    };
    /**
     * 根据快照获取URL地址
     *
     * @param {?} route
     * @return {?}
     */
    ReuseTabService.prototype.getUrl = function (route) {
      /** @type {?} */
      let next = this.getTruthRoute(route);
      let queryParams = Object.entries(next.queryParams);
      /** @type {?} */
      let segments = [];
      while (next) {
        segments.push(next.url.join("/"));
        next = /** @type {?} */ next.parent;
      }
      /** @type {?} */
      let url = `/${segments
        .filter(
          /**
           * @param {?} i
           * @return {?}
           */
          function (i) {
            return i;
          },
        )
        .reverse()
        .join("/")}`;
      return queryParams.length ? `${url}?${queryParams.map(x => `${x[0]}=${x[1]}`).join("&")}` : url;
    };
    ReuseTabService.prototype.store = function (_snapshot, _handle) {
      var _a;
      const url = this.getUrl(_snapshot);
      const idx = this.index(url);
      const isAdd = idx === -1;
      const item = {
        title: this.getTitle(url, _snapshot),
        closable: this.getClosable(url, _snapshot),
        position: this.getKeepingScroll(url, _snapshot) ? this.positionBuffer[url] : null,
        url,
        // !此处添加了queryParams
        queryParams: _snapshot.queryParams,
        _snapshot,
        _handle,
      };
      if (isAdd) {
        if (this.count >= this._max) {
          // Get the oldest closable location
          const closeIdx = this._cached.findIndex(w => w.closable);
          if (closeIdx !== -1) this.remove(closeIdx, false);
        }
        this._cached.push(item);
      } else {
        // Current handler is null when activate routes
        // For better reliability, we need to wait for the component to be attached before call _onReuseInit
        const cahcedComponentRef = (_a = this._cached[idx]._handle) === null || _a === void 0 ? void 0 : _a.componentRef;
        if (_handle == null && cahcedComponentRef != null) {
          timer(100).subscribe(() => this.runHook("_onReuseInit", cahcedComponentRef));
        }
        this._cached[idx] = item;
      }
      this.removeUrlBuffer = null;
      this.di("#store", isAdd ? "[new]" : "[override]", url);
      if (_handle && _handle.componentRef) {
        this.runHook("_onReuseDestroy", _handle.componentRef);
      }
      if (!isAdd) {
        this._cachedChange.next({ active: "override", item, list: this._cached });
      }
    };
    //#endregion
  }
  ngAfterViewInit(): void {
    // 开启水印
    if (environment["enableWatermarking"]) {
      this.watermark({});
    }
  }
  watermark(settings: Partial<typeof DefaultWatermarkSettings>) {
    // 默认设置
    settings = { ...DefaultWatermarkSettings, ...settings, watermark_txt: `${this.settings?.user.username}` };
    if (arguments.length === 1 && typeof arguments[0] === "object") {
      const src = arguments[0] || {};
      for (const key in src) {
        if (src[key] && settings[key] && src[key] === settings[key]) {
          continue;
        } else if (src[key]) {
          settings[key] = src[key];
        }
      }
    }
    const oTemp = document.createElement("div");
    oTemp.style.width = "99vw";
    oTemp.style.height = "99vh";
    oTemp.style.position = "fixed";
    oTemp.style.overflow = "hidden";
    oTemp.style.left = "0";
    oTemp.style.top = "20px";
    oTemp.style.pointerEvents = "none";
    oTemp.style.zIndex = "999999";
    // 获取页面最大宽度
    const page_width = Math.max(document.body.scrollWidth, document.body.clientWidth);
    // 获取页面最大高度
    const page_height = Math.max(document.body.scrollHeight, document.body.clientHeight);
    // 如果将水印列数设置为0，或水印列数设置过大，超过页面最大宽度，则重新计算水印列数和水印x轴间隔
    if (
      settings.watermark_cols == 0 ||
      settings.watermark_x +
        settings.watermark_width * settings.watermark_cols +
        settings.watermark_x_space * (settings.watermark_cols - 1) >
        page_width
    ) {
      settings.watermark_cols =
        (page_width - settings.watermark_x + settings.watermark_x_space) / (settings.watermark_width + settings.watermark_x_space);
      settings.watermark_x_space =
        (page_width - settings.watermark_x - settings.watermark_width * settings.watermark_cols) / (settings.watermark_cols - 1);
    }
    // 如果将水印行数设置为0，或水印行数设置过大，超过页面最大长度，则重新计算水印行数和水印y轴间隔
    if (
      settings.watermark_rows == 0 ||
      settings.watermark_y +
        settings.watermark_height * settings.watermark_rows +
        settings.watermark_y_space * (settings.watermark_rows - 1) >
        page_height
    ) {
      settings.watermark_rows =
        (settings.watermark_y_space + page_height - settings.watermark_y) / (settings.watermark_height + settings.watermark_y_space);
      settings.watermark_y_space =
        (page_height - settings.watermark_y - settings.watermark_height * settings.watermark_rows) / (settings.watermark_rows - 1);
    }
    let x;
    let y;
    for (let i = 0; i < settings.watermark_rows; i++) {
      y = settings.watermark_y + (settings.watermark_y_space + settings.watermark_height) * i;
      for (let j = 0; j < settings.watermark_cols; j++) {
        x = settings.watermark_x + (settings.watermark_width + settings.watermark_x_space) * j;
        const mask_div = document.createElement("div");
        mask_div.id = `mask_div${i}${j}`;
        mask_div.className = "mask_div";
        mask_div.appendChild(document.createTextNode(settings.watermark_txt));
        mask_div.appendChild(document.createElement("br"));
        mask_div.appendChild(document.createTextNode(new Date().format("yyyy-MM-dd")));
        // 设置水印div倾斜显示
        mask_div.style.transform = `rotate(-${settings.watermark_angle}deg)`;
        mask_div.style.visibility = "";
        mask_div.style.position = "absolute";
        mask_div.style.left = `${x}px`;
        mask_div.style.top = `${y}px`;
        mask_div.style.overflow = "hidden";
        mask_div.style.zIndex = "9999";
        // 让水印不遮挡页面的点击事件
        mask_div.style.pointerEvents = "none";
        mask_div.style.opacity = settings.watermark_alpha;
        mask_div.style.fontSize = settings.watermark_fontsize;
        mask_div.style.fontFamily = settings.watermark_font;
        mask_div.style.color = settings.watermark_color;
        mask_div.style.textAlign = "center";
        mask_div.style.width = `${settings.watermark_width}px`;
        mask_div.style.height = `${settings.watermark_height}px`;
        mask_div.style.display = "block";
        oTemp.appendChild(mask_div);
      }
    }
    document.getElementById("container").appendChild(oTemp);
    // document.body.appendChild(oTemp);
  }
  toggleMenu(visible?: boolean) {
    if (visible !== undefined) {
      this.drawMenuVisitable = !this.drawMenuVisitable;
    } else {
      this.drawMenuVisitable = visible;
    }
  }
}
