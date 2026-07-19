import { effect, Injectable, Injector } from "@angular/core";
import { ACLService } from "@delon/acl";
import { geex } from "@geexcode/geex-angular";

@Injectable()
export class LocalStorageACLService extends ACLService {
  static new(injector: Injector) {
    return new LocalStorageACLService(injector);
  }

  constructor(injector: Injector) {
    super();
    effect(async () => {
      const data = await geex.authorization.syncAclFromAuth();
      if (!data) {
        return;
      }
      this["roles"] = data.roles ?? [];
      this["abilities"] = data.abilities ?? [];
      this["full"] = data.full ?? false;
      this.change.subscribe(() => {
        geex.authorization.persistAcl(this.data as typeof data);
      });
      this["aclChange"].next(data);
    });
  }
}
