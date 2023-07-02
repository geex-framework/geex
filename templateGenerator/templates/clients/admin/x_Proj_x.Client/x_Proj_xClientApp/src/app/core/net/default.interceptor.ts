import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpHeaders,
  HttpInterceptor,
  HttpRequest,
  HttpResponseBase,
} from "@angular/common/http";
import { Injectable, Injector } from "@angular/core";
import { Router } from "@angular/router";
import { LoadingService, LoadingType } from "@delon/abc/loading";
import { DA_SERVICE_TOKEN, ITokenService, JWTTokenModel, TokenService } from "@delon/auth";
import { ALAIN_I18N_TOKEN, _HttpClient } from "@delon/theme";
import { environment } from "@env/environment";
import { Store } from "@ngxs/store";
import { NzModalService } from "ng-zorro-antd/modal";
import { NzNotificationService } from "ng-zorro-antd/notification";
import { BehaviorSubject, Observable, of, throwError } from "rxjs";
import { catchError, filter, finalize, mergeMap, switchMap, take } from "rxjs/operators";

import { TenantState } from "../../shared/states/tenant.state";

const CODEMESSAGE: { [key: number]: string } = {
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
/**
 * 默认HTTP拦截器，其注册细节见 `app.module.ts`
 */
@Injectable()
export class DefaultInterceptor implements HttpInterceptor {
  private modalSrv: NzModalService;
  constructor(private injector: Injector, private loadingSrv: LoadingService) {
    this.modalSrv = injector.get(NzModalService);
  }

  private get notification(): NzNotificationService {
    return this.injector.get(NzNotificationService);
  }

  private goTo(url: string): void {
    this.injector
      .get(Router)
      .navigateByUrl(url, { skipLocationChange: true })
      .then(() => {
        clearHistory();
      });
  }

  private checkStatus(ev: HttpResponseBase): void {
    if ((ev.status >= 200 && ev.status < 300) || ev.status === 401) {
      return;
    }

    if (ev instanceof HttpErrorResponse) {
      const errorText = ev.error.errors[0].extensions.message || CODEMESSAGE[ev.status];
      this.notification.error(`请求错误 ${ev.status}`, errorText, {
        //加入statuscode的唯一key, 避免重复弹窗
        nzKey: ev.status.toString(),
      });
    }
  }

  // #endregion
  private toLogin(): void {
    // this.notification.error(`未登录或登录已过期，请重新登录。`, ``, {
    //   nzKey: "未登录或登录已过期，请重新登录。",
    // });
    // bug:此处采用title判断重复, 需要优化
    if (!this.modalSrv.openModals.any(x => x.getConfig().nzTitle == "当前登陆会话已失效或超时，是否重新登录？")) {
      this.modalSrv.confirm({
        nzTitle: "当前登录会话已失效或超时，是否重新登录？",
        nzOnOk: () => {
          this.goTo("/passport/login");
        },
      });
    }
  }

  private handleData(ev: HttpResponseBase, req: HttpRequest<any>, next: HttpHandler): Observable<any> {
    this.checkStatus(ev);
    // 业务处理：一些通用操作
    switch (ev.status) {
      case 200:
        // 业务层级错误处理，以下是假定restful有一套统一输出格式（指不管成功与否都有相应的数据格式）情况下进行处理
        // 例如响应内容：
        //  错误内容：{ status: 1, msg: '非法参数' }
        //  正确内容：{ status: 0, response: {  } }
        // 则以下代码片断可直接适用
        // if (ev instanceof HttpResponse) {
        //   const body = ev.body;
        //   if (body && body.status !== 0) {
        //     this.injector.get(NzMessageService).error(body.msg);
        //     // 继续抛出错误中断后续所有 Pipe、subscribe 操作，因此：
        //     // this.http.get('/').subscribe() 并不会触发
        //     return throwError({});
        //   } else {
        //     // 重新修改 `body` 内容为 `response` 内容，对于绝大多数场景已经无须再关心业务状态码
        //     return of(new HttpResponse(Object.assign(ev, { body: body.response })));
        //     // 或者依然保持完整的格式
        //     return of(ev);
        //   }
        // }
        break;
      case 401:
        this.toLogin();
        break;
      case 403:
      case 404:
      case 500:
        // if (environment.production) {
        //   this.goTo(`/exception/${ev.status}`);
        // }
        break;
      default:
        if (ev instanceof HttpErrorResponse) {
          console.warn(
            "未可知错误，大部分是由于后端不支持跨域CORS或无效配置引起，请参考 https://ng-alain.com/docs/server 解决跨域问题",
            ev,
          );
        }
        break;
    }
    if (ev instanceof HttpErrorResponse) {
      return throwError(ev);
    } else {
      return of(ev);
    }
  }

  private getAdditionalHeaders(headers?: HttpHeaders): { [name: string]: string } {
    const reqHeader: { [name: string]: string } = {};
    const lang = this.injector.get(ALAIN_I18N_TOKEN).currentLang;
    if (!headers?.has("Accept-Language") && lang) {
      reqHeader["Accept-Language"] = lang;
    }

    try {
      const token = this.injector.get<ITokenService>(DA_SERVICE_TOKEN).get<JWTTokenModel>(JWTTokenModel);
      if (token?.token && !token?.isExpired()) {
        if (!headers?.has("authorization")) {
          reqHeader["authorization"] = `Bearer ${token.token}`;
        }
      }
      try {
        const tenantCode = this.injector.get(Store).selectSnapshot(TenantState).code;
        if (tenantCode) {
          if (!headers?.has("__tenant")) {
            reqHeader["__tenant"] = tenantCode;
          }
        }
      } catch (error) {
        // 宿主不附加__tenant
      }
    } catch (error) {}

    return reqHeader;
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // 统一加上服务端前缀
    let url = req.url;
    if (!url.startsWith("https://") && !url.startsWith("http://")) {
      url = environment.api.baseUrl + url;
    }

    const newReq = req.clone({ url, setHeaders: this.getAdditionalHeaders(req.headers) });
    // this.loadingSrv.open({ type: "icon" });
    return next.handle(newReq).pipe(
      mergeMap(ev => {
        // 允许统一对请求错误处理
        if (ev instanceof HttpResponseBase) {
          return this.handleData(ev, newReq, next);
        }
        // 若一切都正常，则后续操作
        return of(ev);
      }),
      catchError((err: HttpErrorResponse) => this.handleData(err, newReq, next)),
      finalize(() => {
        this.loadingSrv.close();
      }),
    );
  }
}
