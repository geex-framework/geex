// 请参考：https://ng-alain.com/docs/i18n
import { Platform } from "@angular/cdk/platform";
import { registerLocaleData } from "@angular/common";
import ngEn from "@angular/common/locales/en";
import ngZh from "@angular/common/locales/zh";
import ngZhTw from "@angular/common/locales/zh-Hant";
import { Injectable } from "@angular/core";
import {
  AlainI18NService,
  DelonLocaleService,
  en_US as delonEnUS,
  SettingsService,
  zh_CN as delonZhCn,
  zh_TW as delonZhTw,
} from "@delon/theme";
import { TranslateService } from "@ngx-translate/core";
import { enUS as dfEn, zhCN as dfZhCn, zhTW as dfZhTw } from "date-fns/locale";
import kiwiIntl from "kiwi-intl";
import { merge, flatMapDeep } from "lodash-es";
import { NzSafeAny } from "ng-zorro-antd/core/types";
import { en_US as zorroEnUS, NzI18nService, zh_CN as zorroZhCN, zh_TW as zorroZhTW } from "ng-zorro-antd/i18n";
import { BehaviorSubject, Observable } from "rxjs";
import { filter } from "rxjs/operators";

import enUSLangs from "../i18n/en-us";
import zhCNLangs from "../i18n/zh-cn";
import { LangObject } from "./types";

interface LangData {
  abbr: string;
  text: string;
  ng: NzSafeAny;
  zorro: NzSafeAny;
  date: NzSafeAny;
  delon: NzSafeAny;
}

const DEFAULT = "zh-cn";
const KIWI_LANGS = {
  "zh-cn": zhCNLangs,
};
const LANGS: { [key: string]: LangData } = {
  "zh-cn": {
    text: "简体中文",
    ng: ngZh,
    zorro: zorroZhCN,
    date: dfZhCn,
    delon: delonZhCn,
    abbr: "🇨🇳",
  },
};

@Injectable({ providedIn: "root" })
export class I18NService implements AlainI18NService {
  private _default = DEFAULT;
  private change$ = new BehaviorSubject<string | null>(null);

  private _langs = Object.keys(LANGS).map(code => {
    const item = LANGS[code];
    return { code, text: item.text, abbr: item.abbr };
  });

  constructor(
    private settings: SettingsService,
    private nzI18nService: NzI18nService,
    private delonLocaleService: DelonLocaleService,
    private translate: TranslateService,
    private platform: Platform,
  ) {
    // `@ngx-translate/core` 预先知道支持哪些语言
    window["translate"] = this;
    const lans = this._langs.map(item => item.code);
    translate.addLangs(lans);

    const defaultLan = this.getDefaultLang();
    if (lans.includes(defaultLan)) {
      this._default = defaultLan;
    }
  }

  private getDefaultLang(): string {
    if (!this.platform.isBrowser) {
      return DEFAULT;
    }
    if (this.settings.layout.lang) {
      return this.settings.layout.lang;
    }
    return (navigator.languages ? navigator.languages[0] : null) || navigator.language;
  }

  private updateLangData(lang: string): void {
    const item = LANGS[lang.toLocaleLowerCase()];
    registerLocaleData(item.ng);
    this.nzI18nService.setLocale(item.zorro);
    this.nzI18nService.setDateLocale(item.date);
    this.delonLocaleService.setLocale(item.delon);
    I18N = kiwiIntl.init(lang, KIWI_LANGS as any);
    // I18N.setLang(lang);
    // I18N = kiwiIntl.init(lang, KIWI_LANGS);
    // console.log(lang);
  }

  get change(): Observable<string> {
    return this.change$.asObservable().pipe(filter(w => w != null)) as Observable<string>;
  }

  merge(translations: Object) {
    // I18N = kiwiIntl.init(this.getDefaultLang(), merge(KIWI_LANGS, translations) as any);
    merge(KIWI_LANGS, translations);
    // for (const iterator of Object.entries(KIWI_LANGS)) {
    //   this.translate.setTranslation(iterator[0], iterator[1], true);
    // }
  }
  use(lang: string): void {
    lang = lang || this.translate.getDefaultLang();
    if (this.currentLang === lang) {
      return;
    }
    console.log(lang);

    this.updateLangData(lang);
    this.translate.use(lang).subscribe(() => this.change$.next(lang));
  }
  /** 获取语言列表 */
  getLangs(): Array<{ code: string; text: string; abbr: string }> {
    return this._langs;
  }
  // /** 翻译 */
  fanyi(key: string, interpolateParams?: {}): any {
    let result = this.translate.instant(key, interpolateParams);
    if (key == result) {
      return `I18N.${key}`;
    }
    return result;
  }
  /** 默认语言 */
  get defaultLang(): string {
    return this._default;
  }
  /** 当前语言 */
  get currentLang(): string {
    return this.translate.currentLang || this.translate.getDefaultLang() || this._default;
  }
}

const i18n = kiwiIntl.init(DEFAULT, KIWI_LANGS);
function attachGetter([key, value]) {
  var parentKey = key;
  if (value instanceof Object) {
    const langObj = value as LangObject<object>;
    const result = Object.entries(value).forEach(([childKey, childValue]) => attachGetter([`${key}.${childKey}`, childValue]));
    langObj.get = function (childKey: string, notFoundValue?: string) {
      const result = value[childKey];
      if (result != undefined) {
        return result;
      }
      if (notFoundValue != undefined || notFoundValue != null) {
        return notFoundValue;
      }
      return `${parentKey}.${childKey}`;
    }.bind(value);
    return result;
  }
  return [];
}
Object.entries(KIWI_LANGS).forEach(x => {
  flatMapDeep(Object.entries(x[1]), ([key, value]) => {
    return attachGetter([`I18N.${key}`, value]);
  });
});
export let I18N = i18n;
