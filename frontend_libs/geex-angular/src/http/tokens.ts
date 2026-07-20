import { HttpContextToken } from "@angular/common/http";
import { InjectionToken } from "@angular/core";
import "../extensions/clear-history.extension";

/** Mark HTTP / GraphQL ops that should not show error UI. */
export const SILENT_REQUEST = new HttpContextToken<boolean>(() => false);

export const GEEX_DEFAULT_HTTP_STATUS_MESSAGES: { [key: number]: string } = {
  200: "服务器成功返回请求的数据。",
  201: "新建或修改数据成功。",
  202: "一个请求已经进入后台排队（异步任务）。",
  204: "删除数据成功。",
  400: "发出的请求有错误，服务器拒绝处理。",
  401: "用户没有权限（令牌、用户名、密码错误）。",
  403: "当前登录的用户没有对应的权限。",
  404: "请求针对的记录不存在。",
  406: "请求的格式不受支持。",
  410: "请求的资源已被永久删除。",
  422: "当创建一个对象时，发生一个验证错误。",
  500: "服务器发生错误，如有疑问，请联系管理员。",
  502: "网关错误。",
  503: "服务不可用，服务器暂时过载或维护。",
  504: "网关超时。",
};

/** Override status → message map (defaults to GEEX_DEFAULT_HTTP_STATUS_MESSAGES). */
export const GEEX_HTTP_STATUS_MESSAGES = new InjectionToken<{ [key: number]: string }>(
  "GEEX_HTTP_STATUS_MESSAGES",
  { providedIn: "root", factory: () => GEEX_DEFAULT_HTTP_STATUS_MESSAGES },
);

/** Login route after 401 (default `/authentication/login`). */
export const GEEX_LOGIN_PATH = new InjectionToken<string>("GEEX_LOGIN_PATH", {
  providedIn: "root",
  factory: () => "/authentication/login",
});

/** Called after navigating to login. Defaults to `window.clearHistory`. */
export const GEEX_AFTER_LOGIN_NAVIGATE = new InjectionToken<() => void>("GEEX_AFTER_LOGIN_NAVIGATE", {
  providedIn: "root",
  factory: () => () => window.clearHistory(),
});

/** API base URL for relative HTTP requests (host `environment.api.baseUrl`). */
export const GEEX_API_BASE_URL = new InjectionToken<string>("GEEX_API_BASE_URL");
