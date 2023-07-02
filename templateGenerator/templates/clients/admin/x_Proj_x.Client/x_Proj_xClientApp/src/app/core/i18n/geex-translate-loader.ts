import { Injectable } from "@angular/core";
import { TranslateLoader } from "@ngx-translate/core";
import { Observable, of } from "rxjs";

import { I18N } from "./i18n.service";

@Injectable({ providedIn: "root" })
export class GeexTranslateLoader implements TranslateLoader {
  /**
   *
   */
  constructor() {}
  getTranslation(lang: string): Observable<any> {
    return of(I18N);
  }
}
