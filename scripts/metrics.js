(() => {
  'use strict';

  const SCHEMA_VERSION = 1;
  const clean = value => String(value || '').replace(/\s+/g, ' ').trim();
  const normalize = value => clean(value).toLowerCase();
  const definitions = [
    { key: 'rolling', label: '滚动用量', terms: ['ローリング利用量', 'rolling usage', 'rolling limit', '滚动用量'] },
    { key: 'weekly', label: '周用量', terms: ['週間利用量', 'weekly usage', 'weekly limit', '周用量'] },
    { key: 'monthly', label: '月用量', terms: ['月間利用量', 'monthly usage', 'monthly limit', '月用量'] }
  ];
  const resetTerms = ['リセット', 'reset', '重置'];
  const allTerms = definitions.flatMap(item => item.terms.map(normalize));
  const matches = (text, terms) => terms.some(term => normalize(text).includes(normalize(term)));

  const ownText = element => clean([...element.childNodes]
    .filter(node => node.nodeType === Node.TEXT_NODE)
    .map(node => node.textContent).join(' '));

  function extractPercent(card) {
    if (!card) return null;
    const text = clean(card.innerText);
    const percentMatch = text.match(/(\d+(?:[.,]\d+)?)\s*%/);
    if (percentMatch) return Math.max(0, Math.min(100, Number(percentMatch[1].replace(',', '.'))));

    const progress = card.querySelector('[role="progressbar"],progress');
    if (progress) {
      const raw = progress.getAttribute('aria-valuenow') || progress.value;
      const number = Number(raw);
      if (Number.isFinite(number)) return Math.max(0, Math.min(100, number));
    }

    const widthElement = [...card.querySelectorAll('[style*="width"]')]
      .find(element => /\d+(?:[.,]\d+)?%/.test(element.style.width));
    if (widthElement) return Math.max(0, Math.min(100, Number(widthElement.style.width.replace('%', '').replace(',', '.'))));
    return null;
  }

  function extractReset(card) {
    if (!card) return '';
    const raw = String(card.innerText || '');
    const lines = raw.split(/\n+/).map(clean).filter(Boolean);
    const line = lines.find(value => resetTerms.some(term => normalize(value).includes(normalize(term))));
    if (line) return line;
    const match = clean(raw).match(/(?:リセットまで|resets?\s+in|reset\s+in|重置(?:还剩|倒计时)?)[^%]{0,80}/i);
    return clean(match?.[0] || '');
  }

  function findCardFromLabel(labelElement) {
    let node = labelElement;
    let best = labelElement;
    for (let depth = 0; node && depth < 8; depth += 1, node = node.parentElement) {
      const text = clean(node.innerText);
      if (!text || text.length > 700) continue;
      const labelCount = allTerms.filter(term => normalize(text).includes(term)).length;
      const hasPercent = /\d+(?:[.,]\d+)?\s*%/.test(text);
      const hasProgress = Boolean(node.querySelector('[role="progressbar"],progress'));
      const hasReset = resetTerms.some(term => normalize(text).includes(normalize(term)));
      if (labelCount <= 1 && (hasPercent || hasProgress || hasReset)) best = node;
      if (labelCount <= 1 && (hasPercent || hasProgress) && hasReset) return node;
    }
    return best;
  }

  // v3: locate a short semantic label, then inspect only its nearest usage card.
  function parseV3SemanticCards() {
    const elements = [...document.querySelectorAll('body *')];
    const items = definitions.map(definition => {
      const labelElement = elements.find(element => {
        const direct = ownText(element);
        if (direct && direct.length <= 80 && matches(direct, definition.terms)) return true;
        const full = clean(element.textContent);
        return element.children.length === 0 && full.length <= 80 && matches(full, definition.terms);
      });
      if (!labelElement) return { key: definition.key, label: definition.label, percent: null, reset: '', found: false };
      const card = findCardFromLabel(labelElement);
      return { key: definition.key, label: definition.label, percent: extractPercent(card), reset: extractReset(card), found: true };
    });
    return items.some(item => item.found && item.percent !== null) ? items : [];
  }

  // v2: start from progress bars and map each one to a known usage label in its ancestors.
  function parseV2ProgressBars() {
    const result = new Map();
    const progressElements = [...document.querySelectorAll('[role="progressbar"],progress,[style*="width"]')];
    for (const progress of progressElements) {
      let node = progress;
      for (let depth = 0; node && depth < 7; depth += 1, node = node.parentElement) {
        const text = clean(node.innerText);
        if (!text || text.length > 700) continue;
        const definition = definitions.find(item => matches(text, item.terms));
        if (!definition || result.has(definition.key)) continue;
        const percent = extractPercent(node);
        if (percent === null) continue;
        result.set(definition.key, { key: definition.key, label: definition.label, percent, reset: extractReset(node), found: true });
        break;
      }
    }
    if (!result.size) return [];
    return definitions.map(definition => result.get(definition.key) || { key: definition.key, label: definition.label, percent: null, reset: '', found: false });
  }

  // v1: search bounded text blocks containing a known label and a percentage.
  function parseV1TextFallback() {
    const result = new Map();
    const blocks = [...document.querySelectorAll('main div,main section,body > div')];
    for (const block of blocks) {
      const text = clean(block.innerText);
      if (!text || text.length > 500) continue;
      const definition = definitions.find(item => matches(text, item.terms));
      if (!definition || result.has(definition.key)) continue;
      const percentMatch = text.match(/(\d+(?:[.,]\d+)?)\s*%/);
      if (!percentMatch) continue;
      result.set(definition.key, {
        key: definition.key,
        label: definition.label,
        percent: Math.max(0, Math.min(100, Number(percentMatch[1].replace(',', '.')))),
        reset: extractReset(block),
        found: true
      });
    }
    if (!result.size) return [];
    return definitions.map(definition => result.get(definition.key) || { key: definition.key, label: definition.label, percent: null, reset: '', found: false });
  }

  const parsers = [
    { version: 'metrics-v3', strategy: 'semantic-card', confidence: 'high', run: parseV3SemanticCards },
    { version: 'metrics-v2', strategy: 'progress-ancestor', confidence: 'medium', run: parseV2ProgressBars },
    { version: 'metrics-v1', strategy: 'bounded-text', confidence: 'low', run: parseV1TextFallback }
  ];

  const attempts = [];
  let selected = { version: 'metrics-none', strategy: 'none', confidence: 'none', items: [] };
  for (const parser of parsers) {
    try {
      const items = parser.run();
      const foundCount = items.filter(item => item?.found).length;
      attempts.push({ version: parser.version, count: foundCount, error: '' });
      if (foundCount) {
        selected = { ...parser, items };
        break;
      }
    } catch (error) {
      attempts.push({ version: parser.version, count: 0, error: clean(error?.message || error).slice(0, 160) });
    }
  }

  return {
    schemaVersion: SCHEMA_VERSION,
    parserVersion: selected.version,
    strategy: selected.strategy,
    confidence: selected.confidence,
    degraded: selected.version !== 'metrics-v3' && selected.version !== 'metrics-none',
    items: selected.items,
    diagnostics: {
      attempts,
      progressCount: document.querySelectorAll('[role="progressbar"],progress').length,
      documentReadyState: document.readyState
    }
  };
})()
