(function () {
  'use strict';

  function val(obj, key) {
    if (!obj) return undefined;
    if (obj[key] !== undefined) return obj[key];
    var camel = key.charAt(0).toLowerCase() + key.slice(1);
    return obj[camel];
  }

  function setText(root, selector, text) {
    var el = root.querySelector(selector);
    if (el) el.textContent = text;
  }

  function setBar(root, pct) {
    var bar = root.querySelector('[data-dbi-bar]');
    if (!bar) return;
    bar.style.width = pct + '%';
    bar.setAttribute('aria-valuenow', pct);
  }

  function updateProgress(root, stage, pct, message) {
    setText(root, '[data-dbi-stage]', stage || '—');
    setText(root, '[data-dbi-pct]', (pct ?? 0) + '%');
    setText(root, '[data-dbi-message]', message || '');
    setBar(root, pct ?? 0);
  }

  function toggleStudios() {
    document.querySelectorAll('[data-studio]').forEach(function (panel) {
      var key = panel.getAttribute('data-studio');
      var checkbox = document.querySelector('input[data-action="' + key + '"]');
      panel.hidden = !(checkbox && checkbox.checked);
    });
  }

  function initStudios() {
    document.querySelectorAll('input[data-action]').forEach(function (cb) {
      cb.addEventListener('change', toggleStudios);
    });
    toggleStudios();
  }

  function initSignalR() {
    var root = document.querySelector('[data-dbi-operate-progress]');
    if (!root || typeof signalR === 'undefined') return;

    var jobId = root.getAttribute('data-job-id');
    var tenantId = root.getAttribute('data-tenant-id');
    if (!jobId || !tenantId) return;

    var connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/db-intelligence')
      .withAutomaticReconnect()
      .build();

    connection.on('OperationStarted', function () {
      updateProgress(root, 'Analyzing', 10, 'Operation started');
    });

    connection.on('OperationProgress', function (payload) {
      var progress = val(payload, 'progress') || payload;
      updateProgress(
        root,
        val(progress, 'Stage') || val(progress, 'stage'),
        val(progress, 'ProgressPercent') ?? val(progress, 'progressPercent'),
        val(progress, 'Message') || val(progress, 'message'));
    });

    connection.on('OperationCompleted', function (payload) {
      var result = val(payload, 'result') || payload;
      updateProgress(root, 'Completed', 100, 'Operation completed');
      if (result) {
        setText(root, '[data-dbi-imported]', val(result, 'ImportedRows') ?? val(result, 'importedRows') ?? 0);
      }
    });

    connection.on('OperationFailed', function (payload) {
      updateProgress(root, 'Failed', 0, val(payload, 'message') || 'Operation failed');
    });

    connection.start()
      .then(function () { return connection.invoke('SubscribeOperationJob', jobId, tenantId); })
      .catch(function () { /* hub optional */ });
  }

  function init() {
    initStudios();
    initSignalR();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
