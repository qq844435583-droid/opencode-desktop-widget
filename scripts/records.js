(() => {
  'use strict';

  const SCHEMA_VERSION = 2;
  const maxRows = 50;
  const clean = value => String(value || '').replace(/\s+/g, ' ').trim();
  const lower = value => clean(value).toLowerCase();

  // 把网页里的"7月29日 下午4:44[:30]"之类中文/日文时间文本转成 ISO 字符串，
  // 方便 widget 用 new Date() 做相对时间、排序等处理。
  function parsePageTime(raw) {
    const text = clean(raw);
    if (!text) return '';

    // 已经是 ISO / RFC 字符串则直接返回
    if (!Number.isNaN(Date.parse(text))) return new Date(text).toISOString();

    // 匹配 "2025年7月29日 下午4:44[:30]" / "7月29日 下午4:44[:30]" 等变体
    const matched = text.match(/(?:(\d{2,4})年)?(\d{1,2})月(\d{1,2})日\s*(上午|下午|午前|午後|AM|PM|am|pm)?\s*(\d{1,2}):(\d{2})(?::(\d{2}))?/);
    if (matched) {
      const now = new Date();
      let year = matched[1] ? Number(matched[1]) : now.getFullYear();
      const month = Number(matched[2]);
      const day = Number(matched[3]);
      const ampm = matched[4] || '';
      let hour = Number(matched[5]);
      const minute = Number(matched[6]);
      const second = Number(matched[7] || 0);
      const lowerAmpm = ampm.toLowerCase();
      if ((lowerAmpm === '下午' || lowerAmpm === '午後' || lowerAmpm === 'pm') && hour < 12) hour += 12;
      if ((lowerAmpm === '上午' || lowerAmpm === '午前' || lowerAmpm === 'am') && hour === 12) hour = 0;

      const tryDate = new Date(year, month - 1, day, hour, minute, second);
      if (!Number.isNaN(tryDate.getTime())) {
        // 如果算出来的日期离现在太远（>60 天），可能是年份推断错了
        if (tryDate.getTime() - Date.now() > 60 * 86400_000) tryDate.setFullYear(year - 1);
        return tryDate.toISOString();
      }
    }
    return text;
  }

  const words = {
    time: ['日付', '日時', '時間', 'date', 'time', '日期', '时间'],
    model: ['モデル', 'model', '模型'],
    input: ['入力', 'input', 'prompt', '输入', '輸入'],
    output: ['出力', 'output', 'completion', '输出', '輸出'],
    cost: ['コスト', '費用', '料金', 'cost', '成本'],
    session: ['セッション', 'session', '会話', '会话', '工作階段']
  };

  const includesWord = (text, candidates) => candidates.some(word => lower(text).includes(lower(word)));
  const findIndex = (headers, candidates) => headers.findIndex(text => candidates.some(word => text.includes(lower(word))));
  const cellText = (cells, index) => index >= 0 ? clean(cells[index]?.textContent) : '';

  const looksLikeTime = text => {
    const value = clean(text);
    return /\d{1,2}月\d{1,2}日/.test(value)
      || /\d{4}[\/-]\d{1,2}[\/-]\d{1,2}/.test(value)
      || /\d{1,2}[\/-]\d{1,2}(?:[\/-]\d{2,4})?/.test(value)
      || /(?:上午|下午|午前|午後)?\s*\d{1,2}:\d{2}/.test(value);
  };

  const isModel = text => {
    const value = clean(text);
    if (!value || value.length > 120 || words.model.some(word => lower(word) === lower(value))) return false;
    return /^[a-z0-9][a-z0-9._:/+\-]*$/i.test(value) && /[a-z]/i.test(value);
  };

  const normalizeRows = rows => {
    const seen = new Set();
    return (rows || []).map(item => ({
      time: parsePageTime(item?.time),
      model: clean(item?.model),
      input: clean(item?.input),
      output: clean(item?.output),
      cost: clean(item?.cost),
      session: clean(item?.session)
    })).filter(item => item.time && item.model && isModel(item.model)).filter(item => {
      const key = [item.time, item.model, item.input, item.output, item.cost, item.session].map(lower).join('|');
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    }).slice(0, maxRows);
  };

  // v3: use semantic table headers and direct column mapping.
  function parseV3SemanticTable() {
    const candidates = [...document.querySelectorAll('table,[role="table"],[role="grid"]')];
    for (const table of candidates) {
      const rowElements = [...table.querySelectorAll('tr,[role="row"]')];
      const headerRow = rowElements.find(row => {
        const text = clean(row.innerText);
        return includesWord(text, words.model) && includesWord(text, words.time);
      });
      if (!headerRow) continue;

      const headerCells = [...headerRow.querySelectorAll('th,td,[role="columnheader"],[role="cell"],[role="gridcell"]')]
        .map(cell => lower(cell.textContent));
      const indexes = {
        time: findIndex(headerCells, words.time),
        model: findIndex(headerCells, words.model),
        input: findIndex(headerCells, words.input),
        output: findIndex(headerCells, words.output),
        cost: findIndex(headerCells, words.cost),
        session: findIndex(headerCells, words.session)
      };
      if (indexes.model < 0 || indexes.time < 0) continue;

      const rows = rowElements.filter(row => row !== headerRow).map(row => {
        const cells = [...row.querySelectorAll('th,td,[role="cell"],[role="gridcell"]')];
        return {
          time: cellText(cells, indexes.time),
          model: cellText(cells, indexes.model),
          input: cellText(cells, indexes.input),
          output: cellText(cells, indexes.output),
          cost: cellText(cells, indexes.cost),
          session: cellText(cells, indexes.session)
        };
      });
      const normalized = normalizeRows(rows);
      if (normalized.length) return normalized;
    }
    return [];
  }

  // v2: tolerate missing semantic roles and infer the header from the first suitable row.
  function parseV2CompatibleTable() {
    const tables = [...document.querySelectorAll('table,[role="table"],[role="grid"]')];
    for (const table of tables) {
      const tableText = clean(table.innerText);
      if (!includesWord(tableText, words.model) || !includesWord(tableText, words.time)) continue;
      const rowElements = [...table.querySelectorAll('tr,[role="row"]')];
      if (!rowElements.length) continue;

      const headerRow = rowElements.find(row => includesWord(row.innerText, words.model) && includesWord(row.innerText, words.time)) || rowElements[0];
      const headers = [...headerRow.querySelectorAll('th,td,[role="columnheader"],[role="cell"],[role="gridcell"],:scope > div')]
        .map(cell => lower(cell.textContent));
      const modelIndex = findIndex(headers, words.model);
      const timeIndex = findIndex(headers, words.time);
      if (modelIndex < 0 || timeIndex < 0) continue;

      const rows = rowElements.filter(row => row !== headerRow).map(row => {
        const cells = [...row.querySelectorAll('th,td,[role="cell"],[role="gridcell"],:scope > div')];
        return {
          time: cellText(cells, timeIndex),
          model: cellText(cells, modelIndex),
          input: cellText(cells, findIndex(headers, words.input)),
          output: cellText(cells, findIndex(headers, words.output)),
          cost: cellText(cells, findIndex(headers, words.cost)),
          session: cellText(cells, findIndex(headers, words.session))
        };
      });
      const normalized = normalizeRows(rows);
      if (normalized.length) return normalized;
    }
    return [];
  }

  // v1: last-resort text extraction. It intentionally returns fewer fields rather than failing completely.
  function parseV1GenericRows() {
    const rowLike = [...document.querySelectorAll('tr,[role="row"],main li,main [data-row],main [class*="row"]')];
    const rows = rowLike.map(row => {
      const cells = [...row.querySelectorAll(':scope > th,:scope > td,:scope > [role="cell"],:scope > [role="gridcell"],:scope > div,:scope > span')]
        .map(element => clean(element.textContent)).filter(value => value && value.length <= 180);
      const time = cells.find(looksLikeTime) || clean(row.innerText).match(/\d{4}[\/-]\d{1,2}[\/-]\d{1,2}[^\n]{0,30}/)?.[0] || '';
      const model = cells.find(value => isModel(value) && !looksLikeTime(value)) || '';
      return { time, model, input: '', output: '', cost: '', session: '' };
    });
    return normalizeRows(rows);
  }

  const parsers = [
    { version: 'records-v3', strategy: 'semantic-table', confidence: 'high', run: parseV3SemanticTable },
    { version: 'records-v2', strategy: 'compatible-table', confidence: 'medium', run: parseV2CompatibleTable },
    { version: 'records-v1', strategy: 'generic-row', confidence: 'low', run: parseV1GenericRows }
  ];

  const attempts = [];
  let selected = { version: 'records-none', strategy: 'none', confidence: 'none', items: [] };
  for (const parser of parsers) {
    try {
      const items = parser.run();
      attempts.push({ version: parser.version, count: items.length, error: '' });
      if (items.length) {
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
    degraded: selected.version !== 'records-v3' && selected.version !== 'records-none',
    items: selected.items,
    diagnostics: {
      attempts,
      tableCount: document.querySelectorAll('table,[role="table"],[role="grid"]').length,
      rowCount: document.querySelectorAll('tr,[role="row"]').length,
      documentReadyState: document.readyState
    }
  };
})()
