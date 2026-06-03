/* AutonomusFlow — World Class interactions */
(function () {
  'use strict';

  var THEME_KEY = 'flow-theme';

  function initTheme() {
    var stored = localStorage.getItem(THEME_KEY);
    var theme = stored || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    document.documentElement.setAttribute('data-flow-theme', theme === 'dark' ? 'dark' : 'light');
    var btn = document.getElementById('flow-theme-toggle');
    if (btn) {
      btn.textContent = theme === 'dark' ? 'Modo claro' : 'Modo oscuro';
      btn.setAttribute('aria-pressed', theme === 'dark' ? 'true' : 'false');
      btn.addEventListener('click', function () {
        var cur = document.documentElement.getAttribute('data-flow-theme') === 'dark' ? 'dark' : 'light';
        var next = cur === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-flow-theme', next);
        localStorage.setItem(THEME_KEY, next);
        btn.textContent = next === 'dark' ? 'Modo claro' : 'Modo oscuro';
        btn.setAttribute('aria-pressed', next === 'dark' ? 'true' : 'false');
      });
    }
  }

  function initKanban() {
    var board = document.getElementById('flow-deals-kanban');
    if (!board) return;
    var tenantId = board.getAttribute('data-tenant-id');
    var dragging = null;

    board.querySelectorAll('.flow-kanban-card').forEach(function (card) {
      card.setAttribute('draggable', 'true');
      card.addEventListener('dragstart', function (e) {
        dragging = card;
        card.classList.add('is-dragging');
        e.dataTransfer.setData('text/plain', card.getAttribute('data-deal-id') || '');
      });
      card.addEventListener('dragend', function () {
        card.classList.remove('is-dragging');
        dragging = null;
        board.querySelectorAll('.flow-kanban-dropzone').forEach(function (z) {
          z.classList.remove('is-drag-over');
        });
      });
    });

    board.querySelectorAll('.flow-kanban-dropzone').forEach(function (zone) {
      zone.addEventListener('dragover', function (e) {
        e.preventDefault();
        zone.classList.add('is-drag-over');
      });
      zone.addEventListener('dragleave', function () {
        zone.classList.remove('is-drag-over');
      });
      zone.addEventListener('drop', function (e) {
        e.preventDefault();
        zone.classList.remove('is-drag-over');
        if (!dragging) return;
        var dealId = dragging.getAttribute('data-deal-id');
        var stage = parseInt(zone.getAttribute('data-stage'), 10);
        if (!dealId || isNaN(stage)) return;
        zone.appendChild(dragging);
        updateDealStage(tenantId, dealId, stage);
      });
    });
  }

  function updateDealStage(tenantId, dealId, stage) {
    fetch('/api/deals/' + dealId + '/stage', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'same-origin',
      body: JSON.stringify({ dealId: dealId, tenantId: tenantId, stage: stage })
    }).then(function (r) {
      if (!r.ok) console.warn('Stage update failed', r.status);
    }).catch(function (err) { console.warn(err); });
  }

  function initDataTables() {
    document.querySelectorAll('[data-flow-datatable]').forEach(function (wrap) {
      var table = wrap.querySelector('table');
      var search = wrap.querySelector('.flow-datatable-search');
      if (!table || !search) return;
      search.addEventListener('input', function () {
        var q = search.value.toLowerCase();
        table.querySelectorAll('tbody tr[data-search]').forEach(function (row) {
          var text = (row.getAttribute('data-search') || '').toLowerCase();
          row.style.display = !q || text.indexOf(q) >= 0 ? '' : 'none';
        });
      });
      table.querySelectorAll('tbody tr[data-drawer]').forEach(function (row) {
        row.addEventListener('click', function (e) {
          if (e.target.closest('a, button, input')) return;
          openDrawer(row.getAttribute('data-drawer-title'), row.getAttribute('data-drawer-body'), row.getAttribute('data-drawer-href'));
          table.querySelectorAll('tbody tr').forEach(function (r) { r.classList.remove('is-selected'); });
          row.classList.add('is-selected');
        });
      });
    });
  }

  function openDrawer(title, bodyHtml, href) {
    var overlay = document.getElementById('flow-drawer-overlay');
    var drawer = document.getElementById('flow-drawer');
    if (!overlay || !drawer) return;
    var t = drawer.querySelector('[data-drawer-title]');
    var b = drawer.querySelector('[data-drawer-body]');
    var link = drawer.querySelector('[data-drawer-link]');
    if (t) t.textContent = title || '';
    if (b) b.textContent = bodyHtml || '';
    if (link) {
      if (href) { link.href = href; link.style.display = ''; }
      else { link.style.display = 'none'; }
    }
    overlay.classList.add('is-open');
    drawer.classList.add('is-open');
    drawer.setAttribute('aria-hidden', 'false');
  }

  function closeDrawer() {
    var overlay = document.getElementById('flow-drawer-overlay');
    var drawer = document.getElementById('flow-drawer');
    if (overlay) overlay.classList.remove('is-open');
    if (drawer) {
      drawer.classList.remove('is-open');
      drawer.setAttribute('aria-hidden', 'true');
    }
  }

  window.FlowUI = window.FlowUI || {};
  window.FlowUI.openDrawer = openDrawer;
  window.FlowUI.closeDrawer = closeDrawer;

  document.addEventListener('click', function (e) {
    if (e.target.matches('[data-flow-drawer-close]') || e.target.id === 'flow-drawer-overlay') {
      closeDrawer();
    }
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeDrawer();
  });

  initTheme();
  initKanban();
  initDataTables();
})();
