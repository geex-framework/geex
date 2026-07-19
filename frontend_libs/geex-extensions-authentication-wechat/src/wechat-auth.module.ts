import { Injector } from "@angular/core";
import { ApolloClient } from "@apollo/client/core";
import { Apollo } from "apollo-angular";
import { OAuthService } from "angular-oauth2-oidc";
import { firstValueFrom, isObservable } from "rxjs";
import { geex } from "@geexcode/geex-angular";
import { LINK_LOGIN, REGISTER_AND_LINK_LOGIN, RESOLVE_LOGIN } from "./graphql";
import type {
  EstablishSessionOptions,
  GeexAuthenticationWechatOptions,
  ResolveLoginResult,
  WechatAuthModule,
  WechatAuthRenderOverrides,
  WechatLoginProvider,
} from "./wechat-auth.types";

const WX_LOGIN_SCRIPT = "https://res.wx.qq.com/connect/zh_CN/htmledition/js/wxLogin.js";

type ApolloLike = Apollo | ApolloClient;

type MockingWechatHost = {
  getCapabilities(): Promise<{ enabled: boolean; wechatWeb: boolean }>;
  renderWechatQr(overrides?: WechatAuthRenderOverrides): Promise<void>;
};

async function mutate<T>(
  client: ApolloLike,
  options: { mutation: unknown; variables: Record<string, unknown>; context?: Record<string, unknown> },
): Promise<T> {
  const result = (client as Apollo).mutate<T>(options as never) as unknown;
  if (isObservable(result)) {
    const response = await firstValueFrom(result as import("rxjs").Observable<{ data: T }>);
    return response.data;
  }
  const response = await (result as Promise<{ data: T }>);
  return response.data;
}

export function createWechatAuthModule(
  injector: Injector,
  options: Readonly<GeexAuthenticationWechatOptions>,
): WechatAuthModule {
  const apollo = () => injector.get(Apollo);
  const oauth = () => injector.get(OAuthService);

  const getConfig = (overrides?: Partial<GeexAuthenticationWechatOptions>): GeexAuthenticationWechatOptions => {
    const merged = { ...options, ...overrides };
    if (!merged.webAppId || !merged.redirectUri) {
      throw new Error("GeexAuthenticationWechatOptions.webAppId and redirectUri are required.");
    }
    return merged;
  };

  const loadWxLoginScript = async (): Promise<void> => {
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
  };

  const establishSession = (sessionOptions: EstablishSessionOptions): void => {
    // Hand Geex token to IdP; browser leaves this page and returns via OIDC callback,
    // which is then consumed once by GeexStartupService.tryOidcCodeCallback.
    oauth().initCodeFlow(sessionOptions.returnUrl ?? "/", { access_token: sessionOptions.token });
  };

  const resolveLogin = async (params: {
    loginProvider: WechatLoginProvider;
    code: string;
  }): Promise<ResolveLoginResult> => {
    const data = await mutate<{ resolveLogin: ResolveLoginResult }>(apollo(), {
      mutation: RESOLVE_LOGIN,
      variables: {
        loginProvider: params.loginProvider,
        code: params.code,
      },
    });
    return data.resolveLogin;
  };

  const module: WechatAuthModule = {
    getConfig,
    buildAuthorizeUrl: (overrides?: WechatAuthRenderOverrides) => {
      const config = getConfig(overrides);
      const state = overrides?.state ?? "WechatWeb";
      const redirectUri = encodeURIComponent(config.redirectUri);
      return `https://open.weixin.qq.com/connect/qrconnect?appid=${encodeURIComponent(config.webAppId)}&redirect_uri=${redirectUri}&response_type=code&scope=snsapi_login&state=${encodeURIComponent(state)}#wechat_redirect`;
    },
    renderQr: async (overrides?: WechatAuthRenderOverrides) => {
      const mocking = (geex as unknown as { mocking?: MockingWechatHost }).mocking;
      if (mocking?.getCapabilities && mocking.renderWechatQr) {
        const caps = await mocking.getCapabilities();
        if (caps.enabled && caps.wechatWeb) {
          await mocking.renderWechatQr(overrides);
          return;
        }
      }

      const config = getConfig(overrides);
      await loadWxLoginScript();
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
    },
    extractCallbackCode: (search: string = typeof location !== "undefined" ? location.search : "") => {
      const params = new URLSearchParams(search.startsWith("?") ? search : `?${search}`);
      return {
        code: params.get("code") ?? undefined,
        state: params.get("state") ?? undefined,
      };
    },
    resolveLogin,
    linkLogin: async (userLoginLinkToken, context) => {
      const data = await mutate<{
        linkLogin: { token?: string | null; userId: string; loginProvider: string };
      }>(apollo(), {
        mutation: LINK_LOGIN,
        variables: { userLoginLinkToken },
        context,
      });
      return data.linkLogin;
    },
    registerAndLinkLogin: async params => {
      const data = await mutate<{
        registerAndLinkLogin: { token?: string | null; userId: string; loginProvider: string };
      }>(apollo(), {
        mutation: REGISTER_AND_LINK_LOGIN,
        variables: params,
      });
      return data.registerAndLinkLogin;
    },
    establishSession,
    loginWithMiniProgramCode: async (code, returnUrl) => {
      const result = await resolveLogin({
        loginProvider: "WechatMiniProgram",
        code,
      });
      if (result.isLinked && result.session?.token) {
        establishSession({
          token: result.session.token,
          returnUrl,
        });
      }
      return result;
    },
    init: async () => undefined,
  };

  return module;
}
