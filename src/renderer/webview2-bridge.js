(() => {
  'use strict';

  const webview = window.chrome?.webview;
  if (!webview) return;

  let sequence = 0;
  const pending = new Map();
  const listeners = new Map();

  function invoke(method, ...args) {
    const id = `wv2_${Date.now()}_${++sequence}`;
    return new Promise((resolve, reject) => {
      pending.set(id, { resolve, reject });
      webview.postMessage({ kind: 'request', id, method, args });
    });
  }

  function notify(method, ...args) {
    webview.postMessage({ kind: 'notify', method, args });
  }

  function on(eventName, callback) {
    if (!listeners.has(eventName)) listeners.set(eventName, new Set());
    listeners.get(eventName).add(callback);
    return () => listeners.get(eventName)?.delete(callback);
  }

  webview.addEventListener('message', event => {
    const message = event.data || {};
    if (message.kind === 'response') {
      const request = pending.get(message.id);
      if (!request) return;
      pending.delete(message.id);
      if (message.success) request.resolve(message.result);
      else request.reject(new Error(message.error || (document.documentElement.lang.startsWith('en') ? 'The WebView2 host call failed.' : 'WebView2 宿主调用失败。')));
      return;
    }
    if (message.kind === 'event') {
      for (const callback of listeners.get(message.event) || []) {
        try { callback(message.payload); } catch (error) { console.error(error); }
      }
    }
  });

  window.opencode = {
    bootstrap: () => invoke('app:bootstrap'),
    refresh: () => invoke('usage:refresh'),
    login: () => invoke('account:login'),
    saveAccount: payload => invoke('account:save', payload),
    deleteAccount: id => invoke('account:delete', id),
    switchAccount: id => invoke('account:switch', id),
    updateSettings: patch => invoke('settings:update', patch),
    toggleCompact: force => invoke('widget:toggle-compact', force),
    manageLicense: () => invoke('license:manage'),
    licenseStatus: () => invoke('license:status'),
    openWorkspace: () => invoke('workspace:open'),
    exportCsv: () => invoke('usage:export-csv'),
    openConfigFolder: () => invoke('config:open-folder'),
    setHover: hovering => notify('widget:hover', Boolean(hovering)),
    setCompactModelCount: count => notify('widget:compact-model-count', Number(count) || 1),
    windowClose: () => notify('window:close'),
    onUsage: callback => on('usage:updated', callback),
    onLogin: callback => on('account:login-state', callback),
    onWidgetState: callback => on('widget:state', callback),
    onLicense: callback => on('license:state', callback),
    onModelAlert: callback => on('model:alert', callback)
  };

  document.addEventListener('mousedown', event => {
    if (event.button !== 0) return;
    const target = event.target instanceof Element ? event.target : null;
    if (!target?.closest('.drag-region') || target.closest('.no-drag,button,input,textarea,select,a')) return;
    notify('window:drag');
  }, true);
})();
