import { Inject, Injectable, InjectionToken, Optional } from "@angular/core";
import { WechatAuthConfig } from "./types";

export const WECHAT_AUTH_CONFIG = new InjectionToken<WechatAuthConfig>("WECHAT_AUTH_CONFIG");

const WX_LOGIN_SCRIPT = "https://res.wx.qq.com/connect/zh_CN/htmledition/js/wxLogin.js";

@Injectable()
export class WechatWebLoginService {
  constructor(@Optional() @Inject(WECHAT_AUTH_CONFIG) private readonly config: WechatAuthConfig | null) {}

  getConfig(overrides?: Partial<WechatAuthConfig>): WechatAuthConfig {
    const merged = { ...(this.config ?? { webAppId: "", redirectUri: "" }), ...overrides };
    if (!merged.webAppId || !merged.redirectUri) {
      throw new Error("WechatAuthConfig.webAppId and redirectUri are required.");
    }
    return merged;
  }

  buildAuthorizeUrl(overrides?: Partial<WechatAuthConfig> & { state?: string }): string {
    const config = this.getConfig(overrides);
    const state = overrides?.state ?? "WechatWeb";
    const redirectUri = encodeURIComponent(config.redirectUri);
    return `https://open.weixin.qq.com/connect/qrconnect?appid=${encodeURIComponent(config.webAppId)}&redirect_uri=${redirectUri}&response_type=code&scope=snsapi_login&state=${encodeURIComponent(state)}#wechat_redirect`;
  }

  async loadWxLoginScript(): Promise<void> {
    if (typeof window === "undefined") {
      return;
    }
    if (window.WxLogin) {
      return;
    }
    await new Promise<void>((resolve, reject) => {
      const existing = document.querySelector(`script[src="${WX_LOGIN_SCRIPT}"]`);
      if (existing) {
        existing.addEventListener("load", () => resolve());
        existing.addEventListener("error", () => reject(new Error("Failed to load WxLogin.js")));
        return;
      }
      const script = document.createElement("script");
      script.src = WX_LOGIN_SCRIPT;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error("Failed to load WxLogin.js"));
      document.body.appendChild(script);
    });
  }

  async renderQr(overrides?: Partial<WechatAuthConfig> & { state?: string }): Promise<void> {
    const config = this.getConfig(overrides);
    await this.loadWxLoginScript();
    const id = config.containerId ?? "wechat_login_container";
    if (!window.WxLogin) {
      throw new Error("WxLogin is not available.");
    }
    new window.WxLogin({
      self_redirect: false,
      id,
      appid: config.webAppId,
      scope: "snsapi_login",
      redirect_uri: encodeURIComponent(config.redirectUri),
      state: overrides?.state ?? "WechatWeb",
      style: config.style,
      href: config.href,
    });
  }

  extractCallbackCode(search: string = typeof location !== "undefined" ? location.search : ""): {
    code?: string;
    state?: string;
  } {
    const params = new URLSearchParams(search.startsWith("?") ? search : `?${search}`);
    return {
      code: params.get("code") ?? undefined,
      state: params.get("state") ?? undefined,
    };
  }
}
