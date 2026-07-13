export type WechatLoginProvider = "WechatWeb" | "WechatMiniProgram";

export interface WechatAuthConfig {
  webAppId: string;
  redirectUri: string;
  /** WxLogin container element id. Default: wechat_login_container */
  containerId?: string;
  style?: string;
  href?: string;
}

export interface ResolveExternalLoginResult {
  isLinked: boolean;
  session?: {
    token?: string | null;
    userId: string;
    loginProvider: string;
  } | null;
  accountLinkToken?: string | null;
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
