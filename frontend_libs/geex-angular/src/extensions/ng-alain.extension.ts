import { ActivatedRouteSnapshot } from "@angular/router";
import { ReuseTabService } from "@delon/abc/reuse-tab";
import { STComponent, STExportOptions } from "@delon/abc/st";
import { TitleService } from "@delon/theme";
import { deepCopy } from "@delon/util";
import * as abc from "@delon/abc";
import { Observable } from "rxjs";
declare module "@delon/abc/st" {
  interface STComponent {
    exportAll: (opt?: STExportOptions) => Promise<void>;
    $exportData: Observable<any[]>;
  }
}

function exportAll(this: STComponent, opt?: STExportOptions) {
  return this.$exportData.firstValuePromise().then(x => this.export(deepCopy(x), opt));
}
STComponent.prototype.exportAll = exportAll;
