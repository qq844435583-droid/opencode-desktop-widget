'use strict';

(function attachRecentModels(root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  if (root) root.RecentModels = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, () => {
  function timestampOf(record) {
    const value = new Date(record?.time).getTime();
    return Number.isFinite(value) ? value : 0;
  }

  function normalizedModelName(record) {
    return String(record?.model || 'unknown').trim() || 'unknown';
  }

  function selectRecentUniqueModels(records, options = {}) {
    const scanLimit = Math.max(1, Number.parseInt(options.scanLimit, 10) || 10);
    const displayLimit = Math.max(1, Number.parseInt(options.displayLimit, 10) || 5);
    const ordered = (Array.isArray(records) ? records : [])
      .slice()
      .sort((left, right) => timestampOf(right) - timestampOf(left))
      .slice(0, scanLimit);

    const seen = new Set();
    const unique = [];
    for (const record of ordered) {
      const model = normalizedModelName(record);
      const key = model.toLocaleLowerCase();
      if (seen.has(key)) continue;
      seen.add(key);
      unique.push({ ...record, model });
      if (unique.length >= displayLimit) break;
    }
    return unique;
  }

  return { selectRecentUniqueModels };
});
