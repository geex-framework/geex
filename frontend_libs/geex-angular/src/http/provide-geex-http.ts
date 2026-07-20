import { HTTP_INTERCEPTORS } from "@angular/common/http";
import type { Provider } from "@angular/core";

import { GeexHttpInterceptor } from "./geex-http.interceptor";
import { GEEX_API_BASE_URL } from "./tokens";

export interface GeexHttpProvideOptions {
  apiBaseUrl: string;
}

export function provideGeexHttp(options: GeexHttpProvideOptions): Provider[] {
  return [
    { provide: GEEX_API_BASE_URL, useValue: options.apiBaseUrl },
    GeexHttpInterceptor,
    { provide: HTTP_INTERCEPTORS, useExisting: GeexHttpInterceptor, multi: true },
  ];
}
