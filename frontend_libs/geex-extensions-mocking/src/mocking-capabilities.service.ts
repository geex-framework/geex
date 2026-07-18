import { Injectable, InjectionToken, Optional, Inject } from "@angular/core";
import { Apollo, ApolloBase } from "apollo-angular";
import { firstValueFrom, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { MOCKING_CAPABILITIES } from "./graphql";
import { GeexMockingCapabilities, GeexMockingOptions } from "./types";

export const GEEX_MOCKING_OPTIONS =
  new InjectionToken<Readonly<GeexMockingOptions>>("GEEX_MOCKING_OPTIONS");

const DISABLED: GeexMockingCapabilities = {
  enabled: false,
  wechatWeb: false,
  payments: false,
  sms: false,
  management: false,
};

@Injectable()
export class GeexMockingCapabilitiesService {
  private cached?: Promise<GeexMockingCapabilities>;

  constructor(
    private readonly apollo: Apollo,
    @Optional() @Inject(GEEX_MOCKING_OPTIONS) private readonly options: Readonly<GeexMockingOptions> | null,
  ) {}

  getCapabilities(force = false): Promise<GeexMockingCapabilities> {
    if (!force && this.cached) {
      return this.cached;
    }

    const client: ApolloBase = this.apollo.use("silent") ?? this.apollo;

    this.cached = firstValueFrom(
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

    return this.cached;
  }
}
