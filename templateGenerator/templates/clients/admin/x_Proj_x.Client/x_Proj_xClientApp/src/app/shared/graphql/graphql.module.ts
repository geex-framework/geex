import { HttpClient, HttpHeaders } from "@angular/common/http";
import { InjectionToken, NgModule } from "@angular/core";
import { Router } from "@angular/router";
import { ApolloClientOptions, InMemoryCache, concat, ApolloLink, split, DefaultOptions } from "@apollo/client/core";
import { WebSocketLink } from "@apollo/client/link/ws";
import { getMainDefinition } from "@apollo/client/utilities";
import { LoadingService, LoadingType } from "@delon/abc/loading";
import { DA_SERVICE_TOKEN, ITokenService, JWTTokenModel, TokenService } from "@delon/auth";
import { environment } from "@env/environment";
import { Apollo, APOLLO_NAMED_OPTIONS, APOLLO_OPTIONS, NamedOptions } from "apollo-angular";
import { HttpLink } from "apollo-angular/http";
import { onError } from "apollo-link-error";
import { OperationDefinitionNode } from "graphql";
import * as json5 from "json5";
import { NzModalService } from "ng-zorro-antd/modal";
// import { rxjs } from "apollo-link-rxjs";
// import { retry, tap } from "rxjs/operators";
import { TypedTypePolicies } from "src/app/shared/graphql/.generated/apollo-helpers";
// import { LoadingService } from "../services/loading.service";

function findCachedOrg(parentOrgCode: any) {
  let parentOrg = { ...window["orgs"].firstOrDefault(x => x.code == parentOrgCode) };
  return parentOrg;
}

const org = (_, context) => {
  var parentOrgCode = context.readField("parentOrgCode");
  if (!parentOrgCode) {
    return null;
  }
  return findCachedOrg(parentOrgCode);
};

const company = (_, context) => {
  var companyId = context.readField("companyId");
  if (!companyId) {
    return null;
  }
  let company = { ...window["companies"]?.find(x => x.id == companyId) };
  var parentOrgCode = company?.parentOrgCode;
  if (!parentOrgCode) {
    return null;
  }
  company.parentOrg ??= findCachedOrg(parentOrgCode);
  return company;
};

function financialInstitution(fieldName: string) {
  const _financialInstitution = (_, context) => {
    var institutionId = context.readField(fieldName);
    if (!institutionId) {
      return null;
    }
    let institution = { ...window["institutions"]?.find(x => x.id == institutionId) };
    return institution;
  };
  return _financialInstitution;
}

const mainProject = (_, context) => {
  var mainProjectId = context.readField("mainProjectId");
  if (!mainProjectId) {
    return null;
  }
  let mainProject = { ...window["mainProjects"]?.find(x => x.id == mainProjectId) };
  return mainProject;
};

const typePolicies: TypedTypePolicies = {
  Setting: {
    keyFields: ["name"],
  },
  // Keys in this object will be validated against the typed on your schema
  IContract: {
    fields: {
      company: {
        read: company,
      },
      issuanceFinancialInstitution: {
        read: financialInstitution("issuanceInstitutionId"),
      },
      executingFinancialInstitution: {
        read: financialInstitution("executingInstitutionId"),
      },
      mainProject: {
        read: mainProject,
      },
    },
  },
  IProject: {
    fields: {
      company: {
        read: company,
      },
    },
  },
  MainProject: {
    fields: {
      company: {
        read: company,
      },
    },
  },
  ICompany: {
    fields: {
      parentOrg: {
        read: org,
      },
    },
  },
  Budget: {
    fields: {
      company: {
        read: company,
      },
    },
  },
  Project: {
    fields: {
      company: {
        read: company,
      },
    },
  },
  Credit: {
    fields: {
      company: {
        read: company,
      },
    },
  },
  // Org: {},
};

const gqlCache = new InMemoryCache({
  typePolicies: typePolicies,
  possibleTypes: {
    IContract: ["Subcontract", "StandardContract", "FrameworkContract"],
    ICompany: ["Company"],
  },
});

const defaultGqlOptions = {
  query: {
    fetchPolicy: "network-only",
    errorPolicy: "ignore",
  },
  mutate: {
    fetchPolicy: "no-cache",
    errorPolicy: "ignore",
  },
  watchQuery: {
    fetchPolicy: "cache-first",
    errorPolicy: "ignore",
  },
} as DefaultOptions;

const uriLink = new ApolloLink((operation, forward) => {
  var variables = Object.entries(operation.variables).where(x => x[1] != undefined);
  if (variables.length > 0) {
    operation.setContext(context => {
      let operationType = (operation.query.definitions[0] as OperationDefinitionNode).operation;
      return {
        uri: new URL(`/graphql/${operation.operationName}?${new URLSearchParams(variables).toString()}`, environment.api.baseUrl)
          .toString()
          //避免url超长, 最多显示2047个字符
          .substr(0, 2047),
        headers: {
          "x-readonly": operationType === "query" ? "1" : "0",
          ...context.headers,
        },
      };
    });
  } else {
    operation.setContext(context => {
      return {
        uri: new URL(`/graphql/${operation.operationName}`, environment.api.baseUrl)
          .toString()
          //避免url超长, 最多显示2047个字符
          .substr(0, 2047),
      };
    });
  }
  return forward ? forward(operation) : null;
});

const errorHandlingLink = onError(({ graphQLErrors, networkError, operation, response }) => {
  if (graphQLErrors)
    graphQLErrors.map(({ message, locations, path }) =>
      console.error(`[GraphQL error]: Message: ${message}, Location: ${locations}, Path: ${path}`),
    );

  if (networkError) {
    console.error(`[Network error]: ${networkError}`);
  }
}) as unknown as ApolloLink;

export function createApollo(
  router: Router,
  httpLink: HttpLink,
  loadingSrv: LoadingService,
  tokenSrv: ITokenService,
  modalSrv: NzModalService,
): ApolloClientOptions<any> {
  const loadingLink = new ApolloLink((operation, forward) => {
    loadingSrv?.open({ type: "icon", delay: 50 });
    return forward(operation).map(response => {
      loadingSrv?.close();
      return response;
    });
  });
  const baseHttpLink = httpLink.create({
    withCredentials: true,
  });

  return {
    link: concat(errorHandlingLink, concat(loadingLink, concat(uriLink, baseHttpLink))),
    cache: gqlCache,
    defaultOptions: defaultGqlOptions,
  };
}

export function createSilentApollo(
  router: Router,
  httpLink: HttpLink,
  tokenSrv: ITokenService,
  modalSrv: NzModalService,
): ApolloClientOptions<any> {
  const baseHttpLink = httpLink.create({
    withCredentials: true,
  });

  return {
    link: concat(errorHandlingLink, concat(uriLink, baseHttpLink)),
    cache: gqlCache,
    defaultOptions: defaultGqlOptions,
  };
}

export function createSubscriptionApollo(httpLink: HttpLink, tokenSrv: TokenService): ApolloClientOptions<any> {
  const token = tokenSrv.get(JWTTokenModel);
  const wsLink = new WebSocketLink({
    uri: new URL("/graphql", environment.api.baseUrl.replace(/^http/, "ws")).toString(),
    options: {
      reconnect: true,
      connectionParams: {
        Authorization: token?.token && !token?.isExpired() ? `Bearer ${token.token}` : "",
      },
      connectionCallback: error => {
        if (error) {
          console.error("ws connect failed.", error);
        } else {
          console.log("ws connected.");
        }
      },
    },
  });
  return {
    link: wsLink,
    cache: gqlCache,
    defaultOptions: defaultGqlOptions,
  };
}

export const SilentApollo = new InjectionToken<Apollo>("silent_apollo");
@NgModule({
  providers: [
    {
      provide: APOLLO_OPTIONS,
      useFactory: createApollo,
      deps: [Router, HttpLink, LoadingService, DA_SERVICE_TOKEN, NzModalService],
    },
    {
      provide: APOLLO_NAMED_OPTIONS, // <-- Different from standard initialization
      useFactory(router: Router, httpLink: HttpLink, tokenSrv: TokenService, modalSrv: NzModalService): NamedOptions {
        return {
          silent: createSilentApollo(router, httpLink, tokenSrv, modalSrv),
          subscription: createSubscriptionApollo(httpLink, tokenSrv),
        };
      },
      deps: [Router, HttpLink, DA_SERVICE_TOKEN, NzModalService],
    },
  ],
})
export class GraphQLModule {}
