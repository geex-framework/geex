import { ApolloClient } from "@apollo/client/core";
import { Apollo } from "apollo-angular";
import { OAuthService } from "angular-oauth2-oidc";
import { firstValueFrom, isObservable } from "rxjs";
import {
  LINK_LOGIN,
  REGISTER_AND_LINK_LOGIN,
  RESOLVE_LOGIN,
} from "./graphql";
import {
  EstablishSessionOptions,
  ResolveLoginResult,
  WechatLoginProvider,
} from "./types";

type ApolloLike = Apollo | ApolloClient;

async function mutate<T>(
  client: ApolloLike,
  options: { mutation: any; variables: Record<string, unknown>; context?: Record<string, unknown> },
): Promise<T> {
  const result = (client as Apollo).mutate<T>(options as any);
  if (isObservable(result)) {
    const response = await firstValueFrom(result);
    return response.data as T;
  }
  const response = await result;
  return (response as { data: T }).data;
}

export async function resolveWechatLogin(
  apollo: ApolloLike,
  params: { loginProvider: WechatLoginProvider; code: string },
): Promise<ResolveLoginResult> {
  const data = await mutate<{ resolveLogin: ResolveLoginResult }>(apollo, {
    mutation: RESOLVE_LOGIN,
    variables: {
      loginProvider: params.loginProvider,
      code: params.code,
    },
  });
  return data.resolveLogin;
}

export async function linkLogin(
  apollo: ApolloLike,
  userLoginLinkToken: string,
  context?: { headers?: Record<string, string> },
): Promise<{ token?: string | null; userId: string; loginProvider: string }> {
  const data = await mutate<{
    linkLogin: { token?: string | null; userId: string; loginProvider: string };
  }>(apollo, {
    mutation: LINK_LOGIN,
    variables: { userLoginLinkToken },
    context,
  });
  return data.linkLogin;
}

export async function registerAndLinkLogin(
  apollo: ApolloLike,
  params: {
    userLoginLinkToken: string;
    username: string;
    password: string;
    phoneNumber?: string;
    email?: string;
    nickname?: string;
  },
): Promise<{ token?: string | null; userId: string; loginProvider: string }> {
  const data = await mutate<{
    registerAndLinkLogin: { token?: string | null; userId: string; loginProvider: string };
  }>(apollo, {
    mutation: REGISTER_AND_LINK_LOGIN,
    variables: params,
  });
  return data.registerAndLinkLogin;
}

export function establishGeexSession(
  oauthService: OAuthService,
  options: EstablishSessionOptions,
): void {
  oauthService.initCodeFlow(options.redirectUri ?? "/", { access_token: options.token });
}

export async function loginWithMiniProgramCode(
  client: ApolloLike,
  code: string,
  oauthService?: OAuthService,
  redirectUri?: string,
): Promise<ResolveLoginResult> {
  const result = await resolveWechatLogin(client, {
    loginProvider: "WechatMiniProgram",
    code,
  });
  if (result.isLinked && result.session?.token && oauthService) {
    establishGeexSession(oauthService, {
      token: result.session.token,
      redirectUri,
    });
  }
  return result;
}
