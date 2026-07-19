import type { GeexModule } from "@geexcode/geex-angular";
import type { WechatAuthRenderOverrides } from "@geexcode/geex-extensions-authentication-wechat";

export interface GeexMockingCapabilities {
  enabled: boolean;
  wechatWeb: boolean;
  payments: boolean;
  sms: boolean;
  management: boolean;
}

export interface MockingModule extends GeexModule<{
  getCapabilities(force?: boolean): Promise<GeexMockingCapabilities>;
  renderWechatQr(overrides?: WechatAuthRenderOverrides): Promise<void>;
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    mocking: MockingModule;
  }
}
