import * as i0 from '@angular/core';
import { signal, InjectionToken, runInInjectionContext, makeEnvironmentProviders, Injector, inject, Injectable, provideAppInitializer, importProvidersFrom, ChangeDetectorRef, effect, computed, Component, input, output, contentChild, untracked, isSignal } from '@angular/core';
import { fromEvent, Subject, Observable, throwError, of, firstValueFrom, interval, filter, map, takeUntil, timer, BehaviorSubject, isObservable, lastValueFrom, switchMap as switchMap$1 } from 'rxjs';
import { debounceTime, switchMap, distinctUntilChanged, share, mergeMap, catchError, finalize, filter as filter$1 } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpContextToken, HttpErrorResponse, HttpContext, HttpResponseBase, HTTP_INTERCEPTORS } from '@angular/common/http';
import { InMemoryCache, ApolloLink, CombinedGraphQLErrors } from '@apollo/client';
import { ErrorLink } from '@apollo/client/link/error';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { Apollo, APOLLO_OPTIONS, APOLLO_NAMED_OPTIONS, gql as gql$1 } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';
import extractFiles from 'extract-files/extractFiles.mjs';
import isExtractableFile from 'extract-files/isExtractableFile.mjs';
import { createClient } from 'graphql-ws';
import json5 from 'json5';
import { Router, ActivatedRoute, RouteConfigLoadEnd, NavigationEnd, RouteReuseStrategy, ActivatedRouteSnapshot } from '@angular/router';
import { ALAIN_I18N_TOKEN, SettingsService, MenuService, en_US, zh_CN, DelonLocaleService, ModalHelper, TitleService, AlainThemeModule } from '@delon/theme';
import { OAuthService, OAuthErrorEvent } from 'angular-oauth2-oidc';
import { NzModalService, NzModalRef } from 'ng-zorro-antd/modal';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { ACLService } from '@delon/acl';
import { CookieService, deepCopy } from '@delon/util';
import * as _ from 'lodash-es';
import { merge, flatMapDeep as flatMapDeep$1 } from 'lodash-es';
import { registerLocaleData, Location } from '@angular/common';
import ngEn from '@angular/common/locales/en';
import ngZh from '@angular/common/locales/zh';
import { TranslateService, TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { enUS, zhCN } from 'date-fns/locale';
import kiwiIntl from 'kiwi-intl';
import { en_US as en_US$1, zh_CN as zh_CN$1, NzI18nService } from 'ng-zorro-antd/i18n';
import gql from 'graphql-tag';
import { NzMessageService } from 'ng-zorro-antd/message';
import { FormBuilder, FormControl, FormGroup, AbstractControl } from '@angular/forms';
import { ReuseTabService, ReuseTabStrategy, provideReuseTabConfig } from '@delon/abc/reuse-tab';
import { LoadingService } from '@delon/abc/loading';
import { match, P } from 'ts-pattern';
import * as i1 from '@delon/abc/page-header';
import { PageHeaderModule } from '@delon/abc/page-header';
import * as i2 from '@delon/abc/st';
import { STModule, STComponent } from '@delon/abc/st';
import * as i4 from 'ng-zorro-antd/alert';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import * as i3 from 'ng-zorro-antd/card';
import { NzCardModule } from 'ng-zorro-antd/card';
import * as i5 from 'ng-zorro-antd/divider';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import * as i6 from 'ng-zorro-antd/icon';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { DelonFormModule } from '@delon/form';
import { List } from 'linqts-camelcase';
import { addYears, addMonths, addWeeks, addDays, addHours, addMinutes, addSeconds, addMilliseconds } from 'date-fns';
import SparkMD5 from 'spark-md5';

function guardedSignal(innerSignal, isInitialized) {
    const guard = (() => {
        if (!isInitialized()) {
            throw new Error(`GuardedSignal not initialized. isInitialized: ${isInitialized.toString()}`);
        }
        return innerSignal();
    });
    if ("set" in innerSignal) {
        guard.set = innerSignal.set.bind(innerSignal);
        guard.update = innerSignal.update.bind(innerSignal);
    }
    if ("asReadonly" in innerSignal) {
        guard.asReadonly = innerSignal.asReadonly.bind(innerSignal);
    }
    return guard;
}

const ExtensionModule = {};
function createUiModule(_injector) {
    const _fullScreenSignal = signal(false, ...(ngDevMode ? [{ debugName: "_fullScreenSignal" }] : []));
    const _isMobile = toSignal(fromEvent(window, "resize").pipe(debounceTime(200), switchMap(async () => window.innerHeight / window.innerWidth >= 1.5)));
    let _initialized = false;
    let _initPromise = null;
    const module = {
        fullScreen: guardedSignal(_fullScreenSignal, () => _initialized),
        isMobile: guardedSignal(_isMobile, () => _initialized),
        activeRoutedComponent: undefined,
        init: (force = false) => {
            if (force) {
                _initPromise = null;
                _initialized = false;
            }
            if (!_initPromise) {
                _initPromise = (async () => {
                    _initialized = true;
                })();
            }
            return _initPromise;
        },
    };
    return module;
}

let geex;
let Geex = new InjectionToken("Geex");
function configGeex(injector, overrides = {}, contributions = []) {
    runInInjectionContext(injector, () => {
        const modules = {
            ui: createUiModule(injector),
        };
        const moduleRecord = modules;
        for (const contribution of contributions) {
            const contributedModules = contribution.createModules({
                injector,
                modules,
            });
            for (const [name, module] of Object.entries(contributedModules)) {
                if (name === "init" || name in modules) {
                    throw new Error(`Geex module "${name}" is already registered.`);
                }
                moduleRecord[name] = module;
            }
        }
        Object.assign(modules, overrides);
        let _initPromise = null;
        modules.init ??= (force = false) => {
            if (force) {
                _initPromise = null;
            }
            if (!_initPromise) {
                _initPromise = (async () => {
                    const entries = Object.entries(modules).filter(([key]) => key !== "init");
                    return Object.fromEntries(await Promise.all(entries.map(async ([key, mod]) => {
                        const maybeInit = mod.init;
                        try {
                            return [key, await maybeInit(force)];
                        }
                        catch (err) {
                            console.error(err);
                            return [key, null];
                        }
                    })));
                })();
            }
            return _initPromise;
        };
        geex = modules;
    });
}

const GEEX_MODULE_CONTRIBUTIONS = new InjectionToken("GEEX_MODULE_CONTRIBUTIONS");
function provideGeexModuleContribution(contribution) {
    return makeEnvironmentProviders([
        {
            provide: GEEX_MODULE_CONTRIBUTIONS,
            multi: true,
            useValue: contribution,
        },
    ]);
}

function provideGeex(overrides = {}, extensions = {}) {
    return [
        {
            provide: Geex,
            useFactory: (injector) => {
                const contributions = inject(GEEX_MODULE_CONTRIBUTIONS, { optional: true }) ?? [];
                const mergedModules = {
                    ...extensions,
                    ...overrides,
                };
                configGeex(injector, mergedModules, contributions);
                return geex;
            },
            deps: [Injector],
        },
    ];
}

/**
 * Core meta-provide aligned with backend Geex.Common.
 * Installs geex signal modules. Delon page bases are opt-in via `provideGeexDelonBase()`.
 * Does not install admin business UI pages; use `geex add <name>` for source modules.
 */
function provideGeexCommon(overrides = {}, extensions = {}) {
    return provideGeex(overrides, extensions);
}

function clearHistory() {
    history.pushState(null, "", location.href);
    window.onpopstate = function () {
        history.go(1);
    };
}
window.clearHistory = clearHistory;

/** Mark HTTP / GraphQL ops that should not show error UI. */
const SILENT_REQUEST = new HttpContextToken(() => false);
const GEEX_DEFAULT_HTTP_STATUS_MESSAGES = {
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
const GEEX_HTTP_STATUS_MESSAGES = new InjectionToken("GEEX_HTTP_STATUS_MESSAGES", { providedIn: "root", factory: () => GEEX_DEFAULT_HTTP_STATUS_MESSAGES });
/** Login route after 401 (default `/authentication/login`). */
const GEEX_LOGIN_PATH = new InjectionToken("GEEX_LOGIN_PATH", {
    providedIn: "root",
    factory: () => "/authentication/login",
});
/** Called after navigating to login. Defaults to `window.clearHistory`. */
const GEEX_AFTER_LOGIN_NAVIGATE = new InjectionToken("GEEX_AFTER_LOGIN_NAVIGATE", {
    providedIn: "root",
    factory: () => () => window.clearHistory(),
});
/** API base URL for relative HTTP requests (host `environment.api.baseUrl`). */
const GEEX_API_BASE_URL = new InjectionToken("GEEX_API_BASE_URL");

/**
 * Default Geex HTTP interceptor (zh-CN messages, `/authentication/login`, tenant/Bearer headers).
 * Override via tokens or protected hooks; host may `extends` or provide callbacks.
 */
class GeexHttpInterceptor {
    injector = inject(Injector);
    oauthService = inject(OAuthService);
    modalSrv = inject(NzModalService);
    statusMessages = inject(GEEX_HTTP_STATUS_MESSAGES);
    loginPath = inject(GEEX_LOGIN_PATH);
    afterLoginNavigate = inject(GEEX_AFTER_LOGIN_NAVIGATE);
    apiBaseUrl = inject(GEEX_API_BASE_URL, { optional: true }) ?? "";
    loginTrigger$ = new Subject();
    loginModal$;
    constructor() {
        this.loginModal$ = this.loginTrigger$.pipe(debounceTime(100), distinctUntilChanged(), switchMap(() => {
            return new Observable(subscriber => {
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
        }), share());
        this.loginModal$.subscribe();
    }
    get notification() {
        return this.injector.get(NzNotificationService);
    }
    buildLoginConfirmOptions() {
        return { nzTitle: "当前登录会话已失效或超时，是否重新登录？" };
    }
    goTo(url) {
        this.injector
            .get(Router)
            .navigateByUrl(url, { skipLocationChange: true })
            .then(() => {
            this.afterLoginNavigate();
        });
    }
    isSilentRequest(req) {
        return req.context.get(SILENT_REQUEST) === true;
    }
    shouldAttachTenant() {
        return true;
    }
    notifyHttpError(status, text) {
        this.notification.error(`请求错误 ${status}`, text, {
            nzKey: status.toString(),
        });
    }
    onUnauthorized() {
        this.loginTrigger$.next();
    }
    checkStatus(ev, silent) {
        if (silent || (ev.status >= 200 && ev.status < 300) || ev.status === 401) {
            return;
        }
        if (ev instanceof HttpErrorResponse) {
            const errorText = ev.error?.errors?.[0]?.extensions?.message || this.statusMessages[ev.status];
            this.notifyHttpError(ev.status, errorText);
        }
    }
    /** Status-branch template method; override for custom 200/403/exception routing. */
    handleHttpStatus(ev, silent) {
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
                    console.warn("未可知错误，大部分是由于后端不支持跨域CORS或无效配置引起，请参考 https://ng-alain.com/docs/server 解决跨域问题", ev);
                }
                break;
        }
    }
    handleData(ev, _req, _next) {
        const silent = this.isSilentRequest(_req);
        this.checkStatus(ev, silent);
        this.handleHttpStatus(ev, silent);
        if (ev instanceof HttpErrorResponse) {
            return throwError(() => ev);
        }
        return of(ev);
    }
    buildCommonHeaders(headers) {
        const reqHeader = {};
        try {
            const lang = this.injector.get(ALAIN_I18N_TOKEN, null)?.currentLang;
            if (!headers?.has("Accept-Language") && lang) {
                reqHeader["Accept-Language"] = lang;
            }
        }
        catch {
            /* optional i18n */
        }
        try {
            const token = this.oauthService.hasValidAccessToken() && this.oauthService.getAccessToken();
            if (token && !headers?.has("Authorization")) {
                reqHeader["Authorization"] = `Bearer ${token}`;
            }
        }
        catch {
            /* optional oauth */
        }
        if (this.shouldAttachTenant()) {
            try {
                const tenantCode = geex.multiTenant.current()?.code;
                if (tenantCode && !headers?.has("__tenant")) {
                    reqHeader["__tenant"] = tenantCode;
                }
            }
            catch {
                /* no tenant module */
            }
        }
        return reqHeader;
    }
    handleGraphQLErrors(params) {
        const { graphQLErrors, operation, response } = params;
        if (!graphQLErrors || graphQLErrors.length === 0)
            return;
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
        const hasNoData = !response || response.data == null;
        if (hasNoData) {
            this.notification.error("请求错误 200", messages || "GraphQL 返回错误", {
                nzKey: "graphql-200-error",
            });
        }
        else {
            this.notification.warning("请求警告 200", messages || "GraphQL 部分错误", {
                nzKey: "graphql-200-warn",
            });
        }
    }
    handleGraphQLNetworkError(networkError) {
        if (!networkError)
            return;
        console.error(networkError?.message || "网络错误, 请稍后重试, 如有疑问, 请联系管理员。");
    }
    intercept(req, next) {
        let url = req.url;
        if (!url.startsWith("https://") && !url.startsWith("http://") && this.apiBaseUrl) {
            url = this.apiBaseUrl + url;
        }
        const newReq = req.clone({ url, setHeaders: this.buildCommonHeaders(req.headers) });
        return next.handle(newReq).pipe(mergeMap(ev => {
            if (ev instanceof HttpResponseBase) {
                return this.handleData(ev, newReq, next);
            }
            return of(ev);
        }), catchError((err) => this.handleData(err, newReq, next)), finalize(() => { }));
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexHttpInterceptor, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexHttpInterceptor });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexHttpInterceptor, decorators: [{
            type: Injectable
        }], ctorParameters: () => [] });

const GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS = new InjectionToken("GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS");
function provideGeexApolloTypePolicies(contribution) {
    return makeEnvironmentProviders([
        {
            provide: GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS,
            multi: true,
            useValue: contribution,
        },
    ]);
}
const geexApolloDefaultOptions = {
    query: {
        fetchPolicy: "network-only",
        errorPolicy: "ignore",
    },
    mutate: {
        fetchPolicy: "no-cache",
        errorPolicy: "ignore",
    },
    watchQuery: {
        fetchPolicy: "cache-first",
        errorPolicy: "ignore",
    },
};
/** Core cache policies. Feature-specific policies are registered by extensions. */
function geexDefaultTypePolicies() {
    return {
        Setting: {
            keyFields: ["name"],
        },
    };
}
function mergeTypePolicies(base, extras) {
    let merged = base;
    for (const extra of extras) {
        merged = { ...merged, ...extra };
    }
    return merged;
}
function createGeexInMemoryCache(options = {}, contributions = []) {
    const includeDefaults = options.includeDefaults !== false;
    const contributedPolicies = contributions.map(contribution => contribution());
    const extras = options.typePolicies
        ? [...contributedPolicies, options.typePolicies]
        : contributedPolicies;
    const policies = includeDefaults
        ? mergeTypePolicies(geexDefaultTypePolicies(), extras)
        : mergeTypePolicies({}, extras);
    return new InMemoryCache({
        typePolicies: policies,
        possibleTypes: options.possibleTypes,
    });
}
function createGeexUriLink(baseUrl) {
    return new ApolloLink((operation, forward) => {
        const variables = Object.entries(operation.variables).filter(([, v]) => v != undefined);
        if (variables.length > 0) {
            operation.setContext(() => {
                const encodedParams = variables.map(([k, v]) => `${k}=${json5.stringify(v)}`).join("&");
                return {
                    uri: new URL(`/graphql/${operation.operationName}?${encodedParams}`, baseUrl).toString().substring(0, 2047),
                };
            });
        }
        else {
            operation.setContext(() => ({
                uri: new URL(`/graphql/${operation.operationName}`, baseUrl).toString().substring(0, 2047),
            }));
        }
        return forward(operation);
    });
}
function createGeexHttpApolloOptions(options) {
    const uriLink = createGeexUriLink(options.baseUrl);
    const links = [...(options.extraLinks ?? []), uriLink, options.httpLinkInstance];
    return {
        link: ApolloLink.from(links),
        cache: options.cache,
        defaultOptions: geexApolloDefaultOptions,
    };
}
function isGeexSilentOperation(operation, silentToken = SILENT_REQUEST) {
    const context = operation.getContext() ?? {};
    if (context["silent"] === true) {
        return true;
    }
    const httpContext = context["httpContext"];
    return httpContext instanceof HttpContext && httpContext.get(silentToken) === true;
}
function createGeexGraphqlErrorLink(handler) {
    return new ErrorLink(({ error, result, operation }) => {
        if (isGeexSilentOperation(operation)) {
            return;
        }
        if (CombinedGraphQLErrors.is(error)) {
            handler.handleGraphQLErrors({
                graphQLErrors: error.errors,
                operation,
                response: result,
            });
        }
        else if (error) {
            handler.handleGraphQLNetworkError(error);
        }
    });
}
function createGeexSilentContextLink(silentToken = SILENT_REQUEST) {
    return new ApolloLink((operation, forward) => {
        operation.setContext(context => {
            const prevHttpContext = context.httpContext instanceof HttpContext ? context.httpContext : new HttpContext();
            return {
                silent: true,
                httpContext: prevHttpContext.set(silentToken, true),
            };
        });
        return forward(operation);
    });
}
function createGeexWsApolloOptions(options) {
    const url = options.url ??
        new URL("/graphql", (options.baseUrl ?? "").replace(/^http/, "ws")).toString();
    const client = createClient({
        url,
        lazy: true,
        retryAttempts: options.retryAttempts ?? 3,
        connectionParams: options.connectionParams,
        on: {
            opened: options.onOpened ?? (() => console.log("ws connected.")),
            error: options.onError ?? ((err) => console.error("ws connect failed.", err)),
        },
    });
    return {
        link: new GraphQLWsLink(client),
        cache: options.cache,
        defaultOptions: geexApolloDefaultOptions,
    };
}
/**
 * HttpLink with multipart upload support.
 * Uses peer `extract-files` by default; overrides via options.
 */
function createGeexUploadHttpLink(httpLink, options) {
    const extractFilesFn = options?.extractFilesFn ?? extractFiles;
    const isExtractable = options?.isExtractableFile ?? isExtractableFile;
    return httpLink.create({
        withCredentials: options?.withCredentials ?? true,
        extractFiles: body => extractFilesFn(body, isExtractable),
    });
}
const SilentApollo = new InjectionToken("silent_apollo");
const GEEX_APOLLO_CACHE = new InjectionToken("GEEX_APOLLO_CACHE");
function provideGeexApollo(options) {
    const createHttp = (httpLink) => {
        if (options.createHttpLinkInstance) {
            return options.createHttpLinkInstance(httpLink);
        }
        if (options.enableUpload === false) {
            return httpLink.create({ withCredentials: true });
        }
        return createGeexUploadHttpLink(httpLink);
    };
    return [
        Apollo,
        {
            provide: GEEX_APOLLO_CACHE,
            useFactory: () => createGeexInMemoryCache({
                possibleTypes: options.possibleTypes,
                typePolicies: options.typePolicies,
                includeDefaults: options.includeDefaultTypePolicies !== false,
            }, inject(GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS, { optional: true }) ?? []),
        },
        {
            provide: APOLLO_OPTIONS,
            useFactory: (cache, httpLink, interceptor) => {
                const handler = options.errorHandler ?? interceptor;
                return createGeexHttpApolloOptions({
                    baseUrl: options.baseUrl,
                    httpLinkInstance: createHttp(httpLink),
                    cache,
                    extraLinks: [createGeexGraphqlErrorLink(handler)],
                });
            },
            deps: [GEEX_APOLLO_CACHE, HttpLink, GeexHttpInterceptor],
        },
        {
            provide: APOLLO_NAMED_OPTIONS,
            useFactory: (cache, httpLink, interceptor) => {
                const handler = options.errorHandler ?? interceptor;
                return {
                    subscription: createGeexWsApolloOptions({
                        baseUrl: options.baseUrl,
                        cache,
                        connectionParams: async () => handler.buildCommonHeaders?.() ?? interceptor.buildCommonHeaders(),
                    }),
                    silent: createGeexHttpApolloOptions({
                        baseUrl: options.baseUrl,
                        httpLinkInstance: createHttp(httpLink),
                        cache,
                        extraLinks: [createGeexSilentContextLink()],
                    }),
                };
            },
            deps: [GEEX_APOLLO_CACHE, HttpLink, GeexHttpInterceptor],
        },
        {
            provide: SilentApollo,
            useFactory: (apollo) => apollo.use("silent"),
            deps: [Apollo],
        },
    ];
}

const GEEX_STARTUP_OPTIONS = new InjectionToken("GEEX_STARTUP_OPTIONS");
const GEEX_EXCEPTION_500_PATH = new InjectionToken("GEEX_EXCEPTION_500_PATH", {
    providedIn: "root",
    factory: () => "/exception/500",
});
const GEEX_SESSION_TERMINATED_COPY = new InjectionToken("GEEX_SESSION_TERMINATED_COPY", {
    providedIn: "root",
    factory: () => ({}),
});

/** When true, DebuggerBlockerService activates anti-devtools measures. */
const GEEX_BLOCK_DEBUGGER = new InjectionToken("GEEX_BLOCK_DEBUGGER", {
    providedIn: "root",
    factory: () => false,
});

class DebuggerBlockerService {
    enabled = inject(GEEX_BLOCK_DEBUGGER, { optional: true }) ?? false;
    init() {
        if (!this.enabled) {
            return;
        }
        this.blockDebugger();
        this.disableDevToolsShortcuts();
    }
    blockDebugger() {
        setInterval(() => {
            eval(`
        if (window.outerHeight - window.innerHeight > 160 || window.outerWidth - window.innerWidth > 160) {
          alert("Debugger detected! Please close dev tools to continue.")
          location.reload();
        }
        `);
        }, 1000);
    }
    disableDevToolsShortcuts() {
        document.addEventListener("keydown", e => {
            if (e.key === "F12") {
                e.preventDefault();
                e.stopPropagation();
                return;
            }
            if (e.ctrlKey && e.shiftKey && ["I", "J", "C"].includes(e.key)) {
                e.preventDefault();
                e.stopPropagation();
            }
        }, true);
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: DebuggerBlockerService, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: DebuggerBlockerService, providedIn: "root" });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: DebuggerBlockerService, decorators: [{
            type: Injectable,
            args: [{
                    providedIn: "root",
                }]
        }] });

const GEEX_MENU_CONTRIBUTIONS = new InjectionToken("GEEX_MENU_CONTRIBUTIONS");
/** Host-composed default menus (e.g. from module-registry). */
const GEEX_DEFAULT_MENUS = new InjectionToken("GEEX_DEFAULT_MENUS", {
    providedIn: "root",
    factory: () => [],
});
function provideGeexMenus(menus) {
    return [{ provide: GEEX_DEFAULT_MENUS, useValue: menus }];
}

/**
 * Host provides AlainI18NService-compatible instance (e.g. `GeexI18nService`).
 */
const GEEX_I18N_SERVICE = new InjectionToken("GEEX_I18N_SERVICE");
/**
 * Typed kiwi/i18n dictionary (augment `GeexI18n` in the host app).
 */
const GEEX_I18N = new InjectionToken("GEEX_I18N");
/**
 * Typed AppPermission enum/map (augment `GeexAppPermission` in the host app).
 */
const GEEX_APP_PERMISSION = new InjectionToken("GEEX_APP_PERMISSION");

/** Per-language ngx-translate dictionaries keyed by locale code (e.g. `zh-cn`). */
const GEEX_I18N_PACKS = new InjectionToken("GEEX_I18N_PACKS");
/** Well-known setting names for post-login localization (aligned with SettingDefinition). */
const GEEX_LOCALIZATION_DATA_SETTING = "LocalizationData";
const GEEX_LOCALIZATION_LANGUAGE_SETTING = "LocalizationLanguage";

/** Well-known setting names for post-login app/menu bind (aligned with SettingDefinition). */
const GEEX_APP_NAME_SETTING = "AppAppName";
const GEEX_APP_MENU_SETTING = "AppAppMenu";

const GEEX_SUPER_ADMIN_USER_ID = new InjectionToken("GEEX_SUPER_ADMIN_USER_ID", {
    providedIn: "root",
    factory: () => "000000000000000000000001",
});

/**
 * Single bootstrap entry for app session.
 *
 * Linear flow:
 * 1. configure OAuth
 * 2. if OIDC callback code present → tryLogin (once)
 * 3. geex.init()
 * 4. bind Delon user / ACL / menus
 * 5. start session watch (once)
 *
 * Login pages must not call tryLogin/load for OIDC callbacks.
 * Password / WeChat token handoff uses initCodeFlow → IdP → this bootstrap again.
 */
class GeexStartupService {
    options = inject(GEEX_STARTUP_OPTIONS);
    injector = inject(Injector);
    geex = inject(Geex);
    oAuthService = inject(OAuthService);
    aclService = inject(ACLService);
    settingsService = inject(SettingsService);
    router = inject(Router);
    modalService = inject(NzModalService);
    menuService = inject(MenuService);
    loginPath = inject(GEEX_LOGIN_PATH);
    afterLoginNavigate = inject(GEEX_AFTER_LOGIN_NAVIGATE);
    superAdminUserId = inject(GEEX_SUPER_ADMIN_USER_ID);
    exception500Url = inject(GEEX_EXCEPTION_500_PATH);
    sessionTerminatedCopy = inject(GEEX_SESSION_TERMINATED_COPY);
    defaultMenus = inject(GEEX_DEFAULT_MENUS);
    debuggerBlocker = inject(DebuggerBlockerService);
    bootstrapPromise = null;
    bootstrapped = false;
    sessionWatchStarted = false;
    /** APP_INITIALIZER entry. Safe to call concurrently; runs the bootstrap pipeline once. */
    async load() {
        if (this.bootstrapped) {
            return;
        }
        if (this.bootstrapPromise) {
            return this.bootstrapPromise;
        }
        this.bootstrapPromise = this.bootstrap().finally(() => {
            this.bootstrapPromise = null;
        });
        return this.bootstrapPromise;
    }
    async bootstrap() {
        try {
            this.debuggerBlocker.init();
            this.oAuthService.configure(this.options.oauth.getConfig());
            await this.trySwitchTenant();
            await this.tryAutoOAuthLogin();
            await this.tryOidcCodeCallback();
            this.ensureSessionWatch();
            await this.geex.init();
            await this.bindUiSession();
            this.bootstrapped = true;
        }
        catch (error) {
            await this.router.navigateByUrl(this.exception500Url);
            console.error(error);
        }
    }
    async tryOidcCodeCallback() {
        const url = new URL(location.href);
        const code = url.searchParams.get("code");
        if (!code) {
            return;
        }
        const state = url.searchParams.get("state") ?? "";
        // WeChat QR callback uses the same ?code= param but is handled on the login page first.
        if (state === "WechatWeb" || state.startsWith("WechatWeb")) {
            return;
        }
        // Discovery aligns issuer/jwks before tryLogin (id_token validation). tokenEndpoint alone is not enough.
        try {
            await this.oAuthService.loadDiscoveryDocument();
        }
        catch (err) {
            console.error(err);
        }
        await this.ensureOAuthTokenEndpoint();
        await this.oAuthService.tryLogin();
    }
    /** Fill tokenEndpoint gaps after discovery (or when discovery is unreachable). */
    async ensureOAuthTokenEndpoint() {
        if (this.oAuthService.tokenEndpoint) {
            return;
        }
        const issuer = this.oAuthService.issuer?.replace(/\/?$/, "/");
        if (issuer) {
            this.oAuthService.tokenEndpoint = `${issuer}idsvr/token`;
            return;
        }
        throw new Error("OAuth tokenEndpoint is not configured. Set AuthConfig.tokenEndpoint (e.g. {issuer}/idsvr/token) or make OIDC discovery reachable.");
    }
    async bindUiSession() {
        if (!this.oAuthService.hasValidAccessToken()) {
            return;
        }
        const user = await this.resolveAuthUser();
        if (!user) {
            // Token without federateAuthenticate user is not a completed Geex login.
            console.error("bindUiSession: access token present but geex.authentication.user() missing after federateAuthenticate");
            this.oAuthService.logOut(true);
            return;
        }
        this.settingsService.setUser({
            avatar: user.avatarFile?.url,
            id: user.id,
            phoneNumber: user.phoneNumber,
            email: user.email,
            username: user.username,
            roleName: user.roleNames,
        });
        const adminId = this.superAdminUserId;
        if (user.id == adminId) {
            this.aclService.setFull(true);
        }
        else {
            this.aclService.setRole(user.permissions);
        }
        const settingsModule = this.geex["settings"];
        const settings = settingsModule?.settings?.() ?? [];
        if (!settings.length) {
            return;
        }
        const appName = settings.find(x => x?.name == GEEX_APP_NAME_SETTING)?.value;
        if (appName) {
            this.settingsService.setApp({ name: appName });
        }
        let menus = this.defaultMenus.map(menu => ({
            ...menu,
            children: menu.children ? [...menu.children] : menu.children,
        }));
        const settingMenus = settings.find(x => x?.name == GEEX_APP_MENU_SETTING)?.value;
        if (Array.isArray(settingMenus) && settingMenus.length) {
            menus = settingMenus.map(menu => ({
                ...menu,
                children: menu.children ? [...menu.children] : menu.children,
            }));
        }
        const contributions = this.injector.get(GEEX_MENU_CONTRIBUTIONS, []);
        const contributedGroups = [];
        for (const contribution of contributions) {
            for (const item of (await contribution.resolve(user))) {
                if (item.group === true) {
                    contributedGroups.push(item);
                    continue;
                }
                // Leaf items must stay under a group. Delon top-level `group !== false` renders
                // as a non-clickable title without icons; never promote bare leaves to top-level.
                const leaf = {
                    ...item,
                    group: false,
                    children: Array.isArray(item.children) ? item.children : [],
                };
                const existing = leaf.link ? this.findMenuByLink(menus, leaf.link) : undefined;
                if (existing) {
                    Object.assign(existing, leaf, { hide: leaf.hide ?? false, group: false });
                    continue;
                }
                const systemConfigGroup = this.findSystemConfigGroup(menus);
                if (systemConfigGroup) {
                    systemConfigGroup.children = [...(systemConfigGroup.children ?? []), leaf];
                }
                else {
                    contributedGroups.push({
                        group: true,
                        hideInBreadcrumb: true,
                        open: true,
                        text: "系统及配置",
                        i18n: "Common.menu.systemConfig",
                        children: [leaf],
                    });
                }
            }
        }
        this.menuService.add([...menus, ...contributedGroups]);
        this.menuService.resume();
        const i18n = this.resolveI18nAdapter();
        i18n?.merge(settings.find(x => x?.name == GEEX_LOCALIZATION_DATA_SETTING)?.value);
        const backendLang = settings.find(x => x?.name == GEEX_LOCALIZATION_LANGUAGE_SETTING)?.value;
        if (backendLang) {
            this.settingsService.setLayout("lang", backendLang);
            i18n?.use(backendLang);
        }
        else {
            const cachedLang = this.settingsService.layout.lang;
            if (cachedLang) {
                i18n?.use(cachedLang);
            }
        }
    }
    async tryAutoOAuthLogin() {
        const url = new URL(location.href);
        const autoLogin = url.searchParams.get("_autoLogin");
        if (autoLogin) {
            url.searchParams.delete("_autoLogin");
            this.oAuthService.redirectUri = url.href;
            this.oAuthService.initCodeFlow();
            throw new Error("starting auto login");
        }
    }
    findMenuByLink(menus, link) {
        for (const menu of menus) {
            if (menu.link === link) {
                return menu;
            }
            if (menu.children?.length) {
                const found = this.findMenuByLink(menu.children, link);
                if (found) {
                    return found;
                }
            }
        }
        return undefined;
    }
    findSystemConfigGroup(menus) {
        return menus.find(m => m.i18n === "Common.menu.systemConfig" ||
            m.i18n === "menu.systemConfig" ||
            m.text === "系统及配置" ||
            m.children?.some(c => c.link === "/settings" ||
                c.link === "/tenant" ||
                c.link === "/blob-storage" ||
                c.link === "/mocking" ||
                c.i18n === "Common.menu.settings" ||
                c.i18n === "Common.menu.tenant" ||
                c.i18n === "Mocking.title"));
    }
    resolveI18nAdapter() {
        const service = this.injector.get(GEEX_I18N_SERVICE, null);
        if (service && typeof service.merge === "function" && typeof service.use === "function") {
            return service;
        }
        return null;
    }
    /** Safe read: guardedSignal throws before auth.init finishes. */
    readAuthUser() {
        try {
            return this.geex.authentication.user() ?? undefined;
        }
        catch {
            return undefined;
        }
    }
    /**
     * After OIDC, auth.init may have finished with a null user (token race) or still be settling.
     * Never throw EmptyError into bootstrap (that becomes the 500 page).
     */
    async resolveAuthUser() {
        let user = this.readAuthUser();
        if (user) {
            return user;
        }
        const auth = this.geex.authentication;
        if (typeof auth.reload === "function") {
            await auth.reload();
            user = this.readAuthUser();
            if (user) {
                return user;
            }
        }
        return firstValueFrom(interval(100).pipe(filter(() => this.readAuthUser() != undefined), map(() => this.readAuthUser()), takeUntil(timer(5000))), { defaultValue: undefined });
    }
    async trySwitchTenant() {
        const url = new URL(location.href);
        const targetTenantCode = url.searchParams.get("__tenant");
        url.searchParams.delete("__tenant");
        if (!targetTenantCode) {
            return;
        }
        const currentTenantCode = this.injector.get(CookieService).get("__tenant");
        if (targetTenantCode == currentTenantCode) {
            await this.router.navigateByUrl(url.pathname + url.search + url.hash);
            return;
        }
        this.geex.multiTenant.switchTenant(targetTenantCode);
        await this.router.navigateByUrl(url.pathname + url.search + url.hash);
    }
    ensureSessionWatch() {
        if (this.sessionWatchStarted) {
            return;
        }
        this.sessionWatchStarted = true;
        this.oAuthService.setupAutomaticSilentRefresh();
        this.oAuthService["initSessionCheck"]();
        const loginUrl = this.loginPath;
        const modalCopy = this.sessionTerminatedCopy;
        this.oAuthService.events.subscribe(e => {
            if (e instanceof OAuthErrorEvent && e.reason?.status == 401) {
                this.oAuthService.logOut(true);
            }
            if (e.type == "session_terminated") {
                console.error(e);
                this.modalService.info({
                    nzTitle: modalCopy.title ?? "检测到账号切换, 请重新登入",
                    nzOkText: modalCopy.okText ?? "确认",
                    nzOnOk: async () => {
                        this.settingsService.setUser({});
                        this.aclService.set({});
                        await this.router.navigateByUrl(loginUrl).then(() => {
                            this.afterLoginNavigate();
                        });
                    },
                    nzClosable: false,
                });
            }
        });
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexStartupService, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexStartupService });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexStartupService, decorators: [{
            type: Injectable
        }] });

function provideGeexStartup(options) {
    return [
        { provide: GEEX_STARTUP_OPTIONS, useValue: options },
        { provide: GEEX_BLOCK_DEBUGGER, useValue: options.blockDebugger ?? false },
        DebuggerBlockerService,
        GeexStartupService,
        provideAppInitializer(() => inject(GeexStartupService).load()),
    ];
}

function mergeGeexI18nPacks(base, ...overlays) {
    return merge({}, base, ...overlays);
}

class GeexTranslateLoader {
    packs = inject(GEEX_I18N_PACKS, { optional: true });
    getTranslation(lang) {
        if (this.packs?.[lang]) {
            return of(this.packs[lang]);
        }
        return of({});
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexTranslateLoader, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexTranslateLoader, providedIn: "root" });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexTranslateLoader, decorators: [{
            type: Injectable,
            args: [{ providedIn: "root" }]
        }] });

const DEFAULT = "zh-cn";
const LANGS = {
    "zh-cn": {
        text: "简体中文",
        ng: ngZh,
        zorro: zh_CN$1,
        date: zhCN,
        delon: zh_CN,
        abbr: "🇨🇳",
    },
    "en-us": {
        text: "English",
        ng: ngEn,
        zorro: en_US$1,
        date: enUS,
        delon: en_US,
        abbr: "🇺🇸",
    },
};
/** Mutable kiwi dictionary; host may re-export as `I18N`. */
let I18N;
function attachGetter([key, value]) {
    const parentKey = key;
    if (value instanceof Object) {
        const langObj = value;
        Object.entries(value).forEach(([childKey, childValue]) => attachGetter([`${key}.${childKey}`, childValue]));
        langObj.get = function (childKey, notFoundValue) {
            const result = this[childKey];
            if (result != undefined) {
                return result;
            }
            if (notFoundValue != undefined || notFoundValue != null) {
                return notFoundValue;
            }
            return `${parentKey}.${childKey}`;
        }.bind(value);
    }
    return [];
}
function attachGettersToPacks(packs) {
    Object.entries(packs).forEach(([, pack]) => {
        flatMapDeep$1(Object.entries(pack), ([key, value]) => attachGetter([`I18N.${key}`, value]));
    });
}
/**
 * Alain + kiwi i18n runtime. Packs come from `GEEX_I18N_PACKS` (host zh-CN/en-US assembly).
 */
class GeexI18nService {
    _default = DEFAULT;
    change$ = new BehaviorSubject(null);
    kiwiLangs;
    _langs = Object.keys(LANGS).map(code => {
        const item = LANGS[code];
        return { code, text: item.text, abbr: item.abbr };
    });
    settings = inject(SettingsService);
    nzI18nService = inject(NzI18nService);
    delonLocaleService = inject(DelonLocaleService);
    translate = inject(TranslateService);
    packs = inject(GEEX_I18N_PACKS);
    constructor() {
        this.kiwiLangs = this.packs ?? {};
        attachGettersToPacks(this.kiwiLangs);
        I18N = kiwiIntl.init(DEFAULT, this.kiwiLangs);
        const lans = this._langs.map(item => item.code);
        this.translate.addLangs(lans);
        const defaultLan = this.getDefaultLang().toLowerCase();
        this._default = lans.includes(defaultLan) ? defaultLan : DEFAULT;
        this.use(this._default);
    }
    /** Current kiwi dictionary (also mirrored by module `I18N`). */
    get dictionary() {
        return I18N;
    }
    getDefaultLang() {
        if (this.settings.layout.lang) {
            return this.settings.layout.lang;
        }
        return (navigator.languages?.[0] || navigator.language || DEFAULT).toLowerCase();
    }
    updateLangData(lang) {
        const item = LANGS[lang.toLocaleLowerCase()] ?? LANGS[DEFAULT];
        registerLocaleData(item.ng);
        this.nzI18nService.setLocale(item.zorro);
        this.nzI18nService.setDateLocale(item.date);
        this.delonLocaleService.setLocale(item.delon);
        I18N = kiwiIntl.init(lang, this.kiwiLangs);
    }
    get change() {
        return this.change$.asObservable().pipe(filter$1(w => w != null));
    }
    merge(translations) {
        merge(this.kiwiLangs, translations);
    }
    use(lang) {
        lang = lang || this.translate.getDefaultLang() || this._default;
        if (this.currentLang === lang) {
            return;
        }
        this.updateLangData(lang);
        this.translate.use(lang).subscribe(() => this.change$.next(lang));
    }
    getLangs() {
        return this._langs;
    }
    fanyi(key, interpolateParams) {
        const result = this.translate.instant(key, interpolateParams);
        if (key == result) {
            return `I18N.${key}`;
        }
        return result;
    }
    get defaultLang() {
        return this._default;
    }
    get currentLang() {
        return this.translate.currentLang || this.translate.getDefaultLang() || this._default;
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexI18nService, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexI18nService });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexI18nService, decorators: [{
            type: Injectable
        }], ctorParameters: () => [] });

/**
 * Stable DI surface for `GEEX_I18N` that tracks module `I18N` reassignments
 * without depending on `GeexI18nService` (avoids TranslateService cycle).
 */
function createGeexI18nDictionaryProxy() {
    const proxy = new Proxy({}, {
        get(_target, prop, receiver) {
            const dict = I18N;
            if (dict == null) {
                return undefined;
            }
            const value = Reflect.get(dict, prop, receiver);
            if (typeof value === "function") {
                return value.bind(dict);
            }
            return value;
        },
    });
    return proxy;
}

/**
 * Register kiwi packs + GeexI18nService + Alain/ngx-translate wiring.
 */
function provideGeexI18n(packs, options = {}) {
    return [
        { provide: GEEX_I18N_PACKS, useValue: packs },
        GeexI18nService,
        { provide: GEEX_I18N_SERVICE, useExisting: GeexI18nService },
        {
            provide: GEEX_I18N,
            useFactory: () => createGeexI18nDictionaryProxy(),
        },
        { provide: ALAIN_I18N_TOKEN, useExisting: GeexI18nService },
        importProvidersFrom(TranslateModule.forRoot({
            loader: {
                provide: TranslateLoader,
                useClass: GeexTranslateLoader,
            },
            fallbackLang: options.fallbackLang ?? "en",
        })),
    ];
}

const GEEX_CANCEL_AUTHENTICATION_DOCUMENT = new InjectionToken("GEEX_CANCEL_AUTHENTICATION_DOCUMENT");
/** Header profile route (default `/identity/me`). */
const GEEX_PROFILE_PATH = new InjectionToken("GEEX_PROFILE_PATH", {
    providedIn: "root",
    factory: () => "/identity/me",
});
/** Header profile menu label (default 个人中心). */
const GEEX_PROFILE_LABEL = new InjectionToken("GEEX_PROFILE_LABEL", {
    providedIn: "root",
    factory: () => "个人中心",
});

const cancelAuthenticationMutation = gql `
  mutation cancelAuthenticate {
    cancelAuthentication
  }
`;
class GeexAuthLogout {
    apollo = inject(Apollo);
    oauth = inject(OAuthService);
    settings = inject(SettingsService);
    acl = inject(ACLService);
    router = inject(Router);
    loginPath = inject(GEEX_LOGIN_PATH);
    afterLoginNavigate = inject(GEEX_AFTER_LOGIN_NAVIGATE);
    cancelDocument = inject(GEEX_CANCEL_AUTHENTICATION_DOCUMENT, { optional: true });
    async logout() {
        const mutation = this.cancelDocument ?? cancelAuthenticationMutation;
        await firstValueFrom(this.apollo.mutate({ mutation }));
        this.settings.setUser({});
        this.acl.set({});
        this.oauth.logOut();
        await this.router.navigateByUrl(this.loginPath).then(() => {
            this.afterLoginNavigate();
        });
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexAuthLogout, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexAuthLogout, providedIn: "root" });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexAuthLogout, decorators: [{
            type: Injectable,
            args: [{ providedIn: "root" }]
        }] });

function setByPath(target, path, value) {
    const parts = path.split(".");
    let cur = target;
    for (let i = 0; i < parts.length - 1; i++) {
        const key = parts[i];
        const next = cur[key];
        if (next == null || typeof next !== "object") {
            cur[key] = {};
        }
        cur = cur[key];
    }
    cur[parts[parts.length - 1]] = value;
}
/** Deep-set env from flat / dotted override keys. Host may pass typed `environment` objects. */
function applyEnvironmentOverrides(env, override) {
    const target = env;
    for (const [key, value] of Object.entries(override)) {
        if (key.includes(".")) {
            setByPath(target, key, value);
        }
        else {
            target[key] = value;
        }
    }
}
/**
 * Loads `/assets/environment.override.js` (or options.url) and merges into env.
 * Missing / invalid override is non-fatal.
 */
async function loadEnvironmentOverrides(env, options = {}) {
    const url = options.url ?? "/assets/environment.override.js";
    try {
        const mod = await import(/* @vite-ignore */ /* webpackIgnore: true */ url);
        const override = (mod.default ?? mod);
        if (override && typeof override === "object") {
            applyEnvironmentOverrides(env, override);
            return;
        }
        if (options.onInvalid) {
            options.onInvalid(override, url);
        }
        else {
            console.warn(`[environment] Invalid override export from ${url}, expected object. Using defaults.`, override);
        }
    }
    catch (error) {
        if (options.onLoadError) {
            options.onLoadError(error, url);
        }
        else {
            console.warn(`[environment] Failed to load ${url}, using defaults.`, error);
        }
    }
}

function provideGeexHttp(options) {
    return [
        { provide: GEEX_API_BASE_URL, useValue: options.apiBaseUrl },
        GeexHttpInterceptor,
        { provide: HTTP_INTERCEPTORS, useExisting: GeexHttpInterceptor, multi: true },
    ];
}

class BusinessComponentBase {
    acl = inject(ACLService);
    apollo = inject(Apollo);
    i18n = inject(GEEX_I18N_SERVICE, { optional: true });
    modal = inject(ModalHelper);
    msgSrv = inject(NzMessageService);
    nzModalSrv = inject(NzModalService);
    router = inject(Router);
    params;
    I18N = inject(GEEX_I18N);
    AppPermission = inject(GEEX_APP_PERMISSION);
    can(permission) {
        return this.acl.can(permission);
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: BusinessComponentBase, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: BusinessComponentBase });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: BusinessComponentBase, decorators: [{
            type: Injectable
        }] });

// @ts-nocheck
/* eslint-disable */
// Converted from UMD rison.js for ng-packagr bundling
var risonRegex = /^\s*(?:\([^()]*:[^()]*\)|!\([^()]*\)|!t|!f|!n|-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE]\d+)?|'(?:[^'!]|!(?:'|!))*'|[A-Za-z0-9_./~-]+)\s*$/;
const exports$1 = {};
var rison = exports$1;
//////////////////////////////////////////////////
//
//  the stringifier is based on
//    http://json.org/json.js as of 2006-04-28 from json.org
//  the parser is based on 
//    http://osteele.com/sources/openlaszlo/json
//
if (typeof rison == 'undefined')
    window.rison = {};
/**
*  rules for an uri encoder that is more tolerant than encodeURIComponent
*
*  encodeURIComponent passes  ~!*()-_.'
*
*  we also allow              ,:@$/
*
*/
rison.uri_ok = {
    '~': true, '!': true, '*': true, '(': true, ')': true,
    '-': true, '_': true, '.': true, ',': true,
    ':': true, '@': true, '$': true,
    "\"": true, '/': true
};
/*
* we divide the uri-safe glyphs into three sets
*   <rison> - used by rison                         ' ! : ( ) ,
*   <reserved> - not common in strings, reserved    * @ $ & ; =
*
* we define <identifier> as anything that's not forbidden
*/
/**
* punctuation characters that are legal inside ids.
*/
// this var isn't actually used
//rison.idchar_punctuation = "_-./~";  
(function () {
    var l = [];
    for (var hi = 0; hi < 16; hi++) {
        for (var lo = 0; lo < 16; lo++) {
            if (hi + lo == 0)
                continue;
            var c = String.fromCharCode(hi * 16 + lo);
            if (!/\w|[-_.\/~]/.test(c))
                l.push('\\u00' + hi.toString(16) + lo.toString(16));
        }
    }
    /**
     * characters that are illegal inside ids.
     * <rison> and <reserved> classes are illegal in ids.
     *
     */
    rison.not_idchar = l.join("");
    //idcrx = new RegExp('[' + rison.not_idchar + ']');
    //console.log('NOT', (idcrx.test(' ')) );
})();
//rison.not_idchar  = " \t\r\n\"<>[]{}'!=:(),*@$;&";
rison.not_idchar = " '!:(),*@$";
/**
* characters that are illegal as the start of an id
* this is so ids can't look like numbers.
*/
rison.not_idstart = "-0123456789";
(function () {
    var idrx = '[^' + rison.not_idstart + rison.not_idchar +
        '][^' + rison.not_idchar + ']*';
    rison.id_ok = new RegExp('^' + idrx + '$');
    // regexp to find the end of an id when parsing
    // g flag on the regexp is necessary for iterative regexp.exec()
    rison.next_id = new RegExp(idrx, 'g');
})();
/**
* this is like encodeURIComponent() but quotes fewer characters.
*
* @see rison.uri_ok
*
* encodeURIComponent passes   ~!*()-_.'
* rison.quote also passes   ,:@$/
*   and quotes " " as "+" instead of "%20"
*/
rison.quote = function (x) {
    if (/^[-A-Za-z0-9~!*()_.",:@$\/]*$/.test(x))
        return x;
    return encodeURIComponent(x)
        .replace('%2C', ',', 'g')
        .replace('%3A', ':', 'g')
        .replace('%40', '@', 'g')
        .replace('%24', '$', 'g')
        .replace('%2F', '/', 'g')
        .replace('%20', '+', 'g');
};
//
//  based on json.js 2006-04-28 from json.org
//  license: http://www.json.org/license.html
//
//  hacked by nix for use in uris.
//
(function () {
    var sq = {
        "\"": true, '!': true
    }, s = {
        array: function (x) {
            var a = ['!('], b, f, i, l = x.length, v;
            for (i = 0; i < l; i += 1) {
                v = x[i];
                f = s[typeof v];
                if (f) {
                    v = f(v);
                    if (typeof v == 'string') {
                        if (b) {
                            a[a.length] = ',';
                        }
                        a[a.length] = v;
                        b = true;
                    }
                }
            }
            a[a.length] = ')';
            return a.join("");
        },
        'boolean': function (x) {
            if (x)
                return '!t';
            return '!f';
        },
        'null': function (x) {
            return "!n";
        },
        number: function (x) {
            if (!isFinite(x))
                return '!n';
            // strip '+' out of exponent, '-' is ok though
            return String(x).replace(/\+/, "");
        },
        object: function (x) {
            if (x) {
                if (x instanceof Array) {
                    return s.array(x);
                }
                if (x instanceof Date) {
                    return `!d"${x.toISOString()}"`;
                }
                // WILL: will this work on non-Firefox browsers?
                if (typeof x.__prototype__ === 'object' && typeof x.__prototype__.encode_rison !== 'undefined')
                    return x.encode_rison();
                var a = ['('], b, f, i, v, ki, ks = [];
                for (i in x)
                    ks[ks.length] = i;
                ks.sort();
                for (ki = 0; ki < ks.length; ki++) {
                    i = ks[ki];
                    v = x[i];
                    f = s[typeof v];
                    if (f) {
                        v = f(v);
                        if (typeof v == 'string') {
                            if (b) {
                                a[a.length] = ',';
                            }
                            a.push(s.string(i), ':', v);
                            b = true;
                        }
                    }
                }
                a[a.length] = ')';
                return a.join("");
            }
            return '!n';
        },
        string: function (x) {
            if (x == "")
                return "\"\"";
            if (rison.id_ok.test(x))
                return x;
            x = x.replace(/(['!])/g, function (a, b) {
                if (sq[b])
                    return '!' + b;
                return b;
            });
            return "\"" + x + "\"";
        },
        undefined: function (x) {
            throw new Error("rison can't encode the undefined value");
        }
    };
    /**
     * rison-encode a javascript structure
     *
     *  implemementation based on Douglas Crockford's json.js:
     *    http://json.org/json.js as of 2006-04-28 from json.org
     *
     */
    rison.encode = function (v) {
        return s[typeof v](v);
    };
    /**
     * rison-encode a javascript object without surrounding parens
     *
     */
    rison.encode_object = function (v) {
        if (typeof v != 'object' || v === null || v instanceof Array)
            throw new Error("rison.encode_object expects an object argument");
        var r = s[typeof v](v);
        return r.substring(1, r.length - 1);
    };
    /**
     * rison-encode a javascript array without surrounding parens
     *
     */
    rison.encode_array = function (v) {
        if (!(v instanceof Array))
            throw new Error("rison.encode_array expects an array argument");
        var r = s[typeof v](v);
        return r.substring(2, r.length - 1);
    };
    /**
     * rison-encode and uri-encode a javascript structure
     *
     */
    rison.encode_uri = function (v) {
        return rison.quote(s[typeof v](v));
    };
})();
//
// based on openlaszlo-json and hacked by nix for use in uris.
//
// Author: Oliver Steele
// Copyright: Copyright 2006 Oliver Steele.  All rights reserved.
// Homepage: http://osteele.com/sources/openlaszlo/json
// License: MIT License.
// Version: 1.0
/**
* parse a rison string into a javascript structure.
*
* this is the simplest decoder entry point.
*
*  based on Oliver Steele's OpenLaszlo-JSON
*     http://osteele.com/sources/openlaszlo/json
*/
rison.decode = function (r) {
    var errcb = function (e) { throw Error('rison decoder error: ' + e); };
    var p = new rison.parser(errcb);
    return p.parse(r);
};
/**
* decode a GeexRouter query param value.
* falls back to the raw string when the value is not valid rison
* (e.g. external OAuth callbacks that bypass GeexRouter encoding).
*/
rison.decode_query_param = function (r) {
    if (r == null || r === "") {
        return "";
    }
    try {
        var decoded = rison.decode(r);
        return decoded == null ? "" : String(decoded);
    }
    catch (e) {
        return String(r);
    }
};
/**
* parse an o-rison string into a javascript structure.
*
* this simply adds parentheses around the string before parsing.
*/
rison.decode_object = function (r) {
    return rison.decode('(' + r + ')');
};
/**
* parse an a-rison string into a javascript structure.
*
* this simply adds array markup around the string before parsing.
*/
rison.decode_array = function (r) {
    return rison.decode('!(' + r + ')');
};
/**
* construct a new parser object for reuse.
*
* @constructor
* @class A Rison parser class.  You should probably
*        use rison.decode instead.
* @see rison.decode
*/
rison.parser = function (errcb) {
    this.errorHandler = errcb;
};
/**
* a string containing acceptable whitespace characters.
* by default the rison decoder tolerates no whitespace.
* to accept whitespace set rison.parser.WHITESPACE = " \t\n\r\f";
*/
rison.parser.WHITESPACE = "";
// expose this as-is?
rison.parser.prototype.setOptions = function (options) {
    if (options['errorHandler'])
        this.errorHandler = options.errorHandler;
};
/**
* parse a rison string into a javascript structure.
*/
rison.parser.prototype.parse = function (str) {
    this.string = str;
    this.index = 0;
    this.message = null;
    var value = this.readValue();
    if (!this.message && this.next())
        value = this.error("unable to parse string as rison: '" + rison.encode(str) + "\"");
    if (this.message && this.errorHandler)
        this.errorHandler(this.message, this.index);
    return value;
};
rison.parser.prototype.error = function (message) {
    if (typeof (console) != 'undefined')
        console.log('rison parser error: ', message);
    this.message = message;
    return undefined;
};
rison.parser.prototype.readValue = function () {
    var c = this.next();
    var fn = c && this.table[c];
    if (fn)
        return fn.apply(this);
    // fell through table, parse as an id
    var s = this.string;
    var i = this.index - 1;
    // Regexp.lastIndex may not work right in IE before 5.5?
    // g flag on the regexp is also necessary
    rison.next_id.lastIndex = i;
    var m = rison.next_id.exec(s);
    // console.log('matched id', i, r.lastIndex);
    if (m.length > 0) {
        var id = m[0];
        this.index = i + id.length;
        return id; // a string
    }
    if (c)
        return this.error("invalid character: '" + c + "\"");
    return this.error("empty expression");
};
rison.parser.parse_date = function (parser) {
    var c;
    while ((c = parser.next()) != "Z") {
        --parser.index;
        var n = parser.readValue();
        if (typeof n == "undefined")
            return undefined;
        return new Date(n);
    }
    return undefined;
};
rison.parser.parse_array = function (parser) {
    var ar = [];
    var c;
    while ((c = parser.next()) != ')') {
        if (!c)
            return parser.error("unmatched '!('");
        if (ar.length) {
            if (c != ',')
                parser.error("missing ','");
        }
        else if (c == ',') {
            return parser.error("extra ','");
        }
        else
            --parser.index;
        var n = parser.readValue();
        if (typeof n == "undefined")
            return undefined;
        ar.push(n);
    }
    return ar;
};
rison.parser.bangs = {
    t: true,
    f: false,
    n: null,
    '(': rison.parser.parse_array,
    d: rison.parser.parse_date
};
rison.parser.prototype.table = {
    '!': function () {
        var s = this.string;
        var c = s.charAt(this.index++);
        if (!c)
            return this.error('"!" at end of input');
        var x = rison.parser.bangs[c];
        if (typeof (x) == 'function') {
            return x.call(null, this);
        }
        else if (typeof (x) == 'undefined') {
            return this.error('unknown literal: "!' + c + '"');
        }
        return x;
    },
    '(': function () {
        var o = {};
        var c;
        var count = 0;
        while ((c = this.next()) != ')') {
            if (count) {
                if (c != ',')
                    this.error("missing ','");
            }
            else if (c == ',') {
                return this.error("extra ','");
            }
            else
                --this.index;
            var k = this.readValue();
            if (typeof k == "undefined")
                return undefined;
            if (this.next() != ':')
                return this.error("missing ':'");
            var v = this.readValue();
            if (typeof v == "undefined")
                return undefined;
            o[k] = v;
            count++;
        }
        return o;
    },
    "\"": function () {
        var s = this.string;
        var i = this.index;
        var start = i;
        var segments = [];
        var c;
        while ((c = s.charAt(i++)) != "\"") {
            //if (i == s.length) return this.error('unmatched "\""');
            if (!c)
                return this.error('unmatched "\""');
            if (c == '!') {
                if (start < i - 1)
                    segments.push(s.slice(start, i - 1));
                c = s.charAt(i++);
                if ("!'".indexOf(c) >= 0) {
                    segments.push(c);
                }
                else {
                    return this.error('invalid string escape: "!' + c + '"');
                }
                start = i;
            }
        }
        if (start < i - 1)
            segments.push(s.slice(start, i - 1));
        this.index = i;
        return segments.length == 1 ? segments[0] : segments.join("");
    },
    // Also any digit.  The statement that follows this table
    // definition fills in the digits.
    '-': function () {
        var s = this.string;
        var i = this.index;
        var start = i - 1;
        var state = 'int';
        var permittedSigns = '-';
        var transitions = {
            'int+.': 'frac',
            'int+e': 'exp',
            'frac+e': 'exp'
        };
        do {
            var c = s.charAt(i++);
            if (!c)
                break;
            if ('0' <= c && c <= '9')
                continue;
            if (permittedSigns.indexOf(c) >= 0) {
                permittedSigns = "";
                continue;
            }
            state = transitions[state + '+' + c.toLowerCase()];
            if (state == 'exp')
                permittedSigns = '-';
        } while (state);
        this.index = --i;
        s = s.slice(start, i);
        if (s == '-')
            return this.error("invalid number");
        return Number(s);
    }
};
// copy table['-'] to each of table[i] | i <- '0'..'9':
(function (table) {
    for (var i = 0; i <= 9; i++)
        table[String(i)] = table['-'];
})(rison.parser.prototype.table);
// return the next non-whitespace character, or undefined
rison.parser.prototype.next = function () {
    var s = this.string;
    var i = this.index;
    do {
        if (i == s.length)
            return undefined;
        var c = s.charAt(i++);
    } while (rison.parser.WHITESPACE.indexOf(c) >= 0);
    this.index = i;
    return c;
};

function geexIsEqual$1(a, b) {
    if (Object.is(a, b)) {
        return true;
    }
    try {
        return JSON.stringify(a) === JSON.stringify(b);
    }
    catch {
        return false;
    }
}
class RoutedComponent extends BusinessComponentBase {
    defaultParams;
    paramsForm;
    params;
    title = signal(undefined, ...(ngDevMode ? [{ debugName: "title" }] : []));
    cdr = inject(ChangeDetectorRef);
    fb = inject(FormBuilder);
    loading = signal(false, ...(ngDevMode ? [{ debugName: "loading" }] : []));
    loadingSrv = inject(LoadingService);
    location = inject(Location);
    reuseTabSrv = inject(ReuseTabService);
    route = inject(ActivatedRoute);
    titleSrv = inject(TitleService);
    async ngOnInit() {
        this.defaultParams = Object.fromEntries(Object.entries(this.routeParamsMappings).map(([key, mapping]) => [key, mapping.default]));
        this.params ??= signal(this.defaultParams);
        this.paramsForm ??= this.buildParamsForm(this.defaultParams);
    }
    constructor() {
        super();
        effect(async () => {
            await this.handleRouteReload();
        }, {});
    }
    /** Full route-reload pipeline; override to replace navigation side effects. */
    async handleRouteReload() {
        this.router.navigationReload();
        this.loading.set(true);
        const routeParams = {
            pathParams: await this.route.params.firstValuePromise(),
            queryParams: await this.route.queryParams.firstValuePromise(),
            fragment: await this.route.fragment.firstValuePromise(),
        };
        const params = await this.resolve(routeParams);
        this.paramsForm.reset(params, { emitEvent: false });
        this.params.set(params);
        await this.beforeOnRouted(params);
        await this.onRouted(params);
        await this.afterOnRouted(params);
        this.loading.set(false);
        const title = this.title();
        if (title)
            this.reuseTabSrv.title = title;
        this.cdr.detectChanges();
    }
    beforeOnRouted(_params) { }
    afterOnRouted(_params) { }
    buildParamsForm(defaults) {
        return this.fb.group(Object.fromEntries(Object.entries(defaults).map(x => [x[0], new FormControl(x[1])])));
    }
    decodeQueryParam(raw) {
        return exports$1.decode(raw);
    }
    async resolve({ pathParams, queryParams, fragment }) {
        const params = {};
        if (this.routeParamsMappings) {
            const mappings = Object.entries(this.routeParamsMappings);
            mappings.forEach(([key, mappingValue]) => {
                const value = match(mappingValue.position)
                    .with("pathParams", () => pathParams?.[key])
                    .with("queryParams", () => (queryParams?.[key] ? this.decodeQueryParam(queryParams[key]) : undefined))
                    .with("fragment", () => fragment)
                    .exhaustive();
                params[key] = value ?? mappingValue.default;
            });
        }
        return params;
    }
    refresh() {
        const { pathParams, queryParams, fragment } = this.paramsToRouteParams(this.paramsForm.value);
        this.router.navigate([".", pathParams], {
            relativeTo: this.route,
            queryParams,
            fragment,
            forceReload: true,
            replaceUrl: true,
        });
    }
    reset() {
        this.paramsForm.reset(this.defaultParams, { emitEvent: false });
        this.refresh();
    }
    paramsToRouteParams(params) {
        const routeParams = { pathParams: {}, queryParams: {}, fragment: undefined };
        const mappings = Object.entries(this.routeParamsMappings);
        mappings.forEach(([key, mappingValue]) => {
            const paramValue = params[key] ?? mappingValue.default;
            const defaultParamValue = this.defaultParams[key];
            if (paramValue == undefined || this.isEqualToDefault(paramValue, defaultParamValue)) {
                return;
            }
            match(mappingValue.position)
                .with("pathParams", () => (routeParams.pathParams[key] = paramValue))
                .with("queryParams", () => (routeParams.queryParams[key] = paramValue))
                .with("fragment", () => (routeParams.fragment = paramValue))
                .exhaustive();
        });
        return routeParams;
    }
    isEqualToDefault(value, defaultValue) {
        return geexIsEqual$1(value, defaultValue);
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: RoutedComponent, deps: [], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: RoutedComponent });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: RoutedComponent, decorators: [{
            type: Injectable
        }], ctorParameters: () => [] });

class ListPageParams {
    pi;
    ps;
    /** Host GraphQL sort input; kept loose to match module-specific SortInput types. */
    sort;
}
class RoutedListComponent extends RoutedComponent {
    data = signal([], ...(ngDevMode ? [{ debugName: "data" }] : []));
    total = signal(0, ...(ngDevMode ? [{ debugName: "total" }] : []));
    selectedData = signal([], ...(ngDevMode ? [{ debugName: "selectedData" }] : []));
    allSelected = computed(() => {
        return this.selectedData().length > 0 && this.data()?.length == this.selectedData().length;
    }, ...(ngDevMode ? [{ debugName: "allSelected" }] : []));
    onAllChecked(value) {
        this.selectedData.set(value ? this.data() : []);
    }
    onItemChecked(data, checked) {
        if (checked) {
            this.selectedData.update(selectedData => [...selectedData, data]);
        }
        else {
            this.selectedData.update(selectedData => selectedData.filter(x => x.id !== data.id));
        }
    }
    async tableChange(args) {
        if (args.type == "loaded") {
            return;
        }
        if (args.type == "checkbox") {
            this.onTableCheckbox(args);
            return;
        }
        if (args.type == "pi" || args.type == "ps") {
            this.onTablePage(args);
        }
        if (args.sort?.column?.index) {
            this.onTableSort(args);
        }
    }
    onTableCheckbox(args) {
        this.selectedData.set(args.checkbox);
    }
    onTablePage(args) {
        if (args.pi !== this.paramsForm.value.pi || args.ps !== this.paramsForm.value.ps) {
            this.paramsForm.patchValue({ pi: args.pi, ps: args.ps });
            this.refresh();
        }
    }
    onTableSort(args) {
        const thisSortName = args.sort.column["indexKey"];
        let sorts = args.sort.map["sort"].split("-").map((x) => x.split("."));
        const thisSort = sorts.find((x) => x[0] == thisSortName);
        if (thisSort) {
            sorts = sorts.filter((x) => x !== thisSort);
        }
        sorts.push(thisSort);
        sorts = sorts.filter((x) => x != undefined && x[0] != "");
        const sortsForm = new FormGroup(Object.fromEntries(sorts.map((x) => [x[0], new FormControl(x[1])])));
        this.paramsForm.setControl("sort", sortsForm);
        this.refresh();
    }
    batchOperation(operation, entityType, remark) {
        return new Promise((resolve) => {
            const selectedData = this.selectedData();
            const filtered = this.filterBatchIds(operation, selectedData);
            if (filtered.error) {
                this.msgSrv.warning(filtered.error);
                resolve(false);
                return;
            }
            const ids = filtered.ids;
            if (!ids.length) {
                this.msgSrv.warning("至少选择一项");
                resolve(false);
                return;
            }
            const apiName = this.buildBatchMutation(operation, entityType);
            this.confirmBatch(operation, apiName, ids, remark).then(resolve);
        });
    }
    filterBatchIds(operation, selectedData) {
        let ids = selectedData.map(x => x["id"]);
        if (selectedData[0]?.["approveStatus"] == undefined) {
            return { ids };
        }
        let text = "";
        switch (operation) {
            case "delete":
            case "submit":
                ids = selectedData.filter(x => x["approveStatus"] === "DEFAULT").map(x => x["id"]);
                text = "只能操作未上报状态的数据";
                break;
            case "approve":
            case "unSubmit":
                ids = selectedData.filter(x => x["approveStatus"] === "SUBMITTED").map(x => x["id"]);
                text = "只能操作已上报状态的数据";
                break;
            case "unApprove":
                ids = selectedData.filter(x => x["approveStatus"] === "APPROVED").map(x => x["id"]);
                text = "只能操作已审核状态的数据";
                break;
            default:
                break;
        }
        if (ids.length !== selectedData.length) {
            return { ids, error: text };
        }
        return { ids };
    }
    buildBatchMutation(operation, entityType) {
        if (operation === "delete") {
            return `
          mutation ${operation}${entityType}($ids: [String!]!) {
            ${operation}${entityType}(ids: $ids)
          }
        `;
        }
        return `
      mutation ${operation}${entityType}($ids: [String], $remark:String) {
        ${operation}${entityType}(ids: $ids, remark:$remark)
      }
      `;
    }
    confirmBatch(operation, apiName, ids, remark) {
        return new Promise((resolve) => {
            const common = this.I18N.Common;
            const alertMessage = common?.action?.get?.(operation) ?? operation;
            this.nzModalSrv.confirm({
                nzTitle: `确认${alertMessage}吗？`,
                nzOnOk: async () => {
                    await this.apollo.mutate({
                        mutation: gql$1(apiName),
                        variables: {
                            remark,
                            ids,
                        },
                    }).firstValuePromise();
                    this.msgSrv.success(common?.message?.get?.(operation) ?? "ok");
                    this.refresh();
                    resolve(true);
                },
                nzOnCancel: () => resolve(false),
            });
        });
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: RoutedListComponent, deps: null, target: i0.ɵɵFactoryTarget.Component });
    static ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "14.0.0", version: "20.3.0", type: RoutedListComponent, isStandalone: true, selector: "ng-component", usesInheritance: true, ngImport: i0, template: "", isInline: true });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: RoutedListComponent, decorators: [{
            type: Component,
            args: [{ template: "", standalone: true }]
        }] });

function geexIsEqual(a, b) {
    if (Object.is(a, b)) {
        return true;
    }
    try {
        return JSON.stringify(a) === JSON.stringify(b);
    }
    catch {
        return false;
    }
}
class RoutedEditComponent extends RoutedComponent {
    entity;
    entityForm;
    originalValue;
    async close() {
        if (await this.closableCheck()) {
            await this.back();
        }
    }
    closableCheck() {
        if (!this.isEntityDirty()) {
            return Promise.resolve(true);
        }
        return new Promise((resolve) => {
            this.nzModalSrv.confirm({
                nzTitle: this.unsavedConfirmTitle(),
                nzOnOk: async () => {
                    this.entityForm?.reset(this.originalValue);
                    this.entityForm?.markAsPristine();
                    resolve(true);
                },
                nzOnCancel: () => {
                    resolve(false);
                },
            });
        });
    }
    isEntityDirty() {
        return !geexIsEqual(this.entityForm?.value, this.originalValue);
    }
    unsavedConfirmTitle() {
        return "当前页面内容未保存，确定离开？";
    }
    async back(reload = false) {
        if (reload) {
            if (this.params().id) {
                await this.router.navigate(["../../"], { relativeTo: this.route, replaceUrl: true, forceReload: reload });
            }
            else {
                await this.router.navigate(["../"], { relativeTo: this.route, replaceUrl: true, forceReload: reload });
            }
        }
        else {
            this.location.back();
        }
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: RoutedEditComponent, deps: null, target: i0.ɵɵFactoryTarget.Component });
    static ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "14.0.0", version: "20.3.0", type: RoutedEditComponent, isStandalone: true, selector: "ng-component", usesInheritance: true, ngImport: i0, template: "", isInline: true });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: RoutedEditComponent, decorators: [{
            type: Component,
            args: [{
                    template: "",
                    standalone: true,
                }]
        }] });

/**
 * Base for components opened inside nz-modal.
 * NzModalRef is required; only use for modal-hosted components.
 */
class ModalComponentBase {
    title = "新增";
    loading = false;
    nzModalRef = inject(NzModalRef);
    success(result = true) {
        if (result) {
            this.nzModalRef.close(result);
            this.afterClose(result);
        }
        else {
            this.close();
        }
    }
    close(_$event) {
        this.nzModalRef.close();
        this.afterClose(undefined);
    }
    /** Hook after modal closes; override for cleanup / analytics. */
    afterClose(_result) { }
}

class TreeTableComponentBase {
    mapOfExpandedData = {};
    I18N = inject(GEEX_I18N);
    getNodeKey(node) {
        return node["key"];
    }
    getNodeChildren(node) {
        return node["children"];
    }
    collapse(array, data, $event) {
        if (!$event) {
            const children = this.getNodeChildren(data);
            if (children) {
                children.forEach(d => {
                    const target = array.find(a => this.getNodeKey(a) === this.getNodeKey(d));
                    target["expand"] = false;
                    this.collapse(array, target, false);
                });
            }
        }
    }
    convertTreeToList(root) {
        const stack = [];
        const array = [];
        const hashMap = {};
        stack.push({ ...root, level: 0, expand: false });
        while (stack.length !== 0) {
            const node = stack.pop();
            this.visitNode(node, hashMap, array);
            const children = this.getNodeChildren(node);
            if (children) {
                for (let i = children.length - 1; i >= 0; i--) {
                    stack.push({
                        ...children[i],
                        level: (node["level"] ?? 0) + 1,
                        expand: false,
                        parent: node,
                    });
                }
            }
        }
        return array;
    }
    visitNode(node, hashMap, array) {
        const key = this.getNodeKey(node);
        if (!hashMap[key]) {
            hashMap[key] = true;
            array.push(node);
        }
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: TreeTableComponentBase, deps: [], target: i0.ɵɵFactoryTarget.Component });
    static ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "14.0.0", version: "20.3.0", type: TreeTableComponentBase, isStandalone: true, selector: "ng-component", ngImport: i0, template: "", isInline: true });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: TreeTableComponentBase, decorators: [{
            type: Component,
            args: [{
                    template: "",
                    standalone: true,
                }]
        }] });

class GeexRouter extends Router {
    lastRoute;
    constructor(injector) {
        super();
        const routerEvent = this.events.toSignal();
        effect(() => {
            match(routerEvent())
                .with(P.instanceOf(RouteConfigLoadEnd), (x) => {
                this.lastRoute = x.route;
            })
                .with(P.instanceOf(NavigationEnd), async (x) => {
                const tabSrv = injector.get(ReuseTabService);
                const location = injector.get(Location);
                const cachedTabs = tabSrv.items;
                const deepest = this.routerState.snapshot.root.getDeepestRouteConfig?.();
                const activeRoutedPage = deepest?.component;
                if (!(activeRoutedPage?.prototype instanceof RoutedComponent)) {
                    return;
                }
                const currentUrl = this.lastSuccessfulNavigation?.extractedUrl?.toString();
                const previousUrl = this.lastSuccessfulNavigation?.previousNavigation?.extractedUrl?.toString();
                const cachedTab = cachedTabs.find(tab => tab.url === previousUrl);
                if (this.lastRoute?.data?.["reuse"] === false ||
                    (this.lastSuccessfulNavigation?.extras?.replaceUrl &&
                        currentUrl !== previousUrl &&
                        this.isDifferentPath(currentUrl, previousUrl))) {
                    cachedTab && tabSrv.close(previousUrl);
                }
                if (this.lastSuccessfulNavigation?.extras?.forceReload || cachedTabs.every(tab => tab.url !== currentUrl)) {
                    this.navigationReload.set({
                        ...x,
                        ...this.lastSuccessfulNavigation,
                    });
                }
                location.replaceState(currentUrl);
            });
        }, {});
    }
    isDifferentPath(currentUrl, previousUrl) {
        if (!currentUrl || !previousUrl) {
            return true;
        }
        const getCurrentPath = (url) => {
            const questionMarkIndex = url.indexOf("?");
            return questionMarkIndex === -1 ? url : url.substring(0, questionMarkIndex);
        };
        return getCurrentPath(currentUrl) !== getCurrentPath(previousUrl);
    }
    createUrlTree(commands, navigationExtras = {}) {
        if (navigationExtras.queryParams) {
            const processedParams = {};
            for (const key in navigationExtras.queryParams) {
                const value = navigationExtras.queryParams[key];
                try {
                    processedParams[key] = exports$1.encode(value);
                }
                catch (e) {
                    console.warn(e);
                    processedParams[key] = value;
                }
            }
            navigationExtras = {
                ...navigationExtras,
                queryParams: processedParams,
            };
        }
        return super.createUrlTree(commands, navigationExtras);
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexRouter, deps: [{ token: i0.Injector }], target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexRouter });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexRouter, decorators: [{
            type: Injectable
        }], ctorParameters: () => [{ type: i0.Injector }] });

/**
 * Hardens Delon ReuseTabStrategy against undefined snapshots / empty-path leaves
 * that otherwise throw during createRouterState / outlet.detach (NG04012).
 */
class GeexReuseTabStrategy extends ReuseTabStrategy {
    shouldReuseRoute(future, curr) {
        if (!future || !curr) {
            return false;
        }
        return super.shouldReuseRoute(future, curr);
    }
    shouldDetach(route) {
        if (!route?.routeConfig || route.routeConfig.path === "") {
            return false;
        }
        return super.shouldDetach(route);
    }
    retrieve(route) {
        if (!route?.routeConfig) {
            return null;
        }
        return super.retrieve(route);
    }
    shouldAttach(route) {
        if (!route?.routeConfig) {
            return false;
        }
        return super.shouldAttach(route);
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexReuseTabStrategy, deps: null, target: i0.ɵɵFactoryTarget.Injectable });
    static ɵprov = i0.ɵɵngDeclareInjectable({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexReuseTabStrategy });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: GeexReuseTabStrategy, decorators: [{
            type: Injectable
        }] });

class ListPageLayoutComponent {
    i18n = inject(GEEX_I18N, { optional: true });
    title = input.required(...(ngDevMode ? [{ debugName: "title" }] : []));
    loading = input(false, ...(ngDevMode ? [{ debugName: "loading" }] : []));
    total = input(0, ...(ngDevMode ? [{ debugName: "total" }] : []));
    data = input([], ...(ngDevMode ? [{ debugName: "data" }] : []));
    columns = input([], ...(ngDevMode ? [{ debugName: "columns" }] : []));
    pi = input(1, ...(ngDevMode ? [{ debugName: "pi" }] : []));
    ps = input(10, ...(ngDevMode ? [{ debugName: "ps" }] : []));
    selectedCount = input(0, ...(ngDevMode ? [{ debugName: "selectedCount" }] : []));
    multiSort = input(true, ...(ngDevMode ? [{ debugName: "multiSort" }] : []));
    filtersInHeader = input(true, ...(ngDevMode ? [{ debugName: "filtersInHeader" }] : []));
    tableChange = output();
    refresh = output();
    headerExtraTpl = contentChild("headerExtra", ...(ngDevMode ? [{ debugName: "headerExtraTpl" }] : []));
    headerTabTpl = contentChild("headerTab", ...(ngDevMode ? [{ debugName: "headerTabTpl" }] : []));
    headerActionTpl = contentChild("headerAction", ...(ngDevMode ? [{ debugName: "headerActionTpl" }] : []));
    get selectedLabel() {
        return this.i18n?.Common?.list?.selected ?? "";
    }
    get selectedUnitLabel() {
        return this.i18n?.Common?.list?.selectedUnit ?? "";
    }
    get refreshLabel() {
        return this.i18n?.Common?.list?.refresh ?? "Refresh";
    }
    static ɵfac = i0.ɵɵngDeclareFactory({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: ListPageLayoutComponent, deps: [], target: i0.ɵɵFactoryTarget.Component });
    static ɵcmp = i0.ɵɵngDeclareComponent({ minVersion: "17.0.0", version: "20.3.0", type: ListPageLayoutComponent, isStandalone: true, selector: "list-page-layout", inputs: { title: { classPropertyName: "title", publicName: "title", isSignal: true, isRequired: true, transformFunction: null }, loading: { classPropertyName: "loading", publicName: "loading", isSignal: true, isRequired: false, transformFunction: null }, total: { classPropertyName: "total", publicName: "total", isSignal: true, isRequired: false, transformFunction: null }, data: { classPropertyName: "data", publicName: "data", isSignal: true, isRequired: false, transformFunction: null }, columns: { classPropertyName: "columns", publicName: "columns", isSignal: true, isRequired: false, transformFunction: null }, pi: { classPropertyName: "pi", publicName: "pi", isSignal: true, isRequired: false, transformFunction: null }, ps: { classPropertyName: "ps", publicName: "ps", isSignal: true, isRequired: false, transformFunction: null }, selectedCount: { classPropertyName: "selectedCount", publicName: "selectedCount", isSignal: true, isRequired: false, transformFunction: null }, multiSort: { classPropertyName: "multiSort", publicName: "multiSort", isSignal: true, isRequired: false, transformFunction: null }, filtersInHeader: { classPropertyName: "filtersInHeader", publicName: "filtersInHeader", isSignal: true, isRequired: false, transformFunction: null } }, outputs: { tableChange: "tableChange", refresh: "refresh" }, queries: [{ propertyName: "headerExtraTpl", first: true, predicate: ["headerExtra"], descendants: true, isSignal: true }, { propertyName: "headerTabTpl", first: true, predicate: ["headerTab"], descendants: true, isSignal: true }, { propertyName: "headerActionTpl", first: true, predicate: ["headerAction"], descendants: true, isSignal: true }], ngImport: i0, template: `
    <page-header [title]="title()" [tab]="headerTabTpl()" [extra]="headerExtraTpl()" [action]="headerActionTpl()">
      @if (filtersInHeader()) {
        <ng-content select="[filters]" />
      }
    </page-header>

    <nz-card>
      @if (!filtersInHeader()) {
        <ng-content select="[filters]" />
      }
      <nz-alert class="mb-sm" nzType="info" nzShowIcon [nzMessage]="selectionMessage">
        <ng-template #selectionMessage>
          <span>{{ selectedLabel }}{{ selectedCount() }}{{ selectedUnitLabel }}</span>
          <nz-divider nzType="vertical" />
          <a (click)="refresh.emit()">
            <i nz-icon nzType="reload"></i>
            {{ refreshLabel }}
          </a>
          <ng-content select="[toolbar]" />
        </ng-template>
      </nz-alert>
      <st
        class="mt-sm"
        [multiSort]="multiSort()"
        [loading]="loading()"
        [total]="total()"
        [data]="data()"
        [pi]="pi()"
        [ps]="ps()"
        [columns]="columns()"
        (change)="tableChange.emit($event)"
      />
    </nz-card>
  `, isInline: true, dependencies: [{ kind: "ngmodule", type: PageHeaderModule }, { kind: "component", type: i1.PageHeaderComponent, selector: "page-header", inputs: ["title", "titleSub", "loading", "wide", "home", "homeLink", "homeI18n", "autoBreadcrumb", "autoTitle", "syncTitle", "fixed", "fixedOffsetTop", "breadcrumb", "recursiveBreadcrumb", "logo", "action", "content", "extra", "tab"], exportAs: ["pageHeader"] }, { kind: "ngmodule", type: STModule }, { kind: "component", type: i2.STComponent, selector: "st", inputs: ["req", "res", "page", "data", "delay", "columns", "contextmenu", "ps", "pi", "total", "loading", "loadingDelay", "loadingIndicator", "bordered", "size", "scroll", "drag", "singleSort", "multiSort", "rowClassName", "clickRowClassName", "widthMode", "widthConfig", "resizable", "header", "showHeader", "footer", "bodyHeader", "body", "expandRowByClick", "expandAccordion", "expand", "expandIcon", "noResult", "responsive", "responsiveHideHeaderFooter", "virtualScroll", "virtualItemSize", "virtualMaxBufferPx", "virtualMinBufferPx", "customRequest", "virtualForTrackBy", "trackBy"], outputs: ["error", "change"], exportAs: ["st"] }, { kind: "ngmodule", type: NzCardModule }, { kind: "component", type: i3.NzCardComponent, selector: "nz-card", inputs: ["nzBordered", "nzLoading", "nzHoverable", "nzBodyStyle", "nzCover", "nzActions", "nzType", "nzSize", "nzTitle", "nzExtra"], exportAs: ["nzCard"] }, { kind: "ngmodule", type: NzAlertModule }, { kind: "component", type: i4.NzAlertComponent, selector: "nz-alert", inputs: ["nzAction", "nzCloseText", "nzIconType", "nzMessage", "nzDescription", "nzType", "nzCloseable", "nzShowIcon", "nzBanner", "nzNoAnimation", "nzIcon"], outputs: ["nzOnClose"], exportAs: ["nzAlert"] }, { kind: "ngmodule", type: NzDividerModule }, { kind: "component", type: i5.NzDividerComponent, selector: "nz-divider", inputs: ["nzText", "nzType", "nzOrientation", "nzVariant", "nzDashed", "nzPlain"], exportAs: ["nzDivider"] }, { kind: "ngmodule", type: NzIconModule }, { kind: "directive", type: i6.NzIconDirective, selector: "nz-icon,[nz-icon]", inputs: ["nzSpin", "nzRotate", "nzType", "nzTheme", "nzTwotoneColor", "nzIconfont"], exportAs: ["nzIcon"] }] });
}
i0.ɵɵngDeclareClassMetadata({ minVersion: "12.0.0", version: "20.3.0", ngImport: i0, type: ListPageLayoutComponent, decorators: [{
            type: Component,
            args: [{
                    selector: "list-page-layout",
                    standalone: true,
                    imports: [PageHeaderModule, STModule, NzCardModule, NzAlertModule, NzDividerModule, NzIconModule],
                    template: `
    <page-header [title]="title()" [tab]="headerTabTpl()" [extra]="headerExtraTpl()" [action]="headerActionTpl()">
      @if (filtersInHeader()) {
        <ng-content select="[filters]" />
      }
    </page-header>

    <nz-card>
      @if (!filtersInHeader()) {
        <ng-content select="[filters]" />
      }
      <nz-alert class="mb-sm" nzType="info" nzShowIcon [nzMessage]="selectionMessage">
        <ng-template #selectionMessage>
          <span>{{ selectedLabel }}{{ selectedCount() }}{{ selectedUnitLabel }}</span>
          <nz-divider nzType="vertical" />
          <a (click)="refresh.emit()">
            <i nz-icon nzType="reload"></i>
            {{ refreshLabel }}
          </a>
          <ng-content select="[toolbar]" />
        </ng-template>
      </nz-alert>
      <st
        class="mt-sm"
        [multiSort]="multiSort()"
        [loading]="loading()"
        [total]="total()"
        [data]="data()"
        [pi]="pi()"
        [ps]="ps()"
        [columns]="columns()"
        (change)="tableChange.emit($event)"
      />
    </nz-card>
  `,
                }]
        }] });

/**
 * Delon-coupled Core providers (Router subclass + ReuseTab + optional AppPermission).
 */
function provideGeexDelonBase(options = {}) {
    return [
        ...(options.appPermission ? [{ provide: GEEX_APP_PERMISSION, useValue: options.appPermission }] : []),
        ...(options.reuseTab ? [provideReuseTabConfig(options.reuseTab)] : []),
        { provide: Router, useClass: options.router ?? GeexRouter },
        { provide: RouteReuseStrategy, useClass: options.reuseStrategy ?? GeexReuseTabStrategy },
        importProvidersFrom(AlainThemeModule.forRoot(), DelonFormModule.forRoot()),
    ];
}

// @ts-nocheck
Array.prototype.add = function add(element) {
    return List.prototype.add.bind(new List(this), element)();
};
Array.prototype.clear = function clear() {
    while (this.pop()) { }
};
Array.prototype.addRange = function addRange(elements) {
    return List.prototype.addRange.bind(new List(this), elements)();
};
Array.prototype.aggregate = function aggregate(accumulator, initialValue) {
    return List.prototype.aggregate.bind(new List(this), accumulator, initialValue)();
};
Array.prototype.all = function all(predicate) {
    return List.prototype.all.bind(new List(this), predicate)();
};
Array.prototype.any = function any(predicate) {
    if (this === undefined) {
        return false;
    }
    return List.prototype.any.bind(new List(this), predicate)();
};
Array.prototype.average = function average(transform) {
    return List.prototype.average.bind(new List(this))(transform).toArray();
};
Array.prototype.contains = function contains(element) {
    return List.prototype.contains.bind(new List(this), element)();
};
Array.prototype.count = function count(predicate) {
    return List.prototype.count.bind(new List(this))(predicate);
};
Array.prototype.defaultIfEmpty = function defaultIfEmpty(defaultValue) {
    return List.prototype.defaultIfEmpty.bind(new List(this))(defaultValue).toArray();
};
Array.prototype.distinct = function distinct() {
    return List.prototype.distinct.bind(new List(this))().toArray();
};
Array.prototype.distinctBy = function distinctBy(keySelector) {
    return List.prototype.distinctBy.bind(new List(this))(keySelector).toArray();
};
Array.prototype.elementAt = function elementAt(index) {
    return List.prototype.elementAt.bind(new List(this))(index);
};
Array.prototype.elementAtOrDefault = function elementAtOrDefault(index) {
    if (this?.length) {
        if (this.length <= index) {
            return undefined;
        }
        return List.prototype.elementAtOrDefault.bind(new List(this))(index);
    }
    return undefined;
};
Array.prototype.except = function except(source) {
    return List.prototype.except.bind(new List(this))(source).toArray();
};
Array.prototype.first = function first(predicate) {
    return List.prototype.first.bind(new List(this), predicate)();
};
Array.prototype.firstOrDefault = function firstOrDefault(predicate, defaultValue) {
    const result = List.prototype.firstOrDefault.bind(new List(this), predicate)();
    return result === undefined ? defaultValue : result;
};
Array.prototype.first = function first(predicate) {
    return List.prototype.first.bind(new List(this), predicate)();
};
Array.prototype.firstOrDefault = function firstOrDefault(predicate, defaultValue) {
    const result = List.prototype.firstOrDefault.bind(new List(this), predicate)();
    return result === undefined ? defaultValue : result;
};
Array.prototype.groupBy = function groupBy(grouper, mapper) {
    return List.prototype.groupBy.bind(new List(this))(grouper, mapper);
};
Array.prototype.groupJoin = function groupJoin(list, key1, key2, result) {
    return List.prototype.groupJoin.bind(new List(this), key1, key2, result)().toArray();
};
Array.prototype.insert = function insert(index, element) {
    return List.prototype.insert.bind(new List(this))(index, element);
};
Array.prototype.intersect = function intersect(source) {
    return List.prototype.intersect.bind(new List(this), new List(source))().toArray();
};
Array.prototype.linqJoin = function linqJoin(list, key1, key2, result) {
    return List.prototype.join.bind(new List(this), key1, key2, result)().toArray();
};
Array.prototype.last = function last(predicate) {
    return List.prototype.last.bind(new List(this))(predicate);
};
Array.prototype.lastOrDefault = function lastOrDefault(predicate) {
    return List.prototype.lastOrDefault.bind(new List(this))(predicate);
};
Array.prototype.max = function max(selector) {
    return List.prototype.max.bind(new List(this))(selector);
};
Array.prototype.min = function min(selector) {
    return List.prototype.min.bind(new List(this))(selector);
};
Array.prototype.ofType = function ofType($type) {
    return List.prototype.ofType.bind(new List(this), $type)();
};
Array.prototype.orderBy = function orderBy(keySelector, comparer) {
    return List.prototype.orderBy.bind(new List(this), keySelector, comparer)().toArray();
};
Array.prototype.orderByDescending = function orderByDescending(keySelector, comparer) {
    return List.prototype.orderByDescending.bind(new List(this), keySelector, comparer)().toArray();
};
Array.prototype.thenBy = function thenBy(keySelector) {
    return List.prototype.thenBy.bind(new List(this), keySelector)().toArray();
};
Array.prototype.thenByDescending = function thenByDescending(keySelector) {
    return List.prototype.thenByDescending.bind(new List(this), keySelector)().toArray();
};
Array.prototype.remove = function remove(element) {
    return List.prototype.remove.bind(new List(this), element)();
};
Array.prototype.removeAll = function removeAll(predicate) {
    return List.prototype.removeAll.bind(new List(this))(predicate).toArray();
};
Array.prototype.removeAt = function removeAt(index) {
    return List.prototype.removeAt.bind(new List(this), index)();
};
Array.prototype.selectMany = function selectMany(selector) {
    return List.prototype.selectMany.bind(new List(this))(selector).toArray();
};
Array.prototype.sequenceEqual = function sequenceEqual(list) {
    if (this?.length !== list?.length) {
        return false;
    }
    if (this?.length === 0 && this?.length === list?.length) {
        return true;
    }
    for (let i = 0; i < this.length; i++) {
        const element = this[i];
        if (this[i] === list[i]) {
            continue;
        }
        else {
            return false;
        }
    }
    return true;
};
Array.prototype.single = function single(predicate) {
    return List.prototype.single.bind(new List(this), predicate)();
};
Array.prototype.singleOrDefault = function singleOrDefault(predicate) {
    return List.prototype.singleOrDefault.bind(new List(this), predicate)();
};
Array.prototype.skip = function skip(amount) {
    return List.prototype.skip.bind(new List(this), amount)().toArray();
};
Array.prototype.skipWhile = function skipWhile(predicate) {
    return List.prototype.skipWhile.bind(new List(this), predicate)().toArray();
};
Array.prototype.sum = function sum(transform) {
    return List.prototype.sum.bind(new List(this))(transform);
};
Array.prototype.take = function take(amount) {
    return List.prototype.take.bind(new List(this), amount)().toArray();
};
Array.prototype.takeWhile = function takeWhile(predicate) {
    return List.prototype.takeWhile.bind(this, predicate)().toArray();
};
Array.prototype.toLookup = function toLookup(keySelector, elementSelector) {
    return List.prototype.toLookup.bind(new List(this), keySelector, elementSelector)();
};
Array.prototype.union = function union(list) {
    return List.prototype.union.bind(new List(this), list)().toArray();
};
Array.prototype.where = function where(predicate) {
    return List.prototype.where.bind(new List(this), predicate)().toArray();
};
Array.prototype.toArray = function where() {
    return this;
};
Array.prototype.zip = function zip(list, result) {
    return List.prototype.zip.bind(new List(this))(list, result).toArray();
};

String.prototype.contains = function (value) {
    return this.indexOf(value) >= 0;
};

function deepSignal(initialValue, options) {
    const result = toDeepSignal(signal(initialValue, options));
    return result;
}
function toDeepSignal(source) {
    const value = untracked(() => source());
    if (!isRecord$1(value)) {
        return source;
    }
    if ("set" in source && typeof source.set === "function") {
        return new Proxy(source, {
            get(target, prop) {
                if (!(prop in value) && prop in target) {
                    return target[prop];
                }
                if (isSignal(target[prop])) {
                    Object.defineProperty(target, prop, {
                        value: computed(() => target()[prop]),
                        configurable: true,
                    });
                }
                if (target[prop] == undefined) {
                    return signal(target[prop]);
                }
                return toDeepSignal(target[prop]);
            },
            set(target, prop, value) {
                if (isSignal(target[prop])) {
                    target[prop].set(value);
                }
                else {
                    target[prop] = value;
                }
                return true;
            },
        });
    }
    return new Proxy(signal, {
        get(target, prop) {
            if (!(prop in value) && prop in target) {
                return target[prop];
            }
            if (!isSignal(target[prop])) {
                Object.defineProperty(target, prop, {
                    value: computed(() => target()[prop]),
                    configurable: true,
                });
            }
            return toDeepSignal(target[prop]);
        },
    });
}
function isRecord$1(value) {
    return value?.constructor === Object;
}
function toDebounced(sourceSignal, debounceTimeInMs = 0) {
    const debounceSignal = signal(sourceSignal(), ...(ngDevMode ? [{ debugName: "debounceSignal" }] : []));
    effect(onCleanup => {
        const value = sourceSignal();
        const timeout = setTimeout(() => debounceSignal.set(value), debounceTimeInMs);
        onCleanup(() => clearTimeout(timeout));
    }, {});
    return debounceSignal;
}
const signal1 = signal(null, ...(ngDevMode ? [{ debugName: "signal1" }] : []));
const signalProto = Object.getPrototypeOf(signal1);
signalProto["toDebounced"] = function (debounceTimeInMs = 0) {
    return toDebounced(this, debounceTimeInMs);
};
signalProto["toDeepSignal"] = function () {
    return toDeepSignal(this);
};
function toObservable(sourceSignal) {
    return of(sourceSignal());
}
signalProto["toObservable"] = function () {
    return toObservable(this);
};
function computedAsync(computation) {
    const resultSignal = signal(null, ...(ngDevMode ? [{ debugName: "resultSignal" }] : []));
    effect(async () => {
        const result = computation();
        const unwrappedResult = await (isObservable(result) ? firstValueFrom(result, { defaultValue: null }) : result);
        resultSignal.set(unwrappedResult);
    }, {});
    return resultSignal.asReadonly();
}

Observable.prototype.lastValuePromise = function () {
    return lastValueFrom(this);
};
Observable.prototype.firstValuePromise = function () {
    return firstValueFrom(this);
};
Observable.prototype.toSignal = function (options) {
    if (options?.deep) {
        return toSignal(this, options).toDeepSignal();
    }
    return toSignal(this, options);
};
Observable.prototype.pipeMap = function (project) {
    return this.pipe(map(project));
};
Observable.prototype.pipeSwitchMap = function (project) {
    return this.pipe(switchMap$1(project));
};
Observable.prototype.pipeFilter = function (predicate) {
    return this.pipe(filter(predicate));
};

function getResolvedUrl() {
    return this["_routerState"].url;
}
function getConfiguredUrl() {
    return `/${this.pathFromRoot
        .filter(v => v.routeConfig)
        .map(v => v.routeConfig.path)
        .where(x => x != "")
        .join("/")}`;
}
function getDeepestRouteConfig() {
    let currentRoute = this;
    while (currentRoute.firstChild) {
        currentRoute = currentRoute.firstChild;
    }
    return currentRoute.routeConfig;
}
ActivatedRouteSnapshot.prototype.getResolvedUrl = getResolvedUrl;
ActivatedRouteSnapshot.prototype.getConfiguredUrl = getConfiguredUrl;
ActivatedRouteSnapshot.prototype.getDeepestRouteConfig = getDeepestRouteConfig;
Object.defineProperty(Router.prototype, "navigationReload", {
    value: signal(undefined),
    writable: false,
});
function transformObjectToForm(fb, obj, options) {
    if (obj instanceof Object && !(obj instanceof Date)) {
        const result = {};
        for (const key in obj) {
            if (obj.hasOwnProperty(key)) {
                if (obj[key] instanceof AbstractControl) {
                    result[key] = obj[key];
                    continue;
                }
                if (obj[key] instanceof Object && !(obj[key] instanceof Array)) {
                    result[key] = transformObjectToForm(fb, obj[key], options);
                }
                else if (obj[key] instanceof Array) {
                    result[key] = fb.array(obj[key].map(item => transformObjectToForm(fb, item, options)), options);
                }
                else {
                    result[key] = new FormControl(obj[key], options);
                }
            }
        }
        return fb.group(result, options);
    }
    else {
        return obj;
    }
}
FormBuilder.prototype.build = function (controls, options) {
    let updated = transformObjectToForm(this, controls, options ?? {});
    return updated;
};

function exportAll(opt) {
    return this.$exportData.firstValuePromise().then(x => this.export(deepCopy(x), opt));
}
STComponent.prototype.exportAll = exportAll;

if (!Object.hasOwnProperty("fromEntries")) {
    Object.fromEntries = function fromEntries(iterable) {
        return [...iterable].reduce((obj, [key, val]) => {
            obj[String(key)] = val;
            return obj;
        }, {});
    };
}
Date.prototype.add = function (value) {
    let result = this;
    if (value.years) {
        result = addYears(result, value.years);
    }
    if (value.months) {
        result = addMonths(result, value.months);
    }
    if (value.weeks) {
        result = addWeeks(result, value.weeks);
    }
    if (value.days) {
        result = addDays(result, value.days);
    }
    if (value.hours) {
        result = addHours(result, value.hours);
    }
    if (value.minutes) {
        result = addMinutes(result, value.minutes);
    }
    if (value.seconds) {
        result = addSeconds(result, value.seconds);
    }
    if (value.milliseconds) {
        result = addMilliseconds(result, value.milliseconds);
    }
    return result;
};
Date.prototype.format = function (format) {
    const yyyy = this.getFullYear().toString();
    format = format.replace(/yyyy/g, yyyy);
    const MM = (this.getMonth() + 1).toString();
    format = format.replace(/MM/g, MM[1] ? MM : `0${MM[0]}`);
    const dd = this.getDate().toString();
    format = format.replace(/dd/g, dd[1] ? dd : `0${dd[0]}`);
    const HH = this.getHours().toString();
    format = format.replace(/HH/g, HH[1] ? HH : `0${HH[0]}`);
    const mm = this.getMinutes().toString();
    format = format.replace(/mm/g, mm[1] ? mm : `0${mm[0]}`);
    const ss = this.getSeconds().toString();
    format = format.replace(/ss/g, ss[1] ? ss : `0${ss[0]}`);
    return format;
};
Date.prototype.getTotalMonth = function () {
    return this.getFullYear() * 12 + this.getMonth() + 1;
};
function extract(object, properties) {
    const result = {};
    for (const property of Object.keys(properties)) {
        result[property] = object[property];
    }
    return result;
}
window["extract"] = extract;
const legacyTrimEnd = String.prototype.trimEnd;
String.prototype.trimEnd = function trimEnd(strToTrim) {
    if (strToTrim == undefined) {
        return legacyTrimEnd.bind(this)();
    }
    return this.replace(new RegExp(`${strToTrim}$`), "");
};
Number.prototype.hasFlag = function hasFlag(...flags) {
    return flags.all(flag => flag !== undefined && (this.valueOf() & flag) == flag);
};
Number.prototype.hasNoFlag = function hasNoFlag(...flags) {
    return flags.all(flag => flag !== undefined && (this.valueOf() & flag) == 0);
};
const flatMapDeep = function (iteratee) {
    if (!this || this.length == 0) {
        return [];
    }
    return this.concat(_.flatten(this.map(iteratee).filter(x => x != undefined)).flatMapDeep(iteratee));
};
Array.prototype.flatMapDeep = flatMapDeep;
const range = (start, end) => Array.from({ length: end - start }, (v, k) => k + start);
Array.range = range;
function assertIsDefined(val) {
    if (val === undefined || val === null) {
        throw new Error(`Expected 'val' to be defined, but received ${val}`);
    }
}
function assertIsArray(val) {
    if (!Array.isArray(val)) {
        throw new Error(`Expected 'val' to be defined, but received ${val}`);
    }
}
function assert(_val) { }
function assertIsNotArray(val) {
    if (Array.isArray(val)) {
        throw new Error(`Expected 'val' to be defined, but received ${val}`);
    }
}
function deepProxy(obj, callback) {
    if (obj === null ||
        obj === undefined ||
        obj instanceof String ||
        obj instanceof Date ||
        obj instanceof Number ||
        obj instanceof Function) {
        return obj;
    }
    if (typeof obj === "object") {
        for (const key in obj) {
            if (typeof obj[key] === "object") {
                obj[key] = deepProxy(obj[key], callback);
            }
        }
    }
    return new Proxy(obj, {
        set: (target, key, value, receiver) => {
            if (typeof value === "object") {
                value = deepProxy(value, callback);
            }
            let cbType = target[key] == undefined ? "create" : "modify";
            if (target[key] === value) {
                cbType = "assignment";
            }
            if (!(Array.isArray(target) && key === "length")) {
                callback(cbType, { target, key, value });
            }
            return Reflect.set(target, key, value, receiver);
        },
        deleteProperty: (target, key) => {
            callback("delete", { target, key, value: undefined });
            return Reflect.deleteProperty(target, key);
        },
    });
}
function isRecord(value) {
    return typeof value === "object" && value !== null && !Array.isArray(value) && Object.keys(value).every(key => typeof key === "string");
}

/// <reference path="../shims.d.ts" />
function computeChecksumMd5() {
    return new Promise((resolve, reject) => {
        const chunkSize = 2097152;
        const spark = new SparkMD5.ArrayBuffer();
        const fileReader = new FileReader();
        let cursor = 0;
        fileReader.onerror = () => {
            reject("MD5 computation failed - error reading the file");
        };
        const processChunk = (chunkStart) => {
            const chunkEnd = Math.min(this.size, chunkStart + chunkSize);
            fileReader.readAsArrayBuffer(this.slice(chunkStart, chunkEnd));
        };
        fileReader.onload = (e) => {
            spark.append(e.target.result);
            cursor += chunkSize;
            if (cursor < this.size) {
                processChunk(cursor);
            }
            else {
                resolve(spark.end());
            }
        };
        processChunk(0);
    });
}
Blob.prototype.computeChecksumMd5 = computeChecksumMd5;

function provideGeexExtensions() {
    // side-effect imports above run when this module is loaded
}

const GEEX_MOBILE_PATH_SUFFIX = new InjectionToken("GEEX_MOBILE_PATH_SUFFIX", {
    providedIn: "root",
    factory: () => "/query",
});

const GEEX_EXCEPTION_403_PROFILE_PATH = new InjectionToken("GEEX_EXCEPTION_403_PROFILE_PATH", {
    providedIn: "root",
    factory: () => "/identity/me",
});
const GEEX_EXCEPTION_403_PROFILE_LABEL = new InjectionToken("GEEX_EXCEPTION_403_PROFILE_LABEL", {
    providedIn: "root",
    factory: () => "个人中心",
});
const GEEX_EXCEPTION_LOGIN_PATH = new InjectionToken("GEEX_EXCEPTION_LOGIN_PATH", {
    providedIn: "root",
    factory: () => "/authentication/login",
});

/**
 * Expose package `geex` on globalThis/window via a live getter.
 * Must not copy the value at bind time — `geex` is only assigned inside `configGeex`.
 */
function bindGeexGlobal() {
    const bind = (target) => {
        Reflect.deleteProperty(target, "geex");
        Object.defineProperty(target, "geex", {
            configurable: true,
            enumerable: true,
            get: () => geex,
        });
    };
    if (typeof globalThis !== "undefined") {
        bind(globalThis);
    }
    if (typeof window !== "undefined") {
        bind(window);
    }
}

/**
 * Generated bundle index. Do not edit.
 */

export { BusinessComponentBase, DebuggerBlockerService, ExtensionModule, GEEX_AFTER_LOGIN_NAVIGATE, GEEX_API_BASE_URL, GEEX_APOLLO_CACHE, GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS, GEEX_APP_MENU_SETTING, GEEX_APP_NAME_SETTING, GEEX_APP_PERMISSION, GEEX_BLOCK_DEBUGGER, GEEX_CANCEL_AUTHENTICATION_DOCUMENT, GEEX_DEFAULT_HTTP_STATUS_MESSAGES, GEEX_DEFAULT_MENUS, GEEX_EXCEPTION_403_PROFILE_LABEL, GEEX_EXCEPTION_403_PROFILE_PATH, GEEX_EXCEPTION_500_PATH, GEEX_EXCEPTION_LOGIN_PATH, GEEX_HTTP_STATUS_MESSAGES, GEEX_I18N, GEEX_I18N_PACKS, GEEX_I18N_SERVICE, GEEX_LOCALIZATION_DATA_SETTING, GEEX_LOCALIZATION_LANGUAGE_SETTING, GEEX_LOGIN_PATH, GEEX_MENU_CONTRIBUTIONS, GEEX_MOBILE_PATH_SUFFIX, GEEX_MODULE_CONTRIBUTIONS, GEEX_PROFILE_LABEL, GEEX_PROFILE_PATH, GEEX_SESSION_TERMINATED_COPY, GEEX_STARTUP_OPTIONS, GEEX_SUPER_ADMIN_USER_ID, Geex, GeexAuthLogout, GeexHttpInterceptor, GeexI18nService, GeexReuseTabStrategy, GeexRouter, GeexStartupService, GeexTranslateLoader, I18N, GeexI18nService as I18NService, ListPageLayoutComponent, ListPageParams, ModalComponentBase, RoutedComponent, RoutedEditComponent, RoutedListComponent, SILENT_REQUEST, SilentApollo, TreeTableComponentBase, applyEnvironmentOverrides, assert, assertIsArray, assertIsDefined, assertIsNotArray, bindGeexGlobal, cancelAuthenticationMutation, computedAsync, configGeex, createGeexGraphqlErrorLink, createGeexHttpApolloOptions, createGeexInMemoryCache, createGeexSilentContextLink, createGeexUploadHttpLink, createGeexUriLink, createGeexWsApolloOptions, createUiModule, deepProxy, deepSignal, extract, geex, geexApolloDefaultOptions, geexDefaultTypePolicies, guardedSignal, isGeexSilentOperation, isRecord, loadEnvironmentOverrides, mergeGeexI18nPacks, provideGeex, provideGeexApollo, provideGeexApolloTypePolicies, provideGeexCommon, provideGeexDelonBase, provideGeexExtensions, provideGeexHttp, provideGeexI18n, provideGeexMenus, provideGeexModuleContribution, provideGeexStartup, exports$1 as rison };
//# sourceMappingURL=geexcode-geex-angular.mjs.map
