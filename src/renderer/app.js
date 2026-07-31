'use strict';

const PAGE_SIZE = 10;
const RECORD_LIMIT = 50;
const I18N = {
  'zh-CN': {
    refreshNow: '立即刷新', settings: '设置', loginSwitch: '登录 / 切换账户', hideToTray: '隐藏到托盘', compactOverview: '收起状态概览',
    weekShort: '周', monthShort: '月', noModelRecords: '暂无模型记录', connectOpenCode: '连接 OpenCode',
    connectDescription: '登录后自动显示三项已用额度和最近 50 条调用。', webLogin: '网页登录',
    rollingLimit: '滚动额度', fiveHours: '5 小时', weeklyLimit: '每周额度', sevenDays: '7 天', monthlyLimit: '每月额度', thirtyDays: '30 天',
    waitingData: '等待数据', used: '已用', requests: '请求', input: '输入', cache: '缓存', recentCalls: '最近调用', openOpenCode: '打开 OpenCode', noCalls: '暂无调用记录',
    previousPage: '上一页', nextPage: '下一页', widgetSettings: '挂件设置', settingsSubtitle: '简洁、清晰、贴边不打扰',
    interfaceLanguage: '界面语言', languageDescription: '只翻译软件界面，OpenCode 数据保持官网原文',
    refreshFrequency: '刷新频率', refreshDescription: '自定义秒数，默认 60 秒', seconds: '秒',
    edgeHide: '贴边自动隐藏', edgeHideDescription: '拖到顶部或右侧即可缩进去', alwaysOnTop: '始终置顶', alwaysOnTopDescription: '保持在其他窗口上方',
    launchAtLogin: '开机启动', launchDescription: '登录系统后自动运行', quotaAlerts: '额度提醒', quotaAlertsDescription: '剩余额度低于阈值时通知',
    remainingThreshold: '剩余阈值', ngAlerts: 'NG 模型警报', ngAlertsDescription: '检测到 NG 模型时响铃并弹出系统通知',
    modelRules: 'OK / NG 模型判断', modelRulesDescription: '每行一个模型；支持 * 通配符，NG 优先', allowedModels: '允许模型', alertModels: '警报模型',
    rulesPlaceholder: '例如：\ngpt-5*\nclaude-sonnet-4.5', ngRulesPlaceholder: '例如：\ndeepseek-v4*\nunknown-model',
    loginAddAccount: '登录 / 添加账户', done: '完成', settingsTip: '贴边后鼠标靠近会自动展开。模型规则不区分大小写；相同记录只警报一次。',
    switchToEnglish: '切换到英语', switchToChinese: '切换到中文', unlockExpanded: '解锁展开模式', expand: '展开', collapse: '收起',
    notConnected: '未连接', justNow: '刚刚', minutesAgo: '{count} 分钟', hoursAgo: '{count} 小时', daysAgo: '{count} 天',
    resetUnknown: '重置时间未知', resetDays: '{days} 天 {hours} 小时后重置', resetHours: '{hours} 小时 {minutes} 分钟后重置', resetMinutes: '{minutes} 分钟后重置',
    updating: '更新中', updatingLong: '正在更新…', syncingUsage: '正在同步用量…', updatedAt: '更新 {time} · {countdown}', updateAfter: '{countdown} 后更新', waitingConnection: '等待连接',
    refreshFailed: '刷新失败', invalidRefresh: '刷新秒数请输入 10–86400。', saveFailed: '保存失败', settingsSaved: '设置已保存',
    freeCompactOnly: '免费版只能使用紧凑窗口，输入授权码后可展开。', syncFailed: '同步失败', completeLogin: '请在登录窗口完成登录', accountConnected: '账户连接成功', loginFailed: '登录失败',
    proActivated: '专业版已激活，完整窗口已解锁。', licenseRemoved: '授权已解除，已恢复免费紧凑模式。', unknownModel: '未知模型', modelAlert: 'NG 模型警报：{models}{count}',
    recordCountSuffix: '（{count} 条）', initFailed: '初始化失败：{error}'
  },
  'en-US': {
    refreshNow: 'Refresh now', settings: 'Settings', loginSwitch: 'Sign in / switch account', hideToTray: 'Hide to tray', compactOverview: 'Compact usage overview',
    weekShort: 'W', monthShort: 'M', noModelRecords: 'No recent models', connectOpenCode: 'Connect OpenCode',
    connectDescription: 'Sign in to show all three usage limits and the latest 50 calls.', webLogin: 'Sign in on the web',
    rollingLimit: 'Rolling limit', fiveHours: '5 hours', weeklyLimit: 'Weekly limit', sevenDays: '7 days', monthlyLimit: 'Monthly limit', thirtyDays: '30 days',
    waitingData: 'Waiting for data', used: 'used', requests: 'Requests', input: 'Input', cache: 'Cache', recentCalls: 'Recent calls', openOpenCode: 'Open OpenCode', noCalls: 'No call records',
    previousPage: 'Previous page', nextPage: 'Next page', widgetSettings: 'Widget settings', settingsSubtitle: 'Clean, clear, and unobtrusive',
    interfaceLanguage: 'Interface language', languageDescription: 'Only the app UI is translated; OpenCode data stays exactly as provided',
    refreshFrequency: 'Refresh interval', refreshDescription: 'Custom interval in seconds; default is 60', seconds: 'sec',
    edgeHide: 'Auto-hide at screen edge', edgeHideDescription: 'Drag to the top or right edge to tuck it away', alwaysOnTop: 'Always on top', alwaysOnTopDescription: 'Keep the widget above other windows',
    launchAtLogin: 'Launch at sign-in', launchDescription: 'Start automatically after Windows sign-in', quotaAlerts: 'Usage alerts', quotaAlertsDescription: 'Notify when remaining usage drops below the threshold',
    remainingThreshold: 'Remaining threshold', ngAlerts: 'NG model alert', ngAlertsDescription: 'Play a sound and show a notification when an NG model is detected',
    modelRules: 'OK / NG model rules', modelRulesDescription: 'One model per line; * wildcards supported; NG takes priority', allowedModels: 'Allowed models', alertModels: 'Alert models',
    rulesPlaceholder: 'Example:\ngpt-5*\nclaude-sonnet-4.5', ngRulesPlaceholder: 'Example:\ndeepseek-v4*\nunknown-model',
    loginAddAccount: 'Sign in / add account', done: 'Done', settingsTip: 'Move the pointer near a hidden edge to reveal the widget. Rules are case-insensitive; each record alerts only once.',
    switchToEnglish: 'Switch to English', switchToChinese: 'Switch to Chinese', unlockExpanded: 'Unlock expanded mode', expand: 'Expand', collapse: 'Collapse',
    notConnected: 'Not connected', justNow: 'Just now', minutesAgo: '{count} min', hoursAgo: '{count} hr', daysAgo: '{count} days',
    resetUnknown: 'Reset time unavailable', resetDays: 'Resets in {days} days {hours} hours', resetHours: 'Resets in {hours} hours {minutes} minutes', resetMinutes: 'Resets in {minutes} minutes',
    updating: 'Updating', updatingLong: 'Updating…', syncingUsage: 'Syncing usage…', updatedAt: 'Updated {time} · {countdown}', updateAfter: 'Updates in {countdown}', waitingConnection: 'Waiting to connect',
    refreshFailed: 'Refresh failed', invalidRefresh: 'Enter a refresh interval from 10 to 86400 seconds.', saveFailed: 'Save failed', settingsSaved: 'Settings saved',
    freeCompactOnly: 'The free edition only supports compact mode. Enter a license key to expand.', syncFailed: 'Sync failed', completeLogin: 'Complete sign-in in the login window', accountConnected: 'Account connected', loginFailed: 'Sign-in failed',
    proActivated: 'Pro is activated. Expanded mode is unlocked.', licenseRemoved: 'The license was removed. Free compact mode is active.', unknownModel: 'Unknown model', modelAlert: 'NG model alert: {models}{count}',
    recordCountSuffix: ' ({count} records)', initFailed: 'Initialization failed: {error}'
  }
};

function normalizeLanguage(value) {
  return String(value || '').toLowerCase().startsWith('en') ? 'en-US' : 'zh-CN';
}

function currentLanguage() {
  return normalizeLanguage(state?.settings?.language);
}

function t(key, values = {}) {
  const table = I18N[currentLanguage()] || I18N['zh-CN'];
  let text = table[key] ?? I18N['zh-CN'][key] ?? key;
  for (const [name, value] of Object.entries(values)) text = text.replaceAll(`{${name}}`, String(value));
  return text;
}

function applyLanguage() {
  const language = currentLanguage();
  document.documentElement.lang = language;
  document.querySelectorAll('[data-i18n]').forEach(element => { element.textContent = t(element.dataset.i18n); });
  document.querySelectorAll('[data-i18n-title]').forEach(element => {
    const text = t(element.dataset.i18nTitle);
    element.title = text;
    element.setAttribute('aria-label', text);
  });
  document.querySelectorAll('[data-i18n-aria]').forEach(element => element.setAttribute('aria-label', t(element.dataset.i18nAria)));
  document.querySelectorAll('[data-i18n-placeholder]').forEach(element => element.setAttribute('placeholder', t(element.dataset.i18nPlaceholder)));
  const languageButton = $('#languageButton');
  if (languageButton) {
    const english = language === 'en-US';
    languageButton.querySelector('span').textContent = english ? '中' : 'EN';
    languageButton.title = english ? t('switchToChinese') : t('switchToEnglish');
    languageButton.setAttribute('aria-label', languageButton.title);
  }
  const select = $('#languageSelect');
  if (select) select.value = language;
}

const now = Date.now();
const demoModels = [
  'deepseek-v4-pro',       'gpt-5.4',              'claude-sonnet-4.5',     'gemini-2.5-pro',        'mimo-v2.5',
  'deepseek-v4-flash',     'gpt-4o-2025-11-20',    'claude-opus-4.5',       'qwen-3-max',            'gemini-2.5-flash',
  'gpt-5.4-mini',          'deepseek-r1-0528',     'claude-sonnet-4',       'llama-4-maverick',      'mistral-large-2',
  'gpt-4o-mini-2025-07',   'gemini-2.0-flash',     'grok-3',                'deepseek-v3-0324',      'claude-3.5-sonnet',
  'command-r-plus',        'qwen-2.5-coder-32b',   'gpt-4.5-preview',       'phi-4-multimodal',      'gemma-3-27b',
  'deepseek-coder-v2',     'claude-3.5-haiku',     'mixtral-8x22b',         'gpt-5-nano',            'yi-large-turbo',
  'gemini-1.5-pro',        'deepseek-v4-0620',     'gpt-4-turbo',           'codestral-22b',         'llama-3.1-405b',
  'claude-opus-4',         'gemini-2.5-pro-exp',   'qwen-2.5-72b',          'gpt-4.1-mini',          'mistral-nemo',
  'deepseek-v3',           'claude-3-opus',        'gemini-1.5-flash',      'phi-3.5-vision',        'command-r',
  'gpt-3.5-turbo',         'llama-3.1-70b',        'qwen-2-vl-72b',         'gemma-2-27b',           'deepseek-r1'
];
const demoRecords = demoModels.map((model, index) => ({
  id: String(index + 1),
  time: new Date(now - (index * 8 + 2) * 60_000).toISOString(),
  model,
  cost: (index < 5 ? 0.018 : index < 15 ? 0.012 : index < 30 ? 0.008 : index < 40 ? 0.005 : 0.003)
    + (index % 5) * 0.0015 + Math.sin(index) * 0.002
}));
const demoUsage = {
  workspaceId: 'wrk_demo_liquid_glass',
  summary: {
    rolling: { usedPercent: 28, remainingPercent: 72, resetInSec: 2 * 3600 + 42 * 60 },
    weekly: { usedPercent: 45, remainingPercent: 55, resetInSec: 3 * 86400 + 5 * 3600 },
    monthly: { usedPercent: 61, remainingPercent: 39, resetInSec: 11 * 86400 + 9 * 3600 }
  },
  records: demoRecords,
  detail: { count: 50, totalInput: 592000, totalCache: 2666000 },
  source: 'demo',
  fetchedAt: new Date().toISOString()
};
const demoState = {
  activeAccountId: 'demo',
  accounts: [{ id: 'demo', name: 'Main', workspaceId: demoUsage.workspaceId, hasAuth: true }],
  settings: {
    refreshSeconds: 60,
    launchAtLogin: false,
    notifications: true,
    warningThreshold: 25,
    language: navigator.language?.toLowerCase().startsWith('en') ? 'en-US' : 'zh-CN',
    compact: false,
    alwaysOnTop: true,
    edgeHide: true,
    modelOkRules: ['gpt-5*', 'claude-*', 'gemini-*'],
    modelNgRules: ['deepseek-v4*'],
    ngAlertEnabled: true
  },
  usage: demoUsage,
  license: { isValid: true, isPro: true, edition: 'pro', deviceCode: 'DEMO-0000-0000-0000-0000', message: 'Pro activated.' },
  nextRefreshAt: Date.now() + 60_000,
  appVersion: '3.3.0 Preview',
  platform: 'browser'
};
const demoBridge = {
  bootstrap: async () => structuredClone(demoState),
  refresh: async () => {
    demoState.nextRefreshAt = Date.now() + demoState.settings.refreshSeconds * 1000;
    return { ok: true, data: { ...structuredClone(demoUsage), fetchedAt: new Date().toISOString() }, nextRefreshAt: demoState.nextRefreshAt };
  },
  login: async () => ({ ok: true }),
  updateSettings: async patch => {
    Object.assign(demoState.settings, patch);
    demoState.nextRefreshAt = Date.now() + demoState.settings.refreshSeconds * 1000;
    return { ok: true, settings: demoState.settings, nextRefreshAt: demoState.nextRefreshAt };
  },
  toggleCompact: async force => {
    demoState.settings.compact = typeof force === 'boolean' ? force : !demoState.settings.compact;
    return { ok: true, compact: demoState.settings.compact };
  },
  manageLicense: async () => ({ ok: true, license: demoState.license, compact: demoState.settings.compact, settings: demoState.settings }),
  licenseStatus: async () => ({ ok: true, license: demoState.license, compact: demoState.settings.compact }),
  openWorkspace: async () => {},
  setHover: () => {},
  setCompactModelCount: () => {},
  windowClose: () => {},
  onUsage: () => () => {},
  onLogin: () => () => {},
  onWidgetState: () => () => {},
  onLicense: () => () => {},
  onModelAlert: () => () => {}
};

const bridge = window.opencode || demoBridge;
const $ = selector => document.querySelector(selector);
let state = structuredClone(demoState);
let toastTimer = null;
let countdownTimer = null;
let currentPage = 1;
let isRefreshing = false;
let lastCompactModelCount = null;

function escapeHtml(value) {
  return String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[char]));
}

function activeAccount() {
  return state.accounts?.find(item => item.id === state.activeAccountId) || null;
}

function formatNumber(value) {
  const number = Number(value);
  if (!Number.isFinite(number)) return '—';
  if (number >= 1_000_000) return `${(number / 1_000_000).toFixed(number >= 10_000_000 ? 0 : 1)}M`;
  if (number >= 1_000) return `${(number / 1_000).toFixed(number >= 100_000 ? 0 : 1)}K`;
  return Math.round(number).toLocaleString(currentLanguage());
}

function formatCost(value) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) return '—';
  const normalized = number > 1000 ? number / 10_000_000 : number;
  return `$${normalized < .01 ? normalized.toFixed(3) : normalized.toFixed(2)}`;
}

function relativeTime(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return t('justNow');
  const seconds = Math.max(0, Math.floor((Date.now() - date.getTime()) / 1000));
  if (seconds < 60) return t('justNow');
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return t('minutesAgo', { count: minutes });
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return t('hoursAgo', { count: hours });
  return t('daysAgo', { count: Math.floor(hours / 24) });
}

function clockText(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat(currentLanguage(), { hour: '2-digit', minute: '2-digit', hour12: false }).format(date);
}

function formatRecordTime(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  const now = new Date();
  const isToday = date.getFullYear() === now.getFullYear() && date.getMonth() === now.getMonth() && date.getDate() === now.getDate();
  if (isToday) return clockText(value);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const time = clockText(value);
  return `${month}-${day} ${time}`;
}

function formatReset(seconds, resetText) {
  // resetText comes from OpenCode and must remain in the website's original language.
  if (resetText) return resetText;
  const total = Number(seconds);
  if (!Number.isFinite(total) || total <= 0) return t('resetUnknown');
  const days = Math.floor(total / 86400);
  const hours = Math.floor((total % 86400) / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  if (days) return t('resetDays', { days, hours });
  if (hours) return t('resetHours', { hours, minutes });
  return t('resetMinutes', { minutes: Math.max(1, minutes) });
}

function usedPercent(item) {
  const used = Number(item?.usedPercent ?? item?.usagePercent);
  if (Number.isFinite(used)) return Math.max(0, Math.min(100, used));
  const remaining = Number(item?.remainingPercent);
  return Number.isFinite(remaining) ? Math.max(0, Math.min(100, 100 - remaining)) : null;
}

function normalizeRules(value) {
  const parts = Array.isArray(value) ? value : String(value ?? '').split(/[\n,;，；]+/);
  const seen = new Set();
  return parts.map(item => String(item ?? '').trim()).filter(item => {
    const key = item.toLocaleLowerCase('en-US');
    if (!item || seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

function matchesModelRule(model, rule) {
  const subject = String(model ?? '').trim();
  const pattern = String(rule ?? '').trim();
  if (!subject || !pattern) return false;
  const escaped = pattern.replace(/[|\\{}()[\]^$+?.]/g, '\\$&').replace(/\*/g, '.*');
  try { return new RegExp(`^${escaped}$`, 'i').test(subject); }
  catch { return subject.toLocaleLowerCase('en-US') === pattern.toLocaleLowerCase('en-US'); }
}

function modelStatus(record) {
  const model = record?.model || '';
  const ngRules = normalizeRules(state.settings?.modelNgRules);
  const okRules = normalizeRules(state.settings?.modelOkRules);
  if (ngRules.some(rule => matchesModelRule(model, rule))) return 'ng';
  if (okRules.some(rule => matchesModelRule(model, rule))) return 'ok';
  if (!ngRules.length && !okRules.length && ['ng', 'ok'].includes(record?.modelStatus)) return record.modelStatus;
  return 'unknown';
}

function statusLabel(status) {
  return status === 'ng' ? 'NG' : status === 'ok' ? 'OK' : '—';
}

function formatCountdown(seconds) {
  const safe = Math.max(0, Math.ceil(Number(seconds) || 0));
  if (safe < 60) return `${safe}s`;
  const minutes = Math.floor(safe / 60);
  const rest = safe % 60;
  if (minutes < 60) return `${minutes}m ${String(rest).padStart(2, '0')}s`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h ${String(minutes % 60).padStart(2, '0')}m`;
}

function showToast(message, type = 'info') {
  clearTimeout(toastTimer);
  const toast = $('#toast');
  toast.textContent = message;
  toast.className = `toast ${type}`;
  toastTimer = setTimeout(() => toast.classList.add('hidden'), 3200);
}

function setRefreshing(value) {
  isRefreshing = Boolean(value);
  $('#refreshButton').classList.toggle('refreshing', isRefreshing);
  $('#refreshButton').disabled = isRefreshing;
  updateCountdown();
}

function updateCountdown() {
  const refreshSeconds = Math.max(10, Number(state.settings?.refreshSeconds) || 60);
  if (!Number.isFinite(Number(state.nextRefreshAt)) || Number(state.nextRefreshAt) <= 0) {
    state.nextRefreshAt = Date.now() + refreshSeconds * 1000;
  }
  const remaining = Math.max(0, Math.ceil((Number(state.nextRefreshAt) - Date.now()) / 1000));
  const countdown = isRefreshing ? t('updating') : formatCountdown(remaining);
  $('#autoRefreshText').textContent = isRefreshing ? t('updatingLong') : t('updateAfter', { countdown });
  $('#compactCountdown').textContent = isRefreshing ? t('updating') : countdown;

  const fetchedAt = state.usage?.fetchedAt;
  if (isRefreshing) $('#updateText').textContent = t('syncingUsage');
  else if (fetchedAt) $('#updateText').textContent = t('updatedAt', { time: clockText(fetchedAt), countdown });
  else $('#updateText').textContent = activeAccount() ? t('updateAfter', { countdown }) : t('waitingConnection');
}

function applyLicense() {
  const isPro = Boolean(state.license?.isPro);
  document.body.classList.toggle('free-tier', !isPro);
  if (!isPro) state.settings.compact = true;
}

function applyCompact(compact) {
  const effectiveCompact = state.license?.isPro ? Boolean(compact) : true;
  state.settings.compact = effectiveCompact;
  document.body.classList.toggle('compact', effectiveCompact);
  const button = $('#compactButton');
  button.title = !state.license?.isPro ? t('unlockExpanded') : effectiveCompact ? t('expand') : t('collapse');
  button.setAttribute('aria-label', button.title);
  if (effectiveCompact) $('#settingsPanel').classList.add('hidden');
}

function renderAccount() {
  const account = activeAccount();
  $('#accountName').textContent = account?.name || t('notConnected');
  $('#workspaceText').textContent = account?.workspaceId || 'OpenCode Go';
  $('#accountAvatar').textContent = (account?.name || 'O').trim().charAt(0).toUpperCase();
  $('#connectionDot').classList.toggle('online', Boolean(account));
  const noAccount = !account;
  $('#emptyState').classList.toggle('hidden', !noAccount);
  $('#usageList').classList.toggle('hidden', noAccount);
  $('#quickStats').classList.toggle('hidden', noAccount);
  $('.records-section').classList.toggle('hidden', noAccount);
  $('.widget-footer').classList.toggle('hidden', noAccount);
}

function recentModelRecords() {
  const records = window.RecentModels.selectRecentUniqueModels(state.usage?.records, {
    scanLimit: 10,
    displayLimit: 5
  });
  return records.map(record => ({
    model: String(record?.model || 'unknown').trim() || 'unknown',
    status: modelStatus(record),
    time: record?.time
  }));
}

function renderCompactModels() {
  const records = recentModelRecords();
  const modelCount = Math.max(1, records.length);
  $('#compactModels').innerHTML = records.length
    ? records.map(record => `
      <div class="compact-model ${record.status}" title="${escapeHtml(record.model)}">
        <span class="compact-status">${statusLabel(record.status)}</span>
        <strong>${escapeHtml(record.model)}</strong>
        <small>${escapeHtml(formatRecordTime(record.time))}</small>
      </div>`).join('')
    : `<span class="compact-model empty">${escapeHtml(t('noModelRecords'))}</span>`;
  if (modelCount !== lastCompactModelCount) {
    lastCompactModelCount = modelCount;
    bridge.setCompactModelCount?.(modelCount);
  }
}

function renderUsage() {
  renderAccount();
  const usage = state.usage;
  const account = activeAccount();

  if (!usage) {
    for (const key of ['rolling', 'weekly', 'monthly']) {
      const row = $(`.usage-row[data-key="${key}"]`);
      row.querySelector('.used-value').textContent = '—';
      row.querySelector('.reset-text').textContent = t('waitingData');
      row.querySelector('.progress-fill').style.width = '0%';
      $(`[data-compact="${key}"]`).textContent = '—';
    }
    $('#requestCount').textContent = '—';
    $('#inputTotal').textContent = '—';
    $('#cacheTotal').textContent = '—';
    currentPage = 1;
    renderRecords();
    renderCompactModels();
    updateCountdown();
    return;
  }

  for (const key of ['rolling', 'weekly', 'monthly']) {
    const item = usage.summary?.[key] || {};
    const used = usedPercent(item);
    const display = Number.isFinite(used) ? `${Math.round(used)}%` : '—';
    const row = $(`.usage-row[data-key="${key}"]`);
    row.querySelector('.used-value').textContent = display;
    row.querySelector('.reset-text').textContent = formatReset(item.resetInSec, item.resetText);
    row.querySelector('.progress-fill').style.width = `${Number.isFinite(used) ? used : 0}%`;
    row.classList.toggle('warning', Number.isFinite(used) && used >= 75 && used < 90);
    row.classList.toggle('danger', Number.isFinite(used) && used >= 90);
    $(`[data-compact="${key}"]`).textContent = display;
  }

  $('#requestCount').textContent = formatNumber(usage.detail?.count ?? usage.records?.length);
  $('#inputTotal').textContent = formatNumber(usage.detail?.totalInput);
  $('#cacheTotal').textContent = formatNumber(usage.detail?.totalCache);
  renderRecords();
  renderCompactModels();
  updateCountdown();
}

function renderRecords() {
  const records = Array.isArray(state.usage?.records) ? state.usage.records.slice(0, RECORD_LIMIT) : [];
  const pageCount = Math.max(1, Math.min(5, Math.ceil(records.length / PAGE_SIZE)));
  currentPage = Math.max(1, Math.min(currentPage, pageCount));
  const start = (currentPage - 1) * PAGE_SIZE;
  const pageRecords = records.slice(start, start + PAGE_SIZE);
  const rows = pageRecords.map((item, index) => {
    const status = modelStatus(item);
    return `
    <div class="record-item ${status}">
      <div class="record-model"><span class="model-dot" style="opacity:${1 - index * .045}"></span><strong title="${escapeHtml(item.model || 'unknown')}">${escapeHtml(item.model || 'unknown')}</strong></div>
      <span class="record-status">${statusLabel(status)}</span>
      <span class="record-time">${escapeHtml(formatRecordTime(item.time))}</span>
      <span class="record-cost">${escapeHtml(formatCost(item.cost))}</span>
    </div>`;
  });
  while (rows.length < PAGE_SIZE && records.length) rows.push('<div class="record-placeholder"></div>');
  $('#recordList').innerHTML = records.length ? rows.join('') : `<div class="record-empty">${escapeHtml(t('noCalls'))}</div>`;
  $('#recordCountText').textContent = `${records.length} / ${RECORD_LIMIT}`;
  $('#pageText').textContent = `${currentPage} / ${pageCount}`;
  $('#prevPageButton').disabled = currentPage <= 1;
  $('#nextPageButton').disabled = currentPage >= pageCount;
}

function renderSettings() {
  const settings = state.settings || {};
  $('#languageSelect').value = normalizeLanguage(settings.language);
  $('#refreshInterval').value = String(settings.refreshSeconds || 60);
  $('#edgeHide').checked = settings.edgeHide !== false;
  $('#alwaysOnTop').checked = settings.alwaysOnTop !== false;
  $('#launchAtLogin').checked = Boolean(settings.launchAtLogin);
  $('#notifications').checked = Boolean(settings.notifications);
  $('#ngAlertEnabled').checked = settings.ngAlertEnabled !== false;
  $('#okModelRules').value = normalizeRules(settings.modelOkRules).join('\n');
  $('#ngModelRules').value = normalizeRules(settings.modelNgRules).join('\n');
  $('#warningThreshold').value = String(settings.warningThreshold || 25);
  $('#thresholdValue').textContent = `${settings.warningThreshold || 25}%`;
}

function openSettings() {
  renderSettings();
  $('#settingsPanel').classList.remove('hidden');
}

function closeSettings() {
  $('#settingsPanel').classList.add('hidden');
}

async function refresh() {
  if (!activeAccount()) return bridge.login();
  setRefreshing(true);
  try {
    const result = await bridge.refresh();
    if (!result?.ok) throw new Error(result?.error || t('refreshFailed'));
    if (result.data) state.usage = result.data;
    if (result.nextRefreshAt) state.nextRefreshAt = result.nextRefreshAt;
    currentPage = 1;
    renderUsage();
  } catch (error) {
    showToast(error.message || String(error), 'error');
  } finally {
    setRefreshing(false);
  }
}

async function saveSettings() {
  const refreshSeconds = Math.round(Number($('#refreshInterval').value));
  if (!Number.isFinite(refreshSeconds) || refreshSeconds < 10 || refreshSeconds > 86400) {
    showToast(t('invalidRefresh'), 'error');
    return;
  }
  const patch = {
    refreshSeconds,
    language: normalizeLanguage($('#languageSelect').value),
    edgeHide: $('#edgeHide').checked,
    alwaysOnTop: $('#alwaysOnTop').checked,
    launchAtLogin: $('#launchAtLogin').checked,
    notifications: $('#notifications').checked,
    ngAlertEnabled: $('#ngAlertEnabled').checked,
    modelOkRules: normalizeRules($('#okModelRules').value),
    modelNgRules: normalizeRules($('#ngModelRules').value),
    warningThreshold: Number($('#warningThreshold').value)
  };
  try {
    const result = await bridge.updateSettings(patch);
    if (!result?.ok) throw new Error(result?.error || t('saveFailed'));
    state.settings = { ...state.settings, ...result.settings };
    if (result.nextRefreshAt) state.nextRefreshAt = result.nextRefreshAt;
    applyLanguage();
    renderUsage();
    closeSettings();
    showToast(t('settingsSaved'));
  } catch (error) {
    showToast(error.message || String(error), 'error');
  }
}

async function changeLanguage(language) {
  const next = normalizeLanguage(language);
  const previous = currentLanguage();
  if (next === previous) return;
  state.settings.language = next;
  applyLanguage();
  applyCompact(state.settings.compact);
  renderSettings();
  renderUsage();
  try {
    const result = await bridge.updateSettings({ language: next });
    if (!result?.ok) throw new Error(result?.error || t('saveFailed'));
    state.settings = { ...state.settings, ...result.settings };
    applyLanguage();
  } catch (error) {
    state.settings.language = previous;
    applyLanguage();
    renderSettings();
    renderUsage();
    showToast(error.message || String(error), 'error');
  }
}

function bindEvents() {
  $('#refreshButton').addEventListener('click', refresh);
  $('#languageButton').addEventListener('click', () => changeLanguage(currentLanguage() === 'en-US' ? 'zh-CN' : 'en-US'));
  $('#settingsButton').addEventListener('click', openSettings);
  $('#compactLoginButton').addEventListener('click', () => bridge.login());
  $('#settingsCloseButton').addEventListener('click', closeSettings);
  $('#saveSettingsButton').addEventListener('click', saveSettings);
  $('#closeButton').addEventListener('click', () => bridge.windowClose());
  $('#loginButton').addEventListener('click', () => bridge.login());
  $('#switchAccountButton').addEventListener('click', () => bridge.login());
  $('#openWorkspaceButton').addEventListener('click', () => bridge.openWorkspace());
  $('#compactButton').addEventListener('click', async () => {
    try {
      const result = await bridge.toggleCompact(!state.settings.compact);
      if (result?.license) state.license = result.license;
      if (result?.settings) state.settings = { ...state.settings, ...result.settings };
      applyLicense();
      applyCompact(result?.compact ?? state.settings.compact);
      if (result?.locked) showToast(t('freeCompactOnly'));
    } catch (error) {
      showToast(error.message || String(error), 'error');
    }
  });
  $('#prevPageButton').addEventListener('click', () => {
    currentPage = Math.max(1, currentPage - 1);
    renderRecords();
  });
  $('#nextPageButton').addEventListener('click', () => {
    currentPage = Math.min(5, currentPage + 1);
    renderRecords();
  });
  $('#warningThreshold').addEventListener('input', event => {
    $('#thresholdValue').textContent = `${event.target.value}%`;
  });

  window.addEventListener('mouseenter', () => bridge.setHover?.(true));
  window.addEventListener('mouseleave', () => bridge.setHover?.(false));

  bridge.onUsage(payload => {
    if (payload?.nextRefreshAt) state.nextRefreshAt = payload.nextRefreshAt;
    if (payload?.loading) return setRefreshing(true);
    setRefreshing(false);
    if (payload?.ok && payload.data) {
      state.usage = payload.data;
      currentPage = 1;
      renderUsage();
    } else if (payload && !payload.ok) {
      showToast(payload.error || t('syncFailed'), 'error');
    }
  });
  bridge.onLogin(payload => {
    if (payload?.started) return showToast(t('completeLogin'));
    if (payload?.ok && payload.account) {
      const index = state.accounts.findIndex(item => item.id === payload.account.id);
      if (index >= 0) state.accounts[index] = payload.account;
      else state.accounts.push(payload.account);
      state.activeAccountId = payload.account.id;
      closeSettings();
      renderUsage();
      showToast(t('accountConnected'));
    } else if (payload && !payload.ok) showToast(payload.error || t('loginFailed'), 'error');
  });
  bridge.onLicense?.(payload => {
    if (payload?.license) state.license = payload.license;
    if (payload?.settings) state.settings = { ...state.settings, ...payload.settings };
    applyLicense();
    applyCompact(payload?.compact ?? state.settings.compact);
    if (payload?.changed) showToast(state.license?.isPro ? t('proActivated') : t('licenseRemoved'));
  });
  bridge.onModelAlert?.(payload => {
    const models = Array.isArray(payload?.models) ? payload.models.slice(0, 3).join(currentLanguage() === 'en-US' ? ', ' : '、') : t('unknownModel');
    document.body.classList.remove('ng-alarm');
    void document.body.offsetWidth;
    document.body.classList.add('ng-alarm');
    setTimeout(() => document.body.classList.remove('ng-alarm'), 3600);
    const count = Number(payload?.count) > 1 ? t('recordCountSuffix', { count: payload.count }) : '';
    showToast(t('modelAlert', { models, count }), 'error');
  });
  bridge.onWidgetState(payload => {
    if (payload?.settings) state.settings = { ...state.settings, ...payload.settings };
    if (payload?.license) state.license = payload.license;
    applyLicense();
    applyLanguage();
    if (payload?.nextRefreshAt) state.nextRefreshAt = payload.nextRefreshAt;
    if (typeof payload?.compact === 'boolean') applyCompact(payload.compact);
    renderSettings();
    updateCountdown();
  });
}

async function init() {
  bindEvents();
  try {
    const bootstrap = await bridge.bootstrap();
    state = {
      ...state,
      ...bootstrap,
      settings: { ...state.settings, ...(bootstrap.settings || {}) }
    };
  } catch (error) {
    showToast(t('initFailed', { error: error.message || error }), 'error');
  }
  applyLicense();
  applyLanguage();
  applyCompact(state.settings.compact);
  renderSettings();
  renderUsage();
  clearInterval(countdownTimer);
  countdownTimer = setInterval(updateCountdown, 250);
}

init();
