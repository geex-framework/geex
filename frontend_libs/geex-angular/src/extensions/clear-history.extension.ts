function clearHistory() {
  history.pushState(null, "", location.href);
  window.onpopstate = function () {
    history.go(1);
  };
}

window.clearHistory = clearHistory;

declare global {
  interface Window {
    clearHistory(): void;
  }
  function clearHistory(): void;
}

export {};
