import { HttpContext, HttpContextToken } from "@angular/common/http";
import { EnvironmentProviders, InjectionToken, Provider, inject, makeEnvironmentProviders } from "@angular/core";
import { ApolloClient, ApolloLink, CombinedGraphQLErrors, InMemoryCache, type TypePolicies } from "@apollo/client";
import { ErrorLink } from "@apollo/client/link/error";
import { GraphQLWsLink } from "@apollo/client/link/subscriptions";
import { Apollo, ApolloBase, APOLLO_NAMED_OPTIONS, APOLLO_OPTIONS, NamedOptions } from "apollo-angular";
import { HttpLink } from "apollo-angular/http";
import extractFiles from "extract-files/extractFiles.mjs";
import isExtractableFile from "extract-files/isExtractableFile.mjs";
import { createClient } from "graphql-ws";
import json5 from "json5";

import { GeexHttpInterceptor } from "./http/geex-http.interceptor";
import { SILENT_REQUEST } from "./http/tokens";

export type GeexTypePolicies = TypePolicies | Record<string, unknown>;
export type GeexApolloTypePolicyContribution = () => GeexTypePolicies;

export const GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS = new InjectionToken<
  readonly GeexApolloTypePolicyContribution[]
>("GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS");

export function provideGeexApolloTypePolicies(
  contribution: GeexApolloTypePolicyContribution,
): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS,
      multi: true,
      useValue: contribution,
    },
  ]);
}

export interface GeexApolloCacheOptions {
  possibleTypes?: Record<string, string[]>;
  typePolicies?: GeexTypePolicies;
  /** When true (default), merge with geexDefaultTypePolicies(). */
  includeDefaults?: boolean;
}

export interface GeexApolloLinkOptions {
  baseUrl: string;
  httpLinkInstance: ApolloLink;
  extraLinks?: ApolloLink[];
}

export interface GeexGraphqlErrorHandler {
  handleGraphQLErrors(params: {
    graphQLErrors?: ReadonlyArray<{ message?: string; extensions?: any }>;
    operation?: any;
    response?: { data?: any } | null;
  }): void;
  handleGraphQLNetworkError(networkError: unknown): void;
  buildCommonHeaders?(): { [name: string]: string };
}

export const geexApolloDefaultOptions = {
  query: {
    fetchPolicy: "network-only" as const,
    errorPolicy: "ignore" as const,
  },
  mutate: {
    fetchPolicy: "no-cache" as const,
    errorPolicy: "ignore" as const,
  },
  watchQuery: {
    fetchPolicy: "cache-first" as const,
    errorPolicy: "ignore" as const,
  },
};

/** Core cache policies. Feature-specific policies are registered by extensions. */
export function geexDefaultTypePolicies(): TypePolicies {
  return {
    Setting: {
      keyFields: ["name"],
    },
  };
}

function mergeTypePolicies(base: TypePolicies, extras: readonly GeexTypePolicies[]): TypePolicies {
  let merged = base;
  for (const extra of extras) {
    merged = { ...merged, ...(extra as TypePolicies) };
  }
  return merged;
}

export function createGeexInMemoryCache(
  options: GeexApolloCacheOptions = {},
  contributions: readonly GeexApolloTypePolicyContribution[] = [],
): InMemoryCache {
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

export function createGeexUriLink(baseUrl: string): ApolloLink {
  return new ApolloLink((operation, forward) => {
    const variables = Object.entries(operation.variables).filter(([, v]) => v != undefined);
    if (variables.length > 0) {
      operation.setContext(() => {
        const encodedParams = variables.map(([k, v]) => `${k}=${json5.stringify(v)}`).join("&");
        return {
          uri: new URL(`/graphql/${operation.operationName}?${encodedParams}`, baseUrl).toString().substring(0, 2047),
        };
      });
    } else {
      operation.setContext(() => ({
        uri: new URL(`/graphql/${operation.operationName}`, baseUrl).toString().substring(0, 2047),
      }));
    }
    return forward(operation);
  });
}

export function createGeexHttpApolloOptions(options: GeexApolloLinkOptions & { cache: InMemoryCache }): ApolloClient.Options {
  const uriLink = createGeexUriLink(options.baseUrl);
  const links = [...(options.extraLinks ?? []), uriLink, options.httpLinkInstance];
  return {
    link: ApolloLink.from(links),
    cache: options.cache,
    defaultOptions: geexApolloDefaultOptions,
  } as unknown as ApolloClient.Options;
}

export function isGeexSilentOperation(
  operation: { getContext: () => Record<string, unknown> },
  silentToken: HttpContextToken<boolean> = SILENT_REQUEST,
): boolean {
  const context = operation.getContext() ?? {};
  if (context["silent"] === true) {
    return true;
  }
  const httpContext = context["httpContext"];
  return httpContext instanceof HttpContext && httpContext.get(silentToken) === true;
}

export function createGeexGraphqlErrorLink(handler: GeexGraphqlErrorHandler): ApolloLink {
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
    } else if (error) {
      handler.handleGraphQLNetworkError(error);
    }
  });
}

export function createGeexSilentContextLink(silentToken: HttpContextToken<boolean> = SILENT_REQUEST): ApolloLink {
  return new ApolloLink((operation, forward) => {
    operation.setContext(context => {
      const prevHttpContext =
        context.httpContext instanceof HttpContext ? context.httpContext : new HttpContext();
      return {
        silent: true,
        httpContext: prevHttpContext.set(silentToken, true),
      };
    });
    return forward(operation);
  });
}

export function createGeexWsApolloOptions(options: {
  baseUrl?: string;
  url?: string;
  cache: InMemoryCache;
  connectionParams?: () => Promise<Record<string, string>> | Record<string, string>;
  retryAttempts?: number;
  onOpened?: () => void;
  onError?: (err: unknown) => void;
}): ApolloClient.Options {
  const url =
    options.url ??
    new URL("/graphql", (options.baseUrl ?? "").replace(/^http/, "ws")).toString();

  const client = createClient({
    url,
    lazy: true,
    retryAttempts: options.retryAttempts ?? 3,
    connectionParams: options.connectionParams,
    on: {
      opened: options.onOpened ?? (() => console.log("ws connected.")),
      error: options.onError ?? ((err: unknown) => console.error("ws connect failed.", err)),
    },
  });

  return {
    link: new GraphQLWsLink(client),
    cache: options.cache,
    defaultOptions: geexApolloDefaultOptions,
  } as unknown as ApolloClient.Options;
}

/**
 * HttpLink with multipart upload support.
 * Uses peer `extract-files` by default; overrides via options.
 */
export function createGeexUploadHttpLink(
  httpLink: HttpLink,
  options?: {
    withCredentials?: boolean;
    extractFilesFn?: (body: any, isExtractable: (v: any) => boolean) => any;
    isExtractableFile?: (value: any) => boolean;
  },
): ApolloLink {
  const extractFilesFn = options?.extractFilesFn ?? extractFiles;
  const isExtractable = options?.isExtractableFile ?? isExtractableFile;
  return httpLink.create({
    withCredentials: options?.withCredentials ?? true,
    extractFiles: body => extractFilesFn(body, isExtractable),
  });
}

export const SilentApollo = new InjectionToken<ApolloBase>("silent_apollo");
export const GEEX_APOLLO_CACHE = new InjectionToken<InMemoryCache>("GEEX_APOLLO_CACHE");

export interface ProvideGeexApolloOptions {
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

export function provideGeexApollo(options: ProvideGeexApolloOptions): Provider[] {
  const createHttp = (httpLink: HttpLink) => {
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
      useFactory: () =>
        createGeexInMemoryCache(
          {
            possibleTypes: options.possibleTypes,
            typePolicies: options.typePolicies,
            includeDefaults: options.includeDefaultTypePolicies !== false,
          },
          inject(GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS, { optional: true }) ?? [],
        ),
    },
    {
      provide: APOLLO_OPTIONS,
      useFactory: (cache: InMemoryCache, httpLink: HttpLink, interceptor: GeexHttpInterceptor) => {
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
      useFactory: (cache: InMemoryCache, httpLink: HttpLink, interceptor: GeexHttpInterceptor): NamedOptions => {
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
      useFactory: (apollo: Apollo) => apollo.use("silent"),
      deps: [Apollo],
    },
  ];
}
