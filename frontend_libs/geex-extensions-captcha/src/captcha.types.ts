import type { GeexModule } from "@geexcode/geex-angular";
import type { DocumentNode } from "graphql";

export enum CaptchaProvider {
  Sms = "Sms",
  Image = "Image",
}

export interface Captcha {
  captchaType?: string | null;
  key: string;
  imageBase64?: string | null;
}

export interface GenerateCaptchaRequest {
  captchaProvider: CaptchaProvider;
  smsCaptchaPhoneNumber?: string | null;
}

export interface ValidateCaptchaRequest {
  captchaProvider: CaptchaProvider;
  captchaKey: string;
  captchaCode: string;
}

export interface CaptchaModule extends GeexModule<{
  generateCaptcha(request: GenerateCaptchaRequest): Promise<Captcha | null>;
  validateCaptcha(request: ValidateCaptchaRequest): Promise<boolean>;
  readonly documents: {
    generateCaptcha: DocumentNode;
    validateCaptcha: DocumentNode;
  };
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap {
    captcha: CaptchaModule;
  }
}
