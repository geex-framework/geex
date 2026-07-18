import { inject, Injectable } from "@angular/core";
import { TranslateLoader, TranslationObject } from "@ngx-translate/core";
import { Observable, of } from "rxjs";

import { GEEX_I18N_PACKS } from "./tokens";

@Injectable({ providedIn: "root" })
export class GeexTranslateLoader implements TranslateLoader {
  private readonly packs = inject(GEEX_I18N_PACKS, { optional: true });

  getTranslation(lang: string): Observable<TranslationObject> {
    if (this.packs?.[lang]) {
      return of(this.packs[lang] as TranslationObject);
    }
    return of({});
  }
}
