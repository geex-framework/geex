import { InjectionToken } from "@angular/core";
import type { GeexAuthenticationWechatOptions } from "./types";

export const GEEX_AUTHENTICATION_WECHAT_OPTIONS =
  new InjectionToken<Readonly<GeexAuthenticationWechatOptions>>(
    "GEEX_AUTHENTICATION_WECHAT_OPTIONS",
  );

/** @deprecated Use `GEEX_AUTHENTICATION_WECHAT_OPTIONS`. */
export const WECHAT_AUTH_CONFIG = GEEX_AUTHENTICATION_WECHAT_OPTIONS;
