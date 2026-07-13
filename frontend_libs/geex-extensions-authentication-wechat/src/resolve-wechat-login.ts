import { ApolloClient, NormalizedCacheObject } from "@apollo/client/core";
import { Apollo } from "apollo-angular";
import { OAuthService } from "angular-oauth2-oidc";
import { firstValueFrom, isObservable } from "rxjs";
import {
  LINK_EXTERNAL_LOGIN,
  REGISTER_AND_LINK_EXTERNAL_LOGIN,
  RESOLVE_EXTERNAL_LOGIN,
} from "./graphql";
import {
  EstablishSessionOptions,
  ResolveExternalLoginResult,
  WechatLoginProvider,
} from "./types";

type ApolloLike = Apollo | ApolloClient<NormalizedCacheObject>;

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
): Promise<ResolveExternalLoginResult> {
  const data = await mutate<{ resolveExternalLogin: ResolveExternalLoginResult }>(apollo, {
    mutation: RESOLVE_EXTERNAL_LOGIN,
    variables: {
      loginProvider: params.loginProvider,
      code: params.code,
    },
  });
  return data.resolveExternalLogin;
}

export async function linkExternalLogin(
  apollo: ApolloLike,
  accountLinkToken: string,
  context?: { headers?: Record<string, string> },
): Promise<{ token?: string | null; userId: string; loginProvider: string }> {
  const data = await mutate<{
    linkExternalLogin: { token?: string | null; userId: string; loginProvider: string };
  }>(apollo, {
    mutation: LINK_EXTERNAL_LOGIN,
    variables: { accountLinkToken },
    context,
  });
  return data.linkExternalLogin;
}

export async function registerAndLinkExternalLogin(
  apollo: ApolloLike,
  params: {
    accountLinkToken: string;
    username: string;
    password: string;
    phoneNumber?: string;
    email?: string;
    nickname?: string;
  },
): Promise<{ token?: string | null; userId: string; loginProvider: string }> {
  const data = await mutate<{
    registerAndLinkExternalLogin: { token?: string | null; userId: string; loginProvider: string };
  }>(apollo, {
    mutation: REGISTER_AND_LINK_EXTERNAL_LOGIN,
    variables: params,
  });
  return data.registerAndLinkExternalLogin;
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
): Promise<ResolveExternalLoginResult> {
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
