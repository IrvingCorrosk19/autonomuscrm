(function () {
  'use strict';

  function val(data, key) {
    if (!data) return undefined;
    if (data[key] !== undefined) return data[key];
    var camel = key.charAt(0).toLowerCase() + key.slice(1);
    return data[camel];
  }

  function formatEta(eta) {
    return eta || '—';
  }

  function updateProgressCard(root, data) {
    if (!root || !data) return;
    var pct = Math.min(100, Math.max(0, val(data, 'ProgressPercent') || 0));
    var jobId = val(data, 'JobId');
    root.querySelectorAll('[data-dh-pct]').forEach(function (el) { el.textContent = pct + '%'; });
    root.querySelectorAll('[data-dh-processed]').forEach(function (el) { el.textContent = val(data, 'ProcessedRows') ?? 0; });
    root.querySelectorAll('[data-dh-pending]').forEach(function (el) { el.textContent = val(data, 'PendingRows') ?? 0; });
    root.querySelectorAll('[data-dh-failed]').forEach(function (el) { el.textContent = val(data, 'FailedRows') ?? 0; });
    root.querySelectorAll('[data-dh-skipped]').forEach(function (el) { el.textContent = val(data, 'SkippedRows') ?? 0; });
    root.querySelectorAll('[data-dh-rpm]').forEach(function (el) { el.textContent = val(data, 'RowsPerMinute') ?? 0; });
    root.querySelectorAll('[data-dh-eta]').forEach(function (el) { el.textContent = formatEta(val(data, 'EstimatedRemaining')); });
    root.querySelectorAll('[data-dh-status]').forEach(function (el) { el.textContent = val(data, 'Status') || ''; });
    root.querySelectorAll('.progress-bar[data-dh-bar]').forEach(function (el) {
      el.style.width = pct + '%';
      el.setAttribute('aria-valuenow', pct);
    });
    if (jobId) {
      document.querySelectorAll('[data-dh-job-row="' + jobId + '"]').forEach(function (row) {
        var bar = row.querySelector('.progress-bar');
        if (bar) bar.style.width = pct + '%';
        var status = row.querySelector('[data-dh-row-status]');
        if (status) status.textContent = val(data, 'Status') || '';
        var pctEl = row.querySelector('[data-dh-pct]');
        if (pctEl) pctEl.textContent = pct + '%';
      });
    }
  }

  function init() {
    var roots = document.querySelectorAll('[data-dh-progress]');
    if (!roots.length || typeof signalR === 'undefined') return;

    var connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/datahub')
      .withAutomaticReconnect()
      .build();

    connection.on('ProgressUpdate', function (data) {
      roots.forEach(function (root) {
        var jobId = root.getAttribute('data-job-id');
        var tenantScope = root.getAttribute('data-tenant-monitor');
        var dataJobId = val(data, 'JobId');
        var dataTenantId = val(data, 'TenantId');
        if (jobId && dataJobId === jobId) updateProgressCard(root, data);
        if (tenantScope && dataTenantId === tenantScope) updateProgressCard(document, data);
      });
    });

    connection.start().then(function () {
      roots.forEach(function (root) {
        var jobId = root.getAttribute('data-job-id');
        var tenantId = root.getAttribute('data-tenant-id');
        if (jobId && tenantId) connection.invoke('SubscribeJob', jobId, tenantId);
        else if (tenantId) connection.invoke('SubscribeTenant', tenantId);
      });
    }).catch(function () { /* hub optional offline */ });
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
