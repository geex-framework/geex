import type { GeexModule } from "@geexcode/geex-angular";

export type WechatLoginProvider = "WechatWeb" | "WechatMiniProgram";

export interface GeexAuthenticationWechatOptions {
  readonly webAppId: string;
  readonly redirectUri: string;
  readonly containerId?: string;
  readonly style?: string;
  readonly href?: string;
}

export interface ResolveLoginResult {
  isLinked: boolean;
  session?: {
    token?: string | null;
    userId: string;
    loginProvider: string;
  } | null;
  userLoginLinkToken?: string | null;
  displayName?: string | null;
}

export interface EstablishSessionOptions {
  token: string;
  /** Post-login navigate target, carried as OIDC `state` through IdP roundtrip. */
  returnUrl?: string;
}

export type WechatAuthRenderOverrides = Partial<GeexAuthenticationWechatOptions> & {
  state?: string;
};

export interface WechatAuthModule extends GeexModule<{
  getConfig(overrides?: Partial<GeexAuthenticationWechatOptions>): GeexAuthenticationWechatOptions;
  buildAuthorizeUrl(overrides?: WechatAuthRenderOverrides): string;
  renderQr(overrides?: WechatAuthRenderOverrides): Promise<void>;
  extractCallbackCode(search?: string): { code?: string; state?: string };
  resolveLogin(params: { loginProvider: WechatLoginProvider; code: string }): Promise<ResolveLoginResult>;
  linkLogin(
    userLoginLinkToken: string,
    context?: { headers?: Record<string, string> },
  ): Promise<{ token?: string | null; userId: string; loginProvider: string }>;
  registerAndLinkLogin(params: {
    userLoginLinkToken: string;
    username: string;
    password: string;
    phoneNumber?: string;
    email?: string;
    nickname?: string;
  }): Promise<{ token?: string | null; userId: string; loginProvider: string }>;
  establishSession(options: EstablishSessionOptions): void;
  loginWithMiniProgramCode(code: string, returnUrl?: string): Promise<ResolveLoginResult>;
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    wechatAuth: WechatAuthModule;
  }
}

declare global {
  interface Window {
    WxLogin?: new (options: {
      self_redirect?: boolean;
      id: string;
      appid: string;
      scope: string;
      redirect_uri: string;
      state: string;
      style?: string;
      href?: string;
    }) => void;
  }
}
