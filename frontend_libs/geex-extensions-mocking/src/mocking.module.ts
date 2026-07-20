import { Injector } from "@angular/core";
import { Apollo, ApolloBase } from "apollo-angular";
import { firstValueFrom, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import type { AuthenticationWechatRenderOverrides } from "@geexcode/geex-extensions-authentication-wechat";
import { geex } from "@geexcode/geex-angular";
import {
  CREATE_MOCK_WECHAT_AUTHORIZATION,
  GET_MOCK_WECHAT_AUTHORIZATION_STATUS,
  MOCKING_CAPABILITIES,
} from "./graphql";
import type { GeexMockingCapabilities, MockingModule } from "./mocking.types";

const DISABLED: GeexMockingCapabilities = {
  enabled: false,
  wechatWeb: false,
  payments: false,
  sms: false,
  management: false,
};

export function createMockingModule(injector: Injector): MockingModule {
  let cached: Promise<GeexMockingCapabilities> | undefined;
  const apollo = () => injector.get(Apollo);

  const getCapabilities = (force = false): Promise<GeexMockingCapabilities> => {
    if (!force && cached) {
      return cached;
    }

    const client: ApolloBase = apollo().use("silent") ?? apollo();
    cached = firstValueFrom(
      client
        .query<{ mockingCapabilities: GeexMockingCapabilities }>({
          query: MOCKING_CAPABILITIES,
          fetchPolicy: "network-only",
          errorPolicy: "ignore",
          context: {
            silent: true,
          },
        })
        .pipe(
          map(result => result.data?.mockingCapabilities ?? DISABLED),
          catchError(() => of(DISABLED)),
        ),
    ).catch(() => DISABLED);

    return cached;
  };

  const pollAuthorization = (token: string, redirectUri: string, state: string): void => {
    const timer = window.setInterval(async () => {
      try {
        const result = await firstValueFrom(
          apollo().query<{
            mockWechatAuthorizationStatus: { status: string; code?: string };
          }>({
            query: GET_MOCK_WECHAT_AUTHORIZATION_STATUS,
            variables: { token },
            fetchPolicy: "network-only",
          }),
        );
        const status = result.data?.mockWechatAuthorizationStatus;
        if (status?.status === "Confirmed" && status.code) {
          window.clearInterval(timer);
          const url = new URL(redirectUri);
          url.searchParams.set("code", status.code);
          url.searchParams.set("state", state);
          location.href = url.toString();
        }
        if (status?.status === "Expired" || status?.status === "Consumed") {
          window.clearInterval(timer);
        }
      } catch {
        // keep polling until expire
      }
    }, 1500);
  };

  return {
    getCapabilities,
    renderWechatQr: async (overrides?: AuthenticationWechatRenderOverrides) => {
      const authenticationWechat = geex.authenticationWechat;
      if (!authenticationWechat) {
        throw new Error(
          "renderWechatQr() requires geex.authenticationWechat; register provideGeexAuthenticationWechat() first.",
        );
      }
      const config = authenticationWechat.getConfig(overrides);
      const containerId = overrides?.containerId ?? config.containerId ?? "wechat_login_container";
      const state = overrides?.state ?? "WechatWeb";
      const redirectUri = overrides?.redirectUri || config.redirectUri;
      if (!redirectUri) {
        throw new Error("redirectUri is required for mock WeChat login.");
      }

      const created = await firstValueFrom(
        apollo().mutate<{
          createMockWechatAuthorization: { token: string; qrSvg: string; redirectUri: string; state: string };
        }>({
          mutation: CREATE_MOCK_WECHAT_AUTHORIZATION,
          variables: { request: { redirectUri, state } },
        }),
      );

      const auth = created.data?.createMockWechatAuthorization;
      if (!auth) {
        throw new Error("Failed to create mock WeChat authorization.");
      }

      const container = document.getElementById(containerId);
      if (!container) {
        throw new Error(`Container #${containerId} not found.`);
      }
      container.innerHTML = auth.qrSvg;
      pollAuthorization(auth.token, auth.redirectUri, auth.state);
    },
    init: async () => undefined,
  };
}
