import { ActivatedRouteSnapshot } from "@angular/router";
import { STComponent, STExportOptions } from "@delon/abc/st";
import { TitleService } from "@delon/theme";
import { deepCopy } from "@delon/util";
import { Observable } from "rxjs";

declare module "@delon/abc/st/public_api" {
  interface STComponent {
    exportAll: (opt?: STExportOptions) => Promise<void>;
    $exportData: Observable<any[]>;
  }
}

function exportAll(this: STComponent, opt?: STExportOptions) {
  return this.$exportData.toPromise().then(x => this.export(deepCopy(x), opt));
}
STComponent.prototype.exportAll = exportAll;

declare module "@delon/theme/src/services/title/title.service" {
  interface TitleService {
    updateTitle: (route: ActivatedRouteSnapshot, fallback?: string) => any | Promise<any>;
  }
}

function updateTitle(this: TitleService, route: ActivatedRouteSnapshot, fallback?: string) {
  let title = this["getByRoute"]() || this["getByMenu"]() || this["getByElement"]() || { text: fallback } || this.default;
  this.setTitle(title.text);
}
TitleService.prototype.updateTitle = updateTitle;
