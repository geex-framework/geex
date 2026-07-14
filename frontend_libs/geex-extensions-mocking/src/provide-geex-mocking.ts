import { Provider } from "@angular/core";
import { WechatWebLoginService } from "@geexcode/geex-extensions-authentication-wechat";
import { GEEX_MENU_CONTRIBUTIONS } from "@geexcode/geex-angular";
import { GEEX_MOCKING_OPTIONS, GeexMockingCapabilitiesService } from "./mocking-capabilities.service";
import { MockAwareWechatWebLoginService } from "./mock-aware-wechat-web-login.service";
import { MockingMenuContribution } from "./mocking-menu.contribution";
import { GeexMockingOptions } from "./types";

export function provideGeexMocking(options: GeexMockingOptions = {}): Provider[] {
  return [
    { provide: GEEX_MOCKING_OPTIONS, useValue: options },
    GeexMockingCapabilitiesService,
    { provide: WechatWebLoginService, useClass: MockAwareWechatWebLoginService },
    MockingMenuContribution,
    { provide: GEEX_MENU_CONTRIBUTIONS, multi: true, useExisting: MockingMenuContribution },
  ];
}
