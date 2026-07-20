import { Injectable, inject } from "@angular/core";

import { GEEX_BLOCK_DEBUGGER } from "./debugger-blocker.tokens";

@Injectable({
  providedIn: "root",
})
export class DebuggerBlockerService {
  private readonly enabled = inject(GEEX_BLOCK_DEBUGGER, { optional: true }) ?? false;

  init(): void {
    if (!this.enabled) {
      return;
    }
    this.blockDebugger();
    this.disableDevToolsShortcuts();
  }

  private blockDebugger(): void {
    setInterval(() => {
      eval(`
        if (window.outerHeight - window.innerHeight > 160 || window.outerWidth - window.innerWidth > 160) {
          alert("Debugger detected! Please close dev tools to continue.")
          location.reload();
        }
        `);
    }, 1000);
  }

  private disableDevToolsShortcuts(): void {
    document.addEventListener(
      "keydown",
      e => {
        if (e.key === "F12") {
          e.preventDefault();
          e.stopPropagation();
          return;
        }
        if (e.ctrlKey && e.shiftKey && ["I", "J", "C"].includes(e.key)) {
          e.preventDefault();
          e.stopPropagation();
        }
      },
      true,
    );
  }
}
