import gql from "graphql-tag";

export const GQL_GENERATE_CAPTCHA = gql`
  mutation generateCaptcha($request: SendCaptchaRequest!) {
    generateCaptcha(request: $request) {
      captchaType
      key
      imageBase64
    }
  }
`;

export const GQL_VALIDATE_CAPTCHA = gql`
  mutation validateCaptcha($request: ValidateCaptchaRequest!) {
    validateCaptcha(request: $request)
  }
`;
