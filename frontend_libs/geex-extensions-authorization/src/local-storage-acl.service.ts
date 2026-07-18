import { effect, Injectable, Injector } from "@angular/core";
import { ACLService } from "@delon/acl";
import { geex } from "@geexcode/geex-angular";
import json5 from "json5";

type AuthorizationAuthModule = {
  init(force?: boolean): Promise<unknown>;
  user(): unknown;
};

@Injectable({
  providedIn: "root",
})
export class LocalStorageACLService extends ACLService {
  static instance: LocalStorageACLService;

  static new(injector: Injector) {
    if (LocalStorageACLService.instance) {
      return LocalStorageACLService.instance;
    }
    return (LocalStorageACLService.instance = new LocalStorageACLService(injector));
  }

  constructor(injector: Injector) {
    super();
    effect(async () => {
      const auth = geex["auth"] as AuthorizationAuthModule;
      await auth.init();
      if (auth.user() != undefined) {
        let data = { roles: [] as string[], abilities: [] as (string | number)[], full: false };
        try {
          data = json5.parse(localStorage.getItem("acl") ?? "{}");
        } catch (error) {
          console.error("failed to load acl from localStorage.", error);
        }
        this["roles"] = data?.roles ?? [];
        this["abilities"] = data?.abilities ?? [];
        this["full"] = data?.full ?? false;
        this.change.subscribe(() => {
          localStorage.setItem("acl", json5.stringify(this.data));
        });
        this["aclChange"].next(data);
      }
    });
  }
}
