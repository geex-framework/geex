import { EnvironmentProviders, makeEnvironmentProviders } from "@angular/core";
import { WechatWebLoginService } from "@geexcode/geex-extensions-authentication-wechat";
import { GEEX_MOCKING_OPTIONS, GeexMockingCapabilitiesService } from "./mocking-capabilities.service";
import { MockAwareWechatWebLoginService } from "./mock-aware-wechat-web-login.service";
import { GeexMockingOptions } from "./types";

export function provideGeexMocking(
  options: Readonly<GeexMockingOptions> = {},
): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: GEEX_MOCKING_OPTIONS, useValue: options },
    GeexMockingCapabilitiesService,
    { provide: WechatWebLoginService, useClass: MockAwareWechatWebLoginService },
  ]);
}
