import "./array.extension";

import "./string.extension";

import "./signal.extension";

import "./rxjs.extension";

import "./angular.extension";

import "./ng-alain.extension";

import "./misc.extension";

import "./file.extension";



export { deepSignal, computedAsync } from "./signal.extension";

export type { DeepSignal, WritableDeepSignal } from "./signal.extension";

export {

  assertIsDefined,

  assertIsArray,

  assert,

  assertIsNotArray,

  deepProxy,

  isRecord,

  extract,

} from "./misc.extension";

export type { PropertyAccessType } from "./misc.extension";



export function provideGeexExtensions(): void {

  // side-effect imports above run when this module is loaded

}

