import * as i0 from '@angular/core';
import { WritableSignal, Signal, Injector, Provider, InjectionToken, EnvironmentProviders, ChangeDetectorRef, TemplateRef, Type, ExtendedSignal, CreateSignalOptions } from '@angular/core';
import { HttpContextToken, HttpInterceptor, HttpRequest, HttpResponseBase, HttpHandler, HttpHeaders, HttpEvent } from '@angular/common/http';
import { InMemoryCache, TypePolicies, ApolloLink, ApolloClient } from '@apollo/client';
import { ApolloBase, Apollo } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';
import { AuthConfig, OAuthService } from 'angular-oauth2-oidc';
import { TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { Observable, ObservableInput, ObservedValueOf } from 'rxjs';
import { AlainI18NService, ModalHelper, TitleService } from '@delon/theme';
import { DocumentNode } from 'graphql';
import { NzModalService, NzModalRef } from 'ng-zorro-antd/modal';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { FormGroup, FormControl, FormBuilder, FormArray, AbstractControlOptions } from '@angular/forms';
import { Router, Params, ActivatedRoute, Route, NavigationExtras, UrlTree, ActivatedRouteSnapshot, DetachedRouteHandle, RouteReuseStrategy, Data, NavigationEnd } from '@angular/router';
import { ACLService, ACLCanType } from '@delon/acl';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Location } from '@angular/common';
import { ReuseTabService, ReuseTabStrategy, provideReuseTabConfig } from '@delon/abc/reuse-tab';
import { LoadingService } from '@delon/abc/loading';
import { STChange, STData, STColumn, STExportOptions } from '@delon/abc/st';
import { List } from 'linqts-camelcase';
import { ToSignalOptions } from '@angular/core/rxjs-interop';
import { Maybe } from 'graphql/jsutils/Maybe';

declare const ExtensionModule: Record<string, unknown>;
type ExtensionModule = typeof ExtensionModule;
type GeexModule<TExtension = any> = {
    init: (force?: boolean) => Promise<unknown>;
} & TExtension;
interface UiModule extends GeexModule<{
    fullScreen: WritableSignal<boolean>;
    isMobile: Signal<boolean | undefined>;
    activeRoutedComponent?: unknown;
}> {
}
interface GeexModuleMap {
    ui: UiModule;
    [name: string]: GeexModule<any>;
}
type GeexModules<TExtensionModules extends Record<string, GeexModule> = {}> = {
    init: (force?: boolean) => Promise<{
        [K in keyof (GeexModuleMap & TExtensionModules)]: unknown;
    }>;
} & GeexModuleMap & TExtensionModules;
declare function createUiModule(_injector: Injector): UiModule;

declare function provideGeex<TExtensionModules extends Record<string, GeexModule> = {}>(overrides?: Partial<GeexModules>, extensions?: TExtensionModules): Provider[];

/**
 * Core meta-provide aligned with backend Geex.Common.
 * Installs geex signal modules. Delon page bases are opt-in via `provideGeexDelonBase()`.
 * Does not install admin business UI pages; use `geex add <name>` for source modules.
 */
declare function provideGeexCommon<TExtensionModules extends Record<string, GeexModule> = {}>(overrides?: Partial<GeexModules>, extensions?: TExtensionModules): Provider[];

type GeexTypePolicies = TypePolicies | Record<string, unknown>;
type GeexApolloTypePolicyContribution = () => GeexTypePolicies;
declare const GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS: InjectionToken<readonly GeexApolloTypePolicyContribution[]>;
declare function provideGeexApolloTypePolicies(contribution: GeexApolloTypePolicyContribution): EnvironmentProviders;
interface GeexApolloCacheOptions {
    possibleTypes?: Record<string, string[]>;
    typePolicies?: GeexTypePolicies;
    /** When true (default), merge with geexDefaultTypePolicies(). */
    includeDefaults?: boolean;
}
interface GeexApolloLinkOptions {
    baseUrl: string;
    httpLinkInstance: ApolloLink;
    extraLinks?: ApolloLink[];
}
interface GeexGraphqlErrorHandler {
    handleGraphQLErrors(params: {
        graphQLErrors?: ReadonlyArray<{
            message?: string;
            extensions?: any;
        }>;
        operation?: any;
        response?: {
            data?: any;
        } | null;
    }): void;
    handleGraphQLNetworkError(networkError: unknown): void;
    buildCommonHeaders?(): {
        [name: string]: string;
    };
}
declare const geexApolloDefaultOptions: {
    query: {
        fetchPolicy: "network-only";
        errorPolicy: "ignore";
    };
    mutate: {
        fetchPolicy: "no-cache";
        errorPolicy: "ignore";
    };
    watchQuery: {
        fetchPolicy: "cache-first";
        errorPolicy: "ignore";
    };
};
/** Core cache policies. Feature-specific policies are registered by extensions. */
declare function geexDefaultTypePolicies(): TypePolicies;
declare function createGeexInMemoryCache(options?: GeexApolloCacheOptions, contributions?: readonly GeexApolloTypePolicyContribution[]): InMemoryCache;
declare function createGeexUriLink(baseUrl: string): ApolloLink;
declare function createGeexHttpApolloOptions(options: GeexApolloLinkOptions & {
    cache: InMemoryCache;
}): ApolloClient.Options;
declare function isGeexSilentOperation(operation: {
    getContext: () => Record<string, unknown>;
}, silentToken?: HttpContextToken<boolean>): boolean;
declare function createGeexGraphqlErrorLink(handler: GeexGraphqlErrorHandler): ApolloLink;
declare function createGeexSilentContextLink(silentToken?: HttpContextToken<boolean>): ApolloLink;
declare function createGeexWsApolloOptions(options: {
    baseUrl?: string;
    url?: string;
    cache: InMemoryCache;
    connectionParams?: () => Promise<Record<string, string>> | Record<string, string>;
    retryAttempts?: number;
    onOpened?: () => void;
    onError?: (err: unknown) => void;
}): ApolloClient.Options;
/**
 * HttpLink with multipart upload support.
 * Uses peer `extract-files` by default; overrides via options.
 */
declare function createGeexUploadHttpLink(httpLink: HttpLink, options?: {
    withCredentials?: boolean;
    extractFilesFn?: (body: any, isExtractable: (v: any) => boolean) => any;
    isExtractableFile?: (value: any) => boolean;
}): ApolloLink;
declare const SilentApollo: InjectionToken<ApolloBase>;
declare const GEEX_APOLLO_CACHE: InjectionToken<InMemoryCache>;
interface ProvideGeexApolloOptions {
    baseUrl: string;
    possibleTypes?: Record<string, string[]>;
    typePolicies?: GeexTypePolicies;
    includeDefaultTypePolicies?: boolean;
    /**
     * Default true: multipart upload via `extract-files`.
     * Ignored when `createHttpLinkInstance` is set.
     */
    enableUpload?: boolean;
    /** Override default HttpLink factory (upload or plain). */
    createHttpLinkInstance?: (httpLink: HttpLink) => ApolloLink;
    errorHandler?: GeexGraphqlErrorHandler;
}
declare function provideGeexApollo(options: ProvideGeexApolloOptions): Provider[];

/** Startup-only orchestration. Module-owned keys live on their provideGeex* entrypoints. */
interface GeexStartupOptions {
    oauth: {
        getConfig: () => AuthConfig;
    };
    /** Forward host `environment.blockDebugger`. */
    blockDebugger?: boolean;
}
interface GeexSessionTerminatedCopy {
    title?: string;
    okText?: string;
}
interface GeexStartupI18nAdapter {
    merge(translations: object): void;
    use(lang: string): void;
}

declare const GEEX_STARTUP_OPTIONS: InjectionToken<GeexStartupOptions>;
declare const GEEX_EXCEPTION_500_PATH: InjectionToken<string>;
declare const GEEX_SESSION_TERMINATED_COPY: InjectionToken<GeexSessionTerminatedCopy>;

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
declare class GeexStartupService {
    private readonly options;
    private readonly injector;
    private readonly geex;
    private readonly oAuthService;
    private readonly aclService;
    private readonly settingsService;
    private readonly router;
    private readonly modalService;
    private readonly menuService;
    private readonly loginPath;
    private readonly afterLoginNavigate;
    private readonly superAdminUserId;
    private readonly exception500Url;
    private readonly sessionTerminatedCopy;
    private readonly defaultMenus;
    private readonly debuggerBlocker;
    private bootstrapPromise;
    private bootstrapped;
    private sessionWatchStarted;
    /** APP_INITIALIZER entry. Safe to call concurrently; runs the bootstrap pipeline once. */
    load(): Promise<void>;
    private bootstrap;
    private tryOidcCodeCallback;
    /** Fill tokenEndpoint gaps after discovery (or when discovery is unreachable). */
    private ensureOAuthTokenEndpoint;
    private bindUiSession;
    tryAutoOAuthLogin(): Promise<void>;
    private findMenuByLink;
    private findSystemConfigGroup;
    private resolveI18nAdapter;
    /** Safe read: guardedSignal throws before auth.init finishes. */
    private readAuthUser;
    /**
     * After OIDC, auth.init may have finished with a null user (token race) or still be settling.
     * Never throw EmptyError into bootstrap (that becomes the 500 page).
     */
    private resolveAuthUser;
    private trySwitchTenant;
    private ensureSessionWatch;
    static ɵfac: i0.ɵɵFactoryDeclaration<GeexStartupService, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<GeexStartupService>;
}

declare function provideGeexStartup(options: GeexStartupOptions): Array<Provider | EnvironmentProviders>;

/** Per-language ngx-translate dictionaries keyed by locale code (e.g. `zh-cn`). */
declare const GEEX_I18N_PACKS: InjectionToken<Record<string, Record<string, unknown>>>;
/** Well-known setting names for post-login localization (aligned with SettingDefinition). */
declare const GEEX_LOCALIZATION_DATA_SETTING = "LocalizationData";
declare const GEEX_LOCALIZATION_LANGUAGE_SETTING = "LocalizationLanguage";

/**
 * Nested i18n dictionary typing.
 * Leaf values keep the source type (so Go-to-Definition can reach pack literals).
 * Nested objects also expose runtime `get(key)` from kiwi attachGetter.
 */
type LangObject<O = Record<string, any>> = O extends object ? O extends (...args: never[]) => unknown ? O : {
    get(x: string, notFoundValue?: string): string;
} & {
    [K in keyof O]: LangObject<O[K]>;
} : O;

declare function mergeGeexI18nPacks<T extends Record<string, unknown>>(base: T, ...overlays: Array<Partial<T> | Record<string, unknown>>): T;

declare class GeexTranslateLoader implements TranslateLoader {
    private readonly packs;
    getTranslation(lang: string): Observable<TranslationObject>;
    static ɵfac: i0.ɵɵFactoryDeclaration<GeexTranslateLoader, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<GeexTranslateLoader>;
}

/** Mutable kiwi dictionary; host may re-export as `I18N`. */
declare let I18N: LangObject<any>;
/**
 * Alain + kiwi i18n runtime. Packs come from `GEEX_I18N_PACKS` (host zh-CN/en-US assembly).
 */
declare class GeexI18nService implements AlainI18NService {
    private _default;
    private change$;
    private kiwiLangs;
    private _langs;
    private readonly settings;
    private readonly nzI18nService;
    private readonly delonLocaleService;
    private readonly translate;
    private readonly packs;
    constructor();
    /** Current kiwi dictionary (also mirrored by module `I18N`). */
    get dictionary(): LangObject<any>;
    private getDefaultLang;
    private updateLangData;
    get change(): Observable<string>;
    merge(translations: object): void;
    use(lang: string): void;
    getLangs(): Array<{
        code: string;
        text: string;
        abbr: string;
    }>;
    fanyi(key: string, interpolateParams?: {}): any;
    get defaultLang(): string;
    get currentLang(): string;
    static ɵfac: i0.ɵɵFactoryDeclaration<GeexI18nService, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<GeexI18nService>;
}

interface GeexI18nProvideOptions {
    fallbackLang?: string;
}
/**
 * Register kiwi packs + GeexI18nService + Alain/ngx-translate wiring.
 */
declare function provideGeexI18n(packs: Record<string, Record<string, unknown>>, options?: GeexI18nProvideOptions): Array<Provider | EnvironmentProviders>;

declare const GEEX_CANCEL_AUTHENTICATION_DOCUMENT: InjectionToken<DocumentNode>;
/** Header profile route (default `/identity/me`). */
declare const GEEX_PROFILE_PATH: InjectionToken<string>;
/** Header profile menu label (default 个人中心). */
declare const GEEX_PROFILE_LABEL: InjectionToken<string>;

declare const cancelAuthenticationMutation: DocumentNode;
declare class GeexAuthLogout {
    private readonly apollo;
    private readonly oauth;
    private readonly settings;
    private readonly acl;
    private readonly router;
    private readonly loginPath;
    private readonly afterLoginNavigate;
    private readonly cancelDocument;
    logout(): Promise<void>;
    static ɵfac: i0.ɵɵFactoryDeclaration<GeexAuthLogout, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<GeexAuthLogout>;
}

type GeexEnvironmentOverridesOptions = {
    url?: string;
    onInvalid?: (override: unknown, url: string) => void;
    onLoadError?: (error: unknown, url: string) => void;
};
/** Deep-set env from flat / dotted override keys. Host may pass typed `environment` objects. */
declare function applyEnvironmentOverrides(env: object, override: Record<string, unknown>): void;
/**
 * Loads `/assets/environment.override.js` (or options.url) and merges into env.
 * Missing / invalid override is non-fatal.
 */
declare function loadEnvironmentOverrides(env: object, options?: GeexEnvironmentOverridesOptions): Promise<void>;

declare global {
    interface Window {
        clearHistory(): void;
    }
    function clearHistory(): void;
}

/** Mark HTTP / GraphQL ops that should not show error UI. */
declare const SILENT_REQUEST: HttpContextToken<boolean>;
declare const GEEX_DEFAULT_HTTP_STATUS_MESSAGES: {
    [key: number]: string;
};
/** Override status → message map (defaults to GEEX_DEFAULT_HTTP_STATUS_MESSAGES). */
declare const GEEX_HTTP_STATUS_MESSAGES: InjectionToken<{
    [key: number]: string;
}>;
/** Login route after 401 (default `/authentication/login`). */
declare const GEEX_LOGIN_PATH: InjectionToken<string>;
/** Called after navigating to login. Defaults to `window.clearHistory`. */
declare const GEEX_AFTER_LOGIN_NAVIGATE: InjectionToken<() => void>;
/** API base URL for relative HTTP requests (host `environment.api.baseUrl`). */
declare const GEEX_API_BASE_URL: InjectionToken<string>;

/**
 * Default Geex HTTP interceptor (zh-CN messages, `/authentication/login`, tenant/Bearer headers).
 * Override via tokens or protected hooks; host may `extends` or provide callbacks.
 */
declare class GeexHttpInterceptor implements HttpInterceptor {
    protected injector: Injector;
    protected oauthService: OAuthService;
    protected modalSrv: NzModalService;
    protected statusMessages: {
        [key: number]: string;
    };
    protected loginPath: string;
    protected afterLoginNavigate: () => void;
    protected apiBaseUrl: string;
    private loginTrigger$;
    private loginModal$;
    constructor();
    protected get notification(): NzNotificationService;
    protected buildLoginConfirmOptions(): {
        nzTitle: string;
    };
    protected goTo(url: string): void;
    protected isSilentRequest(req: HttpRequest<any>): boolean;
    protected shouldAttachTenant(): boolean;
    protected notifyHttpError(status: number, text: string): void;
    protected onUnauthorized(): void;
    protected checkStatus(ev: HttpResponseBase, silent: boolean): void;
    /** Status-branch template method; override for custom 200/403/exception routing. */
    protected handleHttpStatus(ev: HttpResponseBase, silent: boolean): void;
    protected handleData(ev: HttpResponseBase, _req: HttpRequest<any>, _next: HttpHandler): Observable<any>;
    buildCommonHeaders(headers?: HttpHeaders): {
        [name: string]: string;
    };
    handleGraphQLErrors(params: {
        graphQLErrors?: ReadonlyArray<{
            message?: string;
            locations?: any;
            path?: ReadonlyArray<string | number>;
            extensions?: any;
        }>;
        operation?: any;
        response?: {
            data?: any;
        } | null;
    }): void;
    handleGraphQLNetworkError(networkError: unknown): void;
    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>>;
    static ɵfac: i0.ɵɵFactoryDeclaration<GeexHttpInterceptor, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<GeexHttpInterceptor>;
}

interface GeexHttpProvideOptions {
    apiBaseUrl: string;
}
declare function provideGeexHttp(options: GeexHttpProvideOptions): Provider[];

interface GeexModuleContributionContext {
    readonly injector: Injector;
    readonly modules: Readonly<Record<string, GeexModule>>;
}
interface GeexModuleContribution {
    readonly createModules: (context: GeexModuleContributionContext) => Readonly<Record<string, GeexModule>>;
}
declare const GEEX_MODULE_CONTRIBUTIONS: InjectionToken<readonly GeexModuleContribution[]>;
declare function provideGeexModuleContribution(contribution: GeexModuleContribution): EnvironmentProviders;

type GeexOverrides<TExtensionModules extends Record<string, GeexModule> = Record<string, GeexModule>> = Partial<Omit<GeexModules<TExtensionModules>, "init">>;
type GeexExtensions<TExtensionModules extends Record<string, GeexModule> = Record<string, GeexModule>> = Partial<TExtensionModules>;
declare let geex: GeexModules;
declare let Geex: InjectionToken<GeexModules>;
declare function configGeex<TExtensionModules extends Record<string, GeexModule> = Record<string, never>>(injector: Injector, overrides?: GeexOverrides<TExtensionModules>, contributions?: readonly GeexModuleContribution[]): void;

declare function guardedSignal<T>(innerSignal: WritableSignal<T>, isInitialized: () => boolean): WritableSignal<T>;
declare function guardedSignal<T>(innerSignal: Signal<T>, isInitialized: () => boolean): Signal<T>;

/** Minimal menu shape shared by host Delon menus and extension plugins. */
interface GeexMenuItem {
    text?: string;
    link?: string;
    icon?: string | object;
    group?: boolean;
    hideInBreadcrumb?: boolean;
    open?: boolean;
    children?: GeexMenuItem[];
    [key: string]: unknown;
}
interface GeexMenuContributionContext {
    id: string;
}
/**
 * Multi-provider token for additive menu contributions.
 * Host should merge contributions into app menus with a single MenuService.add([...app, ...extras]).
 */
interface GeexMenuContribution {
    resolve(user: GeexMenuContributionContext): Promise<GeexMenuItem[]>;
}
declare const GEEX_MENU_CONTRIBUTIONS: InjectionToken<GeexMenuContribution[]>;
/** Host-composed default menus (e.g. from module-registry). */
declare const GEEX_DEFAULT_MENUS: InjectionToken<GeexMenuItem[]>;
declare function provideGeexMenus(menus: GeexMenuItem[]): Provider[];

/** Well-known setting names for post-login app/menu bind (aligned with SettingDefinition). */
declare const GEEX_APP_NAME_SETTING = "AppAppName";
declare const GEEX_APP_MENU_SETTING = "AppAppMenu";

/** When true, DebuggerBlockerService activates anti-devtools measures. */
declare const GEEX_BLOCK_DEBUGGER: InjectionToken<boolean>;

declare class DebuggerBlockerService {
    private readonly enabled;
    init(): void;
    private blockDebugger;
    private disableDevToolsShortcuts;
    static ɵfac: i0.ɵɵFactoryDeclaration<DebuggerBlockerService, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<DebuggerBlockerService>;
}

interface IdentityClaims {
    nbf: number;
    exp: number;
    iss: string;
    aud: string;
    nonce: string;
    iat: number;
    at_hash: string;
    s_hash: string;
    sid: string;
    sub: string;
    auth_time: number;
    idp: string;
    amr: string[];
    picture: string;
    role: string;
    email: string;
    phone_number: string;
    name: string;
    nickname: string;
    __tenant: string;
    preferred_username: string;
    email_verified: boolean;
    login_provider: string;
}
declare module "angular-oauth2-oidc" {
    interface OAuthService {
        getIdentityClaims(this: this): IdentityClaims;
    }
}

/**
 * Host-augmentable i18n dictionary shape.
 * Apps should `declare module "@geexcode/geex-angular" { interface GeexI18n extends ... {} }`
 * (typically in module-registry) so `inject(GEEX_I18N)` / `BusinessComponentBase.I18N` stay typed.
 */
interface GeexI18n {
}
/**
 * Host provides AlainI18NService-compatible instance (e.g. `GeexI18nService`).
 */
declare const GEEX_I18N_SERVICE: InjectionToken<unknown>;
/**
 * Host-augmentable AppPermission map / enum object.
 * Apps should augment `GeexAppPermission` from generated `AppPermission`.
 */
interface GeexAppPermission {
}
/**
 * Typed kiwi/i18n dictionary (augment `GeexI18n` in the host app).
 */
declare const GEEX_I18N: InjectionToken<GeexI18n>;
/**
 * Typed AppPermission enum/map (augment `GeexAppPermission` in the host app).
 */
declare const GEEX_APP_PERMISSION: InjectionToken<GeexAppPermission>;

/** Loose entity / DTO shape used by page bases (host `Hint<T>`-compatible). */
type GeexHint<T> = T & Record<string, any>;
/**
 * Host-compatible typed FormGroup (mirrors admin `TypedFormGroup` augmentation).
 * Keeps `controls.*` as FormControl for template `[formControl]` bindings.
 */
type GeexTypedFormGroup<TValue> = FormGroup & {
    controls: {
        [K in keyof TValue]: FormControl<any>;
    };
    value: TValue;
};

declare abstract class BusinessComponentBase<TParam = any> {
    protected acl: ACLService;
    protected apollo: Apollo;
    protected i18n: unknown;
    protected modal: ModalHelper;
    protected msgSrv: NzMessageService;
    protected nzModalSrv: NzModalService;
    protected router: Router;
    params: WritableSignal<TParam>;
    I18N: GeexI18n;
    AppPermission: GeexAppPermission;
    can(permission: ACLCanType): boolean;
    static ɵfac: i0.ɵɵFactoryDeclaration<BusinessComponentBase<any>, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<BusinessComponentBase<any>>;
}

type ParamMappingValue<TValue> = {
    position: "pathParams" | "queryParams" | "fragment";
    default?: TValue;
};
type RouteParamsMappings<TParams extends {}> = {
    [Key in keyof TParams]: ParamMappingValue<TParams[Key]>;
};
type RouteParams = {
    pathParams?: Params;
    queryParams?: Params;
    fragment?: string;
};
declare abstract class RoutedComponent<TParams extends {}> extends BusinessComponentBase<TParams> {
    abstract routeParamsMappings: RouteParamsMappings<TParams>;
    defaultParams: TParams;
    paramsForm: GeexTypedFormGroup<TParams>;
    params: WritableSignal<TParams>;
    title: i0.ExtendedWritableSignal<string | undefined>;
    protected cdr: ChangeDetectorRef;
    protected fb: FormBuilder;
    protected loading: i0.ExtendedWritableSignal<boolean>;
    protected loadingSrv: LoadingService;
    protected location: Location;
    protected reuseTabSrv: ReuseTabService;
    protected route: ActivatedRoute;
    protected titleSrv: TitleService;
    ngOnInit(): Promise<void>;
    constructor();
    /** Full route-reload pipeline; override to replace navigation side effects. */
    protected handleRouteReload(): Promise<void>;
    protected beforeOnRouted(_params: TParams): void | Promise<void>;
    protected afterOnRouted(_params: TParams): void | Promise<void>;
    protected buildParamsForm(defaults: TParams): GeexTypedFormGroup<TParams>;
    protected decodeQueryParam(raw: string): unknown;
    resolve({ pathParams, queryParams, fragment }: RouteParams): Promise<TParams>;
    refresh(): void;
    reset(): void;
    abstract onRouted(params: TParams): void | Promise<void>;
    paramsToRouteParams(params: TParams): RouteParams;
    protected isEqualToDefault(value: any, defaultValue: any): boolean;
    static ɵfac: i0.ɵɵFactoryDeclaration<RoutedComponent<any>, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<RoutedComponent<any>>;
}

type BatchOperationName = "delete" | "approve" | "submit" | "unApprove" | "unSubmit";
declare class ListPageParams<T> {
    pi?: number;
    ps?: number;
    /** Host GraphQL sort input; kept loose to match module-specific SortInput types. */
    sort?: any;
}
declare abstract class RoutedListComponent<TParams extends ListPageParams<TDto>, TDto extends GeexHint<{
    id?: string;
}>> extends RoutedComponent<TParams> {
    data: i0.ExtendedWritableSignal<GeexHint<TDto>[]>;
    abstract columns?: any;
    total: i0.ExtendedWritableSignal<number>;
    selectedData: i0.ExtendedWritableSignal<GeexHint<TDto>[]>;
    allSelected: i0.Signal<boolean>;
    onAllChecked(value: boolean): void;
    onItemChecked(data: TDto, checked: boolean): void;
    tableChange(args: STChange): Promise<void>;
    protected onTableCheckbox(args: STChange): void;
    protected onTablePage(args: STChange): void;
    protected onTableSort(args: STChange): void;
    batchOperation(operation: BatchOperationName, entityType: string, remark?: string): Promise<boolean>;
    protected filterBatchIds(operation: BatchOperationName, selectedData: Array<GeexHint<TDto>>): {
        ids: any[];
        error?: string;
    };
    protected buildBatchMutation(operation: BatchOperationName, entityType: string): string;
    protected confirmBatch(operation: BatchOperationName, apiName: string, ids: any[], remark?: string): Promise<boolean>;
    static ɵfac: i0.ɵɵFactoryDeclaration<RoutedListComponent<any, any>, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<RoutedListComponent<any, any>, "ng-component", never, {}, {}, never, never, true, never>;
}

declare abstract class RoutedEditComponent<TParams extends GeexHint<{
    id?: string;
}>, TEntity extends GeexHint<{
    id?: string;
}>, TEditSchema extends GeexHint<Partial<TEntity>>> extends RoutedComponent<TParams> {
    entity?: GeexHint<TEntity>;
    entityForm?: GeexTypedFormGroup<TEditSchema>;
    originalValue?: TEditSchema;
    close(): Promise<void>;
    closableCheck(): Promise<boolean>;
    protected isEntityDirty(): boolean;
    protected unsavedConfirmTitle(): string;
    back(reload?: boolean): Promise<void>;
    static ɵfac: i0.ɵɵFactoryDeclaration<RoutedEditComponent<any, any, any>, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<RoutedEditComponent<any, any, any>, "ng-component", never, {}, {}, never, never, true, never>;
}

/**
 * Base for components opened inside nz-modal.
 * NzModalRef is required; only use for modal-hosted components.
 */
declare abstract class ModalComponentBase {
    title: string;
    loading: boolean;
    protected nzModalRef: NzModalRef<any, any>;
    success(result?: any): void;
    close(_$event?: MouseEvent): void;
    /** Hook after modal closes; override for cleanup / analytics. */
    protected afterClose(_result?: unknown): void;
}

declare abstract class TreeTableComponentBase<ITreeNode = any> {
    mapOfExpandedData: {
        [key: string]: ITreeNode[];
    };
    I18N: GeexI18n;
    protected getNodeKey(node: ITreeNode): string;
    protected getNodeChildren(node: ITreeNode): ITreeNode[] | undefined;
    collapse(array: ITreeNode[], data: ITreeNode, $event: boolean): void;
    convertTreeToList(root: ITreeNode): ITreeNode[];
    visitNode(node: ITreeNode, hashMap: {
        [key: string]: boolean;
    }, array: ITreeNode[]): void;
    static ɵfac: i0.ɵɵFactoryDeclaration<TreeTableComponentBase<any>, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<TreeTableComponentBase<any>, "ng-component", never, {}, {}, never, never, true, never>;
}

declare class GeexRouter extends Router {
    lastRoute: Route;
    constructor(injector: Injector);
    private isDifferentPath;
    createUrlTree(commands: any[], navigationExtras?: NavigationExtras): UrlTree;
    static ɵfac: i0.ɵɵFactoryDeclaration<GeexRouter, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<GeexRouter>;
}

/**
 * Hardens Delon ReuseTabStrategy against undefined snapshots / empty-path leaves
 * that otherwise throw during createRouterState / outlet.detach (NG04012).
 */
declare class GeexReuseTabStrategy extends ReuseTabStrategy {
    shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean;
    shouldDetach(route: ActivatedRouteSnapshot): boolean;
    retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle | null;
    shouldAttach(route: ActivatedRouteSnapshot): boolean;
    static ɵfac: i0.ɵɵFactoryDeclaration<GeexReuseTabStrategy, never>;
    static ɵprov: i0.ɵɵInjectableDeclaration<GeexReuseTabStrategy>;
}

declare class ListPageLayoutComponent {
    private i18n;
    title: i0.InputSignal<string>;
    loading: i0.InputSignal<boolean>;
    total: i0.InputSignal<number>;
    data: i0.InputSignal<STData[]>;
    columns: i0.InputSignal<STColumn<any>[]>;
    pi: i0.InputSignal<number>;
    ps: i0.InputSignal<number>;
    selectedCount: i0.InputSignal<number>;
    multiSort: i0.InputSignal<boolean>;
    filtersInHeader: i0.InputSignal<boolean>;
    tableChange: i0.OutputEmitterRef<STChange<any>>;
    refresh: i0.OutputEmitterRef<void>;
    headerExtraTpl: i0.Signal<TemplateRef<void> | undefined>;
    headerTabTpl: i0.Signal<TemplateRef<void> | undefined>;
    headerActionTpl: i0.Signal<TemplateRef<void> | undefined>;
    get selectedLabel(): string;
    get selectedUnitLabel(): string;
    get refreshLabel(): string;
    static ɵfac: i0.ɵɵFactoryDeclaration<ListPageLayoutComponent, never>;
    static ɵcmp: i0.ɵɵComponentDeclaration<ListPageLayoutComponent, "list-page-layout", never, { "title": { "alias": "title"; "required": true; "isSignal": true; }; "loading": { "alias": "loading"; "required": false; "isSignal": true; }; "total": { "alias": "total"; "required": false; "isSignal": true; }; "data": { "alias": "data"; "required": false; "isSignal": true; }; "columns": { "alias": "columns"; "required": false; "isSignal": true; }; "pi": { "alias": "pi"; "required": false; "isSignal": true; }; "ps": { "alias": "ps"; "required": false; "isSignal": true; }; "selectedCount": { "alias": "selectedCount"; "required": false; "isSignal": true; }; "multiSort": { "alias": "multiSort"; "required": false; "isSignal": true; }; "filtersInHeader": { "alias": "filtersInHeader"; "required": false; "isSignal": true; }; }, { "tableChange": "tableChange"; "refresh": "refresh"; }, ["headerExtraTpl", "headerTabTpl", "headerActionTpl"], ["[filters]", "[filters]", "[toolbar]"], true, never>;
}

type GeexReuseTabOptions = NonNullable<Parameters<typeof provideReuseTabConfig>[0]>;
interface GeexDelonProvideOptions {
    router?: Type<Router>;
    reuseStrategy?: Type<RouteReuseStrategy>;
    appPermission?: GeexAppPermission;
    reuseTab?: GeexReuseTabOptions;
}
/**
 * Delon-coupled Core providers (Router subclass + ReuseTab + optional AppPermission).
 */
declare function provideGeexDelonBase(options?: GeexDelonProvideOptions): Array<Provider | EnvironmentProviders>;

declare global {
    interface ReadonlyArray<T> {
        any(): boolean;
        any(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): boolean;
        any(predicate?: any): any;
        first(): T;
        first(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T;
        first(predicate?: any): any;
        firstOrDefault(defaultValue?: Partial<T>): T;
        firstOrDefault(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean, defaultValue?: Partial<T>): T;
        firstOrDefault(predicate?: any, defaultValue?: Partial<T>): any;
    }
    interface Array<T> {
        add(element: T): void;
        addRange(elements: T[]): void;
        aggregate<U>(accumulator: (accum: U, value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => any, initialValue?: U | undefined): any;
        all(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): boolean;
        any(): boolean;
        clear(): void;
        any(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): boolean;
        any(predicate?: any): any;
        average(): number;
        average(transform: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => any): number;
        average(transform?: any): any;
        contains(element: T): boolean;
        count(): number;
        count(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): number;
        count(predicate?: any): any;
        defaultIfEmpty(defaultValue?: T | undefined): T[];
        distinct(): T[];
        distinctBy(keySelector: (key: T) => string | number): T[];
        elementAt(index: number): T;
        toArray(): T[];
        elementAtOrDefault(index: number): T | undefined;
        except(source: T[]): T[];
        first(): T;
        first(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T;
        first(predicate?: any): any;
        firstOrDefault(defaultValue?: Partial<T>): T;
        firstOrDefault(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean, defaultValue?: Partial<T>): T;
        firstOrDefault(predicate?: any, defaultValue?: Partial<T>): any;
        groupBy<TResult = T>(grouper: (key: T) => string | number, mapper?: ((element: T) => TResult) | undefined): {
            [key: string]: TResult[];
        };
        groupJoin<U, R>(list: List<U>, key1: (k: T) => any, key2: (k: U) => any, result: (first: T, second: List<U>) => any): R[];
        insert(index: number, element: T): void | Error;
        intersect(source: T[]): T[];
        linqJoin<U, R>(list: List<U>, key1: (key: T) => any, key2: (key: U) => any, result: (first: T, second: U) => R): R[];
        last(): T;
        last(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T;
        last(predicate?: any): any;
        lastOrDefault(): T;
        lastOrDefault(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T;
        lastOrDefault(predicate?: any): any;
        max(): number;
        max(selector: (value: T, index: number, array: T[]) => number): number;
        max(selector?: any): any;
        min(): number;
        min(selector: (value: T, index: number, array: T[]) => number): number;
        min(selector?: any): any;
        ofType<U>($type: any): List<U>;
        orderBy(keySelector: (key: T) => any, comparer?: ((a: T, b: T) => number) | undefined): T[];
        orderByDescending(keySelector: (key: T) => any, comparer?: ((a: T, b: T) => number) | undefined): T[];
        thenBy(keySelector: (key: T) => any): T[];
        thenByDescending(keySelector: (key: T) => any): T[];
        remove(element: T): boolean;
        removeAll(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T[];
        removeAt(index: number): void;
        selectMany<TOut extends any[]>(selector: (element: T, index: number) => TOut): TOut;
        sequenceEqual(list: T[]): boolean;
        single(predicate?: ((value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean) | undefined): T;
        singleOrDefault(predicate?: ((value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean) | undefined): T;
        skip(amount: number): T[];
        skipWhile(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T[];
        sum(): number;
        sum(transform: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => number): number;
        sum(transform?: any): any;
        take(amount: number): T[];
        takeWhile(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T[];
        toDictionary<TKey>(key: (key: T) => TKey): List<{
            Key: TKey;
            Value: T;
        }>;
        toDictionary<TKey, TValue>(key: (key: T) => TKey, value: (value: T) => TValue): List<{
            Key: TKey;
            Value: T | TValue;
        }>;
        toDictionary(key: any, value?: any): any;
        toLookup<TResult>(keySelector: (key: T) => string | number, elementSelector: (element: T) => TResult): {
            [key: string]: TResult[];
        };
        union(list: T[]): T[];
        where(predicate: (value?: T | undefined, index?: number | undefined, list?: T[] | undefined) => boolean): T[];
        zip<U, TOut>(list: U[], result: (first: T, second: U) => TOut): TOut[];
    }
}

declare global {
    interface String {
        contains(this: string, value: string): boolean;
    }
}

declare module "@angular/core" {
    interface SignalExtensions<T> {
        toDebounced(debounceTimeInMs: number): ExtendedSignal<T>;
        toObservable(): Observable<T>;
    }
    type ExtendedSignal<T> = Signal<T> & SignalExtensions<T> & {
        toDeepSignal(): DeepSignal<T>;
    };
    type ExtendedWritableSignal<T> = WritableSignal<T> & SignalExtensions<T> & {
        toDeepSignal(): WritableDeepSignal<T>;
    };
    function signal<T>(initialValue: T, options?: CreateSignalOptions<T>): ExtendedWritableSignal<T>;
}
type IsRecord<T> = T extends object ? T extends unknown[] ? false : T extends Set<unknown> ? false : T extends Map<unknown, unknown> ? false : T extends Function ? false : true : false;
type IsUnknownRecord<T> = string extends keyof T ? true : number extends keyof T ? true : false;
type IsKnownRecord<T> = IsRecord<T> extends true ? (IsUnknownRecord<T> extends true ? false : true) : false;
type DeepSignal<T> = ExtendedSignal<T> & (IsKnownRecord<T> extends true ? Readonly<{
    [K in keyof T]: IsKnownRecord<T[K]> extends true ? DeepSignal<T[K]> : ExtendedSignal<T[K]>;
}> : unknown);
type WritableDeepSignal<T> = Signal<T> & WritableSignal<T> & (IsKnownRecord<T> extends true ? Readonly<{
    [K in keyof T]: IsKnownRecord<T[K]> extends true ? WritableDeepSignal<T[K]> : WritableSignal<T[K]>;
}> : unknown);
declare function deepSignal<T>(initialValue: T, options?: CreateSignalOptions<T>): WritableDeepSignal<T>;
declare function computedAsync<T>(computation: () => Observable<T> | Promise<T> | T | undefined | null): Signal<T | null>;

declare module "rxjs" {
    interface Observable<T> {
        lastValuePromise(this: this): Promise<T | undefined>;
        firstValuePromise(this: this): Promise<T | undefined>;
        toSignal(this: this, options?: ToSignalOptions<T> & {
            deep?: false;
            initialValue?: T;
            requireSync?: false;
        }): ExtendedSignal<T>;
        toSignal(this: this, options?: ToSignalOptions<T> & {
            deep?: true;
            initialValue?: T;
            requireSync?: false;
        }): DeepSignal<T>;
        pipeMap<R>(this: this, project: (value: T, index: number) => R | undefined): Observable<R | undefined>;
        pipeSwitchMap<R extends ObservableInput<any>>(this: this, project: (value: T, index: number) => R): Observable<ObservedValueOf<R>>;
        pipeFilter(this: this, predicate: (value: T, index: number) => boolean): Observable<T | undefined>;
    }
}

declare module "@angular/router" {
    interface ActivatedRouteSnapshot {
        getResolvedUrl(): string;
        getConfiguredUrl(): string;
        getDeepestRouteConfig(): GeexRoute | null;
    }
    interface RouteData extends Data {
        singleton?: boolean;
        reuse?: boolean;
        title?: string;
    }
    interface NavigationBehaviorOptions {
        forceReload?: boolean;
    }
    type GeexRoutes = GeexRoute[];
    interface GeexRoute extends Route {
        data?: RouteData;
    }
    interface Router {
        navigationReload: WritableSignal<NavigationEnd | undefined>;
    }
}
declare module "@angular/forms" {
    type IsArray<TValue> = TValue extends Array<any> ? true : false;
    type IsObject<TValue> = TValue extends object ? (TValue extends Array<any> ? false : TValue extends Date ? false : true) : false;
    type TypedAbstractControl<TValue> = IsArray<TValue> extends true ? TypedFormArray<TValue extends Array<infer U> ? U : never> & FormControl<TValue> : IsObject<TValue> extends true ? TypedFormGroup<TValue> & FormControl<TValue> : FormControl<TValue>;
    interface TypedFormGroup<TValue> extends FormGroup {
        controls: {
            [K in keyof TValue]: TypedAbstractControl<TValue[K]>;
        };
        value: TValue;
    }
    interface TypedFormArray<TValue> extends FormArray {
        controls: Array<TypedAbstractControl<TValue>>;
        value: Array<TValue>;
    }
    type ValueOrTypedAbstractControl<T> = {
        [key in keyof T]: FormControl<T[key]> | ValueOrTypedAbstractControl<T[key]>;
    };
    interface FormBuilder {
        build<T>(controls: ValueOrTypedAbstractControl<T>, options?: AbstractControlOptions | null): TypedFormGroup<T>;
    }
}

declare module "@delon/abc/st" {
    interface STComponent {
        exportAll: (opt?: STExportOptions) => Promise<void>;
        $exportData: Observable<any[]>;
    }
}

declare function extract<TActual = any, T = any>(object: TActual, properties: Partial<Record<keyof (T | TActual), true>>): Partial<T>;
declare global {
    type Hint<T> = Maybe<T>;
    function extract<TActual = any, T = any>(object: TActual, properties: Partial<Record<keyof (T | TActual), true>>): Partial<T>;
    interface Date {
        add: (value: {
            years?: number;
            months?: number;
            weeks?: number;
            days?: number;
            hours?: number;
            minutes?: number;
            seconds?: number;
            milliseconds?: number;
        }) => Date;
        format(format: string): string;
        getTotalMonth(): number;
    }
    interface Array<T> {
        flatMapDeep<U>(this: this, iteratee: (value: T, index: number, array: T[]) => U[], thisArg?: any): U[];
    }
    interface ArrayConstructor {
        range(start: number, end: number): number[];
    }
    interface Number {
        hasFlag(...flags: number[]): boolean;
        hasNoFlag(...flags: number[]): boolean;
    }
    interface String {
        trimEnd(strToTrim: string): string;
    }
}
declare function assertIsDefined<T>(val: T): asserts val is NonNullable<T>;
declare function assertIsArray<T>(val: T | T[]): asserts val is T[];
declare function assert<T>(_val: any): asserts _val is T;
declare function assertIsNotArray<T>(val: T | T[]): asserts val is T;
type PropertyAccessType = "create" | "modify" | "delete" | "assignment";
declare function deepProxy<T>(obj: T, callback: (type: PropertyAccessType, param: {
    target: any;
    key: string | number | symbol;
    value: any;
}) => void): any;
declare function isRecord<TValue = any>(value: unknown): value is Record<string, TValue>;

declare global {
    interface Blob {
        computeChecksumMd5(this: Blob): Promise<string>;
    }
}

declare function provideGeexExtensions(): void;

declare const GEEX_MOBILE_PATH_SUFFIX: InjectionToken<string>;

declare const GEEX_SUPER_ADMIN_USER_ID: InjectionToken<string>;

declare const GEEX_EXCEPTION_403_PROFILE_PATH: InjectionToken<string>;
declare const GEEX_EXCEPTION_403_PROFILE_LABEL: InjectionToken<string>;
declare const GEEX_EXCEPTION_LOGIN_PATH: InjectionToken<string>;

declare global {
    var geex: typeof geex;
    interface Window {
        geex: typeof geex;
    }
}
/**
 * Expose package `geex` on globalThis/window via a live getter.
 * Must not copy the value at bind time — `geex` is only assigned inside `configGeex`.
 */
declare function bindGeexGlobal(): void;

declare const exports$1: Record<string, any>;

export { BusinessComponentBase, DebuggerBlockerService, ExtensionModule, GEEX_AFTER_LOGIN_NAVIGATE, GEEX_API_BASE_URL, GEEX_APOLLO_CACHE, GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS, GEEX_APP_MENU_SETTING, GEEX_APP_NAME_SETTING, GEEX_APP_PERMISSION, GEEX_BLOCK_DEBUGGER, GEEX_CANCEL_AUTHENTICATION_DOCUMENT, GEEX_DEFAULT_HTTP_STATUS_MESSAGES, GEEX_DEFAULT_MENUS, GEEX_EXCEPTION_403_PROFILE_LABEL, GEEX_EXCEPTION_403_PROFILE_PATH, GEEX_EXCEPTION_500_PATH, GEEX_EXCEPTION_LOGIN_PATH, GEEX_HTTP_STATUS_MESSAGES, GEEX_I18N, GEEX_I18N_PACKS, GEEX_I18N_SERVICE, GEEX_LOCALIZATION_DATA_SETTING, GEEX_LOCALIZATION_LANGUAGE_SETTING, GEEX_LOGIN_PATH, GEEX_MENU_CONTRIBUTIONS, GEEX_MOBILE_PATH_SUFFIX, GEEX_MODULE_CONTRIBUTIONS, GEEX_PROFILE_LABEL, GEEX_PROFILE_PATH, GEEX_SESSION_TERMINATED_COPY, GEEX_STARTUP_OPTIONS, GEEX_SUPER_ADMIN_USER_ID, Geex, GeexAuthLogout, GeexHttpInterceptor, GeexI18nService, GeexReuseTabStrategy, GeexRouter, GeexStartupService, GeexTranslateLoader, I18N, GeexI18nService as I18NService, ListPageLayoutComponent, ListPageParams, ModalComponentBase, RoutedComponent, RoutedEditComponent, RoutedListComponent, SILENT_REQUEST, SilentApollo, TreeTableComponentBase, applyEnvironmentOverrides, assert, assertIsArray, assertIsDefined, assertIsNotArray, bindGeexGlobal, cancelAuthenticationMutation, computedAsync, configGeex, createGeexGraphqlErrorLink, createGeexHttpApolloOptions, createGeexInMemoryCache, createGeexSilentContextLink, createGeexUploadHttpLink, createGeexUriLink, createGeexWsApolloOptions, createUiModule, deepProxy, deepSignal, extract, geex, geexApolloDefaultOptions, geexDefaultTypePolicies, guardedSignal, isGeexSilentOperation, isRecord, loadEnvironmentOverrides, mergeGeexI18nPacks, provideGeex, provideGeexApollo, provideGeexApolloTypePolicies, provideGeexCommon, provideGeexDelonBase, provideGeexExtensions, provideGeexHttp, provideGeexI18n, provideGeexMenus, provideGeexModuleContribution, provideGeexStartup, exports$1 as rison };
export type { BatchOperationName, DeepSignal, GeexApolloCacheOptions, GeexApolloLinkOptions, GeexApolloTypePolicyContribution, GeexAppPermission, GeexDelonProvideOptions, GeexEnvironmentOverridesOptions, GeexExtensions, GeexGraphqlErrorHandler, GeexHint, GeexHttpProvideOptions, GeexI18n, GeexI18nProvideOptions, GeexMenuContribution, GeexMenuContributionContext, GeexMenuItem, GeexModule, GeexModuleContribution, GeexModuleContributionContext, GeexModuleMap, GeexModules, GeexOverrides, GeexSessionTerminatedCopy, GeexStartupI18nAdapter, GeexStartupOptions, GeexTypePolicies, GeexTypedFormGroup, IdentityClaims, LangObject, PropertyAccessType, ProvideGeexApolloOptions, RouteParams, RouteParamsMappings, UiModule, WritableDeepSignal };
//# sourceMappingURL=index.d.ts.map
