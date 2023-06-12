import { Injectable, Injector } from "@angular/core";
import { ACLService } from "@delon/acl";
import { AlainConfigService } from "@delon/util";
import json5 from "json5";
export class LocalStorageACLService extends ACLService {
  // 这俩变量是防止多次注册
  static new(injector: Injector) {
    if (LocalStorageACLService.instance) {
      return LocalStorageACLService.instance;
    }
    return (LocalStorageACLService.instance = new LocalStorageACLService(injector));
  }
  static instance: LocalStorageACLService;
  /**
   *
   */
  constructor(injector: Injector) {
    super(injector.get(AlainConfigService));
    const data = json5.parse(localStorage.getItem("acl")) ?? { roles: [], abilities: [], full: false };
    this["roles"] = data.roles;
    this["abilities"] = data.abilities;
    this["full"] = data.full;
    // 监听权限，改变时写localStorage，目的是为了和token，user，appSetting等信息读取写入行为一致
    this.change.subscribe(x => {
      localStorage.setItem("acl", json5.stringify(this.data));
    });
    this["aclChange"].next(data);
  }
}
