import { Injector } from "@angular/core";
import { Apollo } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import { GQL_GENERATE_CAPTCHA, GQL_VALIDATE_CAPTCHA } from "./graphql";
import type {
  Captcha,
  CaptchaModule,
  GenerateCaptchaRequest,
  ValidateCaptchaRequest,
} from "./captcha.types";

export function createCaptchaModule(injector: Injector): CaptchaModule {
  const apollo = () => injector.get(Apollo);

  return {
    documents: {
      generateCaptcha: GQL_GENERATE_CAPTCHA,
      validateCaptcha: GQL_VALIDATE_CAPTCHA,
    },
    async generateCaptcha(request: GenerateCaptchaRequest): Promise<Captcha | null> {
      const res = await firstValueFrom(
        apollo().mutate<{ generateCaptcha: Captcha }>({
          mutation: GQL_GENERATE_CAPTCHA,
          variables: { request },
        }),
      );
      return res.data?.generateCaptcha ?? null;
    },
    async validateCaptcha(request: ValidateCaptchaRequest): Promise<boolean> {
      const res = await firstValueFrom(
        apollo().mutate<{ validateCaptcha: boolean }>({
          mutation: GQL_VALIDATE_CAPTCHA,
          variables: { request },
        }),
      );
      return !!res.data?.validateCaptcha;
    },
    init: async () => undefined,
  };
}
