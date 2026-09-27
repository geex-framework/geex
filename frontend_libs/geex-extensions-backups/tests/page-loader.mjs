import { readFile } from "node:fs/promises";
import ts from "typescript";

const pageUrl = new URL("../../geex-module-schematics/files/extensions/backups/pages/list.page.ts", import.meta.url).href;
const mocks = {
  "rxjs": `export { Subscription } from '${new URL("../node_modules/rxjs/dist/cjs/index.js", import.meta.url).href}';`,
  "@angular/core": `
    export const Component = () => target => target;
    export const ViewChild = () => () => {};
    export class TemplateRef {};
    export class DestroyRef {};
    export const inject = token => globalThis.backupPageTest.services[token.name ?? token];
    export { signal } from '${new URL("../node_modules/@angular/core/fesm2022/core.mjs", import.meta.url).href}';`,
  "@geexcode/geex-angular": `export class GeexSubscriptionConnection {}; export const GEEX_I18N = 'GEEX_I18N'; export const geex = { get backups() { return globalThis.backupPageTest.api; }, get messaging() { return globalThis.backupPageTest.messaging; } };`,
};
const tokens = {
  "@angular/router": ["Router", "NavigationEnd"], "angular-oauth2-oidc": ["OAuthService"],
  "@angular/common": ["DatePipe"], "@angular/forms": ["FormsModule"],
  "@delon/abc/st": ["STModule"], "@delon/abc/page-header": ["PageHeaderModule"], "@delon/acl": ["ACLService"],
  "ng-zorro-antd/message": ["NzMessageService"], "ng-zorro-antd/modal": ["NzModalService"],
  ...Object.fromEntries(["form", "input", "button", "card", "select", "tag", "alert", "empty"].map(name =>
    [`ng-zorro-antd/${name}`, [`Nz${name[0].toUpperCase()}${name.slice(1)}Module`]])),
  "ng-zorro-antd/date-picker": ["NzDatePickerModule"],
};
for (const [name, names] of Object.entries(tokens)) mocks[name] = names.map(token => `export class ${token} {}`).join("\n");

export async function resolve(specifier, context, nextResolve) {
  if (context.parentURL === pageUrl && mocks[specifier])
    return { url: `data:text/javascript,${encodeURIComponent(mocks[specifier])}`, shortCircuit: true };
  return nextResolve(specifier, context);
}

export async function load(url, context, nextLoad) {
  if (url === pageUrl || url.endsWith("/backups/i18n/en-US.ts")) {
    const source = await readFile(new URL(url), "utf8");
    return { format: "module", source: ts.transpileModule(source, {
      compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022, experimentalDecorators: true },
    }).outputText, shortCircuit: true };
  }
  return nextLoad(url, context);
}
