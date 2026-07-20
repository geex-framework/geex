import { registerLocaleData } from "@angular/common";
import ngEn from "@angular/common/locales/en";
import ngZh from "@angular/common/locales/zh";
import { Injectable, inject } from "@angular/core";
import {
  AlainI18NService,
  DelonLocaleService,
  en_US as delonEnUS,
  SettingsService,
  zh_CN as delonZhCn,
} from "@delon/theme";
import { TranslateService } from "@ngx-translate/core";
import { enUS as dfEn, zhCN as dfZhCn } from "date-fns/locale";
import kiwiIntl from "kiwi-intl";
import { flatMapDeep, merge } from "lodash-es";
import { NzSafeAny } from "ng-zorro-antd/core/types";
import { en_US as zorroEnUS, NzI18nService, zh_CN as zorroZhCN } from "ng-zorro-antd/i18n";
import { BehaviorSubject, Observable } from "rxjs";
import { filter } from "rxjs/operators";

import { GEEX_I18N_PACKS } from "./tokens";
import type { LangObject } from "./types";

interface LangData {
  abbr: string;
  text: string;
  ng: NzSafeAny;
  zorro: NzSafeAny;
  date: NzSafeAny;
  delon: NzSafeAny;
}

const DEFAULT = "zh-cn";

const LANGS: { [key: string]: LangData } = {
  "zh-cn": {
    text: "简体中文",
    ng: ngZh,
    zorro: zorroZhCN,
    date: dfZhCn,
    delon: delonZhCn,
    abbr: "🇨🇳",
  },
  "en-us": {
    text: "English",
    ng: ngEn,
    zorro: zorroEnUS,
    date: dfEn,
    delon: delonEnUS,
    abbr: "🇺🇸",
  },
};

/** Mutable kiwi dictionary; host may re-export as `I18N`. */
export let I18N: LangObject<any>;

function attachGetter([key, value]: [string, unknown]): unknown {
  const parentKey = key;
  if (value instanceof Object) {
    const langObj = value as LangObject<object>;
    Object.entries(value as object).forEach(([childKey, childValue]) =>
      attachGetter([`${key}.${childKey}`, childValue]),
    );
    langObj.get = function (this: Record<string, unknown>, childKey: string, notFoundValue?: string) {
      const result = this[childKey];
      if (result != undefined) {
        return result as string;
      }
      if (notFoundValue != undefined || notFoundValue != null) {
        return notFoundValue;
      }
      return `${parentKey}.${childKey}`;
    }.bind(value as Record<string, unknown>);
  }
  return [];
}

function attachGettersToPacks(packs: Record<string, Record<string, unknown>>): void {
  Object.entries(packs).forEach(([, pack]) => {
    flatMapDeep(Object.entries(pack), ([key, value]) => attachGetter([`I18N.${key}`, value]));
  });
}

/**
 * Alain + kiwi i18n runtime. Packs come from `GEEX_I18N_PACKS` (host zh-CN/en-US assembly).
 */
@Injectable()
export class GeexI18nService implements AlainI18NService {
  private _default = DEFAULT;
  private change$ = new BehaviorSubject<string | null>(null);
  private kiwiLangs: Record<string, Record<string, unknown>>;

  private _langs = Object.keys(LANGS).map(code => {
    const item = LANGS[code];
    return { code, text: item.text, abbr: item.abbr };
  });

  private readonly settings = inject(SettingsService);
  private readonly nzI18nService = inject(NzI18nService);
  private readonly delonLocaleService = inject(DelonLocaleService);
  private readonly translate = inject(TranslateService);
  private readonly packs = inject(GEEX_I18N_PACKS);

  constructor() {
    this.kiwiLangs = this.packs ?? {};
    attachGettersToPacks(this.kiwiLangs);
    I18N = kiwiIntl.init(DEFAULT, this.kiwiLangs as any);

    const lans = this._langs.map(item => item.code);
    this.translate.addLangs(lans);

    const defaultLan = this.getDefaultLang().toLowerCase();
    this._default = lans.includes(defaultLan) ? defaultLan : DEFAULT;
    this.use(this._default);
  }

  /** Current kiwi dictionary (also mirrored by module `I18N`). */
  get dictionary(): LangObject<any> {
    return I18N;
  }

  private getDefaultLang(): string {
    if (this.settings.layout.lang) {
      return this.settings.layout.lang;
    }
    return (navigator.languages?.[0] || navigator.language || DEFAULT).toLowerCase();
  }

  private updateLangData(lang: string): void {
    const item = LANGS[lang.toLocaleLowerCase()] ?? LANGS[DEFAULT];
    registerLocaleData(item.ng);
    this.nzI18nService.setLocale(item.zorro);
    this.nzI18nService.setDateLocale(item.date);
    this.delonLocaleService.setLocale(item.delon);
    I18N = kiwiIntl.init(lang, this.kiwiLangs as any);
  }

  get change(): Observable<string> {
    return this.change$.asObservable().pipe(filter(w => w != null)) as Observable<string>;
  }

  merge(translations: object): void {
    merge(this.kiwiLangs, translations);
  }

  use(lang: string): void {
    lang = lang || this.translate.getDefaultLang() || this._default;
    if (this.currentLang === lang) {
      return;
    }
    this.updateLangData(lang);
    this.translate.use(lang).subscribe(() => this.change$.next(lang));
  }

  getLangs(): Array<{ code: string; text: string; abbr: string }> {
    return this._langs;
  }

  fanyi(key: string, interpolateParams?: {}): any {
    const result = this.translate.instant(key, interpolateParams);
    if (key == result) {
      return `I18N.${key}`;
    }
    return result;
  }

  get defaultLang(): string {
    return this._default;
  }

  get currentLang(): string {
    return this.translate.currentLang || this.translate.getDefaultLang() || this._default;
  }
}

/** Host-compatible alias. */
export { GeexI18nService as I18NService };
