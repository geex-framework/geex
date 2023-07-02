import { Maybe, OrgBriefFragment, OrgCacheItemFragment, Setting } from "../graphql/.generated/type";

interface SettingsDto {}
// declare global {
//   interface Window {
//     settings: SettingsDto;
//   }
// }

interface Window {
  // settings: SettingsDto;
  fullScreen: boolean;
  isMobile: boolean;
}
declare global {
  interface Window {
    settings: Array<Pick<Setting, "name" | "value">>;
    fullScreen: boolean;
    isMobile: boolean;
  }
}
Object.defineProperty(window, "isMobile", {
  get: () => window.innerHeight / window.innerWidth >= 1.5,
});
