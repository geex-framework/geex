export type WechatLoginProvider = "WechatWeb" | "WechatMiniProgram";

export interface GeexAuthenticationWechatOptions {
  readonly webAppId: string;
  readonly redirectUri: string;
  /** WxLogin container element id. Default: wechat_login_container */
  readonly containerId?: string;
  readonly style?: string;
  readonly href?: string;
}

/** @deprecated Use `GeexAuthenticationWechatOptions`. */
export type WechatAuthConfig = GeexAuthenticationWechatOptions;

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
  redirectUri?: string;
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
