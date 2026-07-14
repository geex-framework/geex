import { Injectable, Optional, Inject } from "@angular/core";
import { Apollo } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import {
  WECHAT_AUTH_CONFIG,
  WechatAuthConfig,
  WechatWebLoginService,
} from "@geexcode/geex-extensions-authentication-wechat";
import { GeexMockingCapabilitiesService } from "./mocking-capabilities.service";
import { CREATE_MOCK_WECHAT_AUTHORIZATION, GET_MOCK_WECHAT_AUTHORIZATION_STATUS } from "./graphql";

@Injectable()
export class MockAwareWechatWebLoginService extends WechatWebLoginService {
  constructor(
    @Optional() @Inject(WECHAT_AUTH_CONFIG) config: WechatAuthConfig | null,
    private readonly capabilities: GeexMockingCapabilitiesService,
    private readonly apollo: Apollo,
  ) {
    super(config);
  }

  override async renderQr(overrides?: Partial<WechatAuthConfig> & { state?: string }): Promise<void> {
    const caps = await this.capabilities.getCapabilities();
    if (!caps.enabled || !caps.wechatWeb) {
      return super.renderQr(overrides);
    }

    const redirectUri = overrides?.redirectUri || this.tryGetRedirectUri(overrides);
    const containerId = overrides?.containerId ?? "wechat_login_container";
    const state = overrides?.state ?? "WechatWeb";
    if (!redirectUri) {
      throw new Error("redirectUri is required for mock WeChat login.");
    }

    const created = await firstValueFrom(
      this.apollo.mutate<{
        createMockWechatAuthorization: { token: string; qrSvg: string; redirectUri: string; state: string };
      }>({
        mutation: CREATE_MOCK_WECHAT_AUTHORIZATION,
        variables: { request: { redirectUri, state } },
      }),
    );

    const auth = created.data?.createMockWechatAuthorization;
    if (!auth) {
      return super.renderQr(overrides);
    }

    const container = document.getElementById(containerId);
    if (!container) {
      throw new Error(`Container #${containerId} not found.`);
    }
    container.innerHTML = auth.qrSvg;
    this.pollAuthorization(auth.token, auth.redirectUri, auth.state);
  }

  private tryGetRedirectUri(overrides?: Partial<WechatAuthConfig>): string | undefined {
    try {
      return this.getConfig(overrides).redirectUri;
    } catch {
      return typeof location !== "undefined" ? `${location.origin}/auth/login` : undefined;
    }
  }

  private pollAuthorization(token: string, redirectUri: string, state: string): void {
    const timer = window.setInterval(async () => {
      try {
        const result = await firstValueFrom(
          this.apollo.query<{
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
  }
}
