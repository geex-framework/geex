import { List } from "linqts-camelcase";
declare global {
  interface String {
    contains(this: string, value: string): boolean;
  }
}

String.prototype.contains = function (this: string, value: string): boolean {
  return this.indexOf(value) >= 0;
};
