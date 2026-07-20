import {
  HttpContext,
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpHeaders,
  HttpInterceptor,
  HttpRequest,
  HttpResponseBase,
} from "@angular/common/http";
import { Injectable, Injector, inject } from "@angular/core";
import { Router } from "@angular/router";
import { ALAIN_I18N_TOKEN } from "@delon/theme";
import { OAuthService } from "angular-oauth2-oidc";
import { NzModalService } from "ng-zorro-antd/modal";
import { NzNotificationService } from "ng-zorro-antd/notification";
import { Observable, of, throwError, Subject } from "rxjs";
import { catchError, finalize, mergeMap, debounceTime, distinctUntilChanged, switchMap, share } from "rxjs/operators";

import { geex } from "../geex";
import {
  GEEX_AFTER_LOGIN_NAVIGATE,
  GEEX_API_BASE_URL,
  GEEX_HTTP_STATUS_MESSAGES,
  GEEX_LOGIN_PATH,
  SILENT_REQUEST,
} from "./tokens";

/**
 * Default Geex HTTP interceptor (zh-CN messages, `/authentication/login`, tenant/Bearer headers).
 * Override via tokens or protected hooks; host may `extends` or provide callbacks.
 */
@Injectable()
export class GeexHttpInterceptor implements HttpInterceptor {
  protected injector = inject(Injector);
  protected oauthService = inject(OAuthService);
  protected modalSrv = inject(NzModalService);
  protected statusMessages = inject(GEEX_HTTP_STATUS_MESSAGES);
  protected loginPath = inject(GEEX_LOGIN_PATH);
  protected afterLoginNavigate = inject(GEEX_AFTER_LOGIN_NAVIGATE);
  protected apiBaseUrl = inject(GEEX_API_BASE_URL, { optional: true }) ?? "";

  private loginTrigger$ = new Subject<void>();
  private loginModal$: Observable<void>;

  constructor() {
    this.loginModal$ = this.loginTrigger$.pipe(
      debounceTime(100),
      distinctUntilChanged(),
      switchMap(() => {
        return new Observable<void>(subscriber => {
          const options = this.buildLoginConfirmOptions();
          const modal = this.modalSrv.confirm({
            ...options,
            nzOnOk: () => {
              this.goTo(this.loginPath);
              subscriber.next();
              subscriber.complete();
              return true;
            },
            nzOnCancel: () => {
              subscriber.next();
              subscriber.complete();
              return true;
            },
            nzClosable: true,
          });
          modal.afterClose.subscribe(() => {
            subscriber.next();
            subscriber.complete();
          });
          return () => {
            modal?.destroy();
          };
        });
      }),
      share(),
    );
    this.loginModal$.subscribe();
  }

  protected get notification(): NzNotificationService {
    return this.injector.get(NzNotificationService);
  }

  protected buildLoginConfirmOptions(): { nzTitle: string } {
    return { nzTitle: "当前登录会话已失效或超时，是否重新登录？" };
  }

  protected goTo(url: string): void {
    this.injector
      .get(Router)
      .navigateByUrl(url, { skipLocationChange: true })
      .then(() => {
        this.afterLoginNavigate();
      });
  }

  protected isSilentRequest(req: HttpRequest<any>): boolean {
    return req.context.get(SILENT_REQUEST) === true;
  }

  protected shouldAttachTenant(): boolean {
    return true;
  }

  protected notifyHttpError(status: number, text: string): void {
    this.notification.error(`请求错误 ${status}`, text, {
      nzKey: status.toString(),
    });
  }

  protected onUnauthorized(): void {
    this.loginTrigger$.next();
  }

  protected checkStatus(ev: HttpResponseBase, silent: boolean): void {
    if (silent || (ev.status >= 200 && ev.status < 300) || ev.status === 401) {
      return;
    }
    if (ev instanceof HttpErrorResponse) {
      const errorText = ev.error?.errors?.[0]?.extensions?.message || this.statusMessages[ev.status];
      this.notifyHttpError(ev.status, errorText);
    }
  }

  /** Status-branch template method; override for custom 200/403/exception routing. */
  protected handleHttpStatus(ev: HttpResponseBase, silent: boolean): void {
    switch (ev.status) {
      case 200:
        break;
      case 401:
        this.onUnauthorized();
        break;
      case 403:
      case 404:
      case 500:
        break;
      default:
        if (!silent && ev instanceof HttpErrorResponse) {
          console.warn(
            "未可知错误，大部分是由于后端不支持跨域CORS或无效配置引起，请参考 https://ng-alain.com/docs/server 解决跨域问题",
            ev,
          );
        }
        break;
    }
  }

  protected handleData(ev: HttpResponseBase, _req: HttpRequest<any>, _next: HttpHandler): Observable<any> {
    const silent = this.isSilentRequest(_req);
    this.checkStatus(ev, silent);
    this.handleHttpStatus(ev, silent);
    if (ev instanceof HttpErrorResponse) {
      return throwError(() => ev) as unknown as Observable<any>;
    }
    return of(ev);
  }

  public buildCommonHeaders(headers?: HttpHeaders): { [name: string]: string } {
    const reqHeader: { [name: string]: string } = {};
    try {
      const lang = this.injector.get(ALAIN_I18N_TOKEN, null)?.currentLang;
      if (!headers?.has("Accept-Language") && lang) {
        reqHeader["Accept-Language"] = lang;
      }
    } catch {
      /* optional i18n */
    }

    try {
      const token = this.oauthService.hasValidAccessToken() && this.oauthService.getAccessToken();
      if (token && !headers?.has("Authorization")) {
        reqHeader["Authorization"] = `Bearer ${token}`;
      }
    } catch {
      /* optional oauth */
    }

    if (this.shouldAttachTenant()) {
      try {
        const tenantCode = geex.multiTenant.current()?.code;
        if (tenantCode && !headers?.has("__tenant")) {
          reqHeader["__tenant"] = tenantCode;
        }
      } catch {
        /* no tenant module */
      }
    }

    return reqHeader;
  }

  public handleGraphQLErrors(params: {
    graphQLErrors?: ReadonlyArray<{
      message?: string;
      locations?: any;
      path?: ReadonlyArray<string | number>;
      extensions?: any;
    }>;
    operation?: any;
    response?: { data?: any } | null;
  }): void {
    const { graphQLErrors, operation, response } = params;
    if (!graphQLErrors || graphQLErrors.length === 0) return;

    const context = operation?.getContext?.() ?? {};
    if (context.silent === true) {
      return;
    }
    const httpContext = context.httpContext;
    if (httpContext instanceof HttpContext && httpContext.get(SILENT_REQUEST) === true) {
      return;
    }

    const messages = graphQLErrors
      .map(err => err?.message)
      .filter(m => !!m)
      .join("；");

    const hasNoData = !response || (response as any).data == null;
    if (hasNoData) {
      this.notification.error("请求错误 200", messages || "GraphQL 返回错误", {
        nzKey: "graphql-200-error",
      });
    } else {
      this.notification.warning("请求警告 200", messages || "GraphQL 部分错误", {
        nzKey: "graphql-200-warn",
      });
    }
  }

  public handleGraphQLNetworkError(networkError: unknown): void {
    if (!networkError) return;
    console.error((networkError as any)?.message || "网络错误, 请稍后重试, 如有疑问, 请联系管理员。");
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    let url = req.url;
    if (!url.startsWith("https://") && !url.startsWith("http://") && this.apiBaseUrl) {
      url = this.apiBaseUrl + url;
    }

    const newReq = req.clone({ url, setHeaders: this.buildCommonHeaders(req.headers) });
    return next.handle(newReq).pipe(
      mergeMap(ev => {
        if (ev instanceof HttpResponseBase) {
          return this.handleData(ev, newReq, next);
        }
        return of(ev);
      }),
      catchError((err: HttpErrorResponse) => this.handleData(err, newReq, next)),
      finalize(() => {}),
    );
  }
}
