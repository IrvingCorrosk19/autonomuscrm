/* AutonomusFlow — Shell & Command Palette v1 */
(function () {
  'use strict';

  var i18n = (window.__flowI18n && window.__flowI18n.strings) || {};
  function t(key, fallback) {
    return i18n[key] || fallback || key;
  }

  var routes = ((window.__flowI18n && window.__flowI18n.routes) || []).map(function (r) {
    return { name: r.name, path: r.path, group: r.group };
  });

  var app = document.getElementById('flow-app');
  var palette = document.getElementById('flow-palette');
  var paletteInput = document.getElementById('flow-palette-input');
  var paletteList = document.getElementById('flow-palette-list');
  var selectedIndex = 0;

  function toggleSidebar() {
    if (!app) return;
    if (window.innerWidth < 1024) {
      app.classList.toggle('flow-sidebar-mobile-open');
    } else {
      app.classList.toggle('flow-sidebar-collapsed');
    }
  }

  var toggleBtn = document.getElementById('flow-sidebar-toggle');
  if (toggleBtn) {
    toggleBtn.addEventListener('click', function (e) {
      e.preventDefault();
      toggleSidebar();
    });
  }

  var backdrop = document.getElementById('flow-sidebar-backdrop');
  if (backdrop) {
    backdrop.addEventListener('click', function () {
      app && app.classList.remove('flow-sidebar-mobile-open');
    });
  }

  var userMenu = document.getElementById('flow-topbar-user');
  var userBtn = document.getElementById('flow-user-menu-btn');
  if (userBtn && userMenu) {
    userBtn.addEventListener('click', function () {
      userMenu.classList.toggle('is-open');
      userBtn.setAttribute('aria-expanded', userMenu.classList.contains('is-open'));
    });
    document.addEventListener('click', function (e) {
      if (!userMenu.contains(e.target)) {
        userMenu.classList.remove('is-open');
        userBtn.setAttribute('aria-expanded', 'false');
      }
    });
  }

  function paletteItemHtml(href, title, sub, type, idx) {
    return '<a class="flow-palette-item' + (idx === 0 ? ' is-selected' : '') + '" href="' + href + '" role="option">' +
      title + '<small class="flow-palette-type">' + (type || '') + '</small><small>' + (sub || '') + '</small></a>';
  }

  function renderPaletteList(entries) {
    if (!paletteList) return;
    selectedIndex = 0;
    if (!entries.length) {
      paletteList.innerHTML = '<div style="padding:16px;color:var(--flow-text-muted);">' + t('noResults', 'No results') + '</div>';
      return;
    }
    paletteList.innerHTML = entries.map(function (e, i) {
      return paletteItemHtml(e.href, e.title, e.sub, e.type, i);
    }).join('');
  }

  var paletteSearchToken = 0;
  function renderPalette(filter) {
    if (!paletteList) return;
    var q = (filter || '').trim();
    var ql = q.toLowerCase();
    var entries = routes.filter(function (r) {
      return !ql || r.name.toLowerCase().indexOf(ql) >= 0 || r.group.toLowerCase().indexOf(ql) >= 0;
    }).map(function (r) {
      return { href: r.path, title: r.name, sub: r.group, type: t('typeRoute', 'route') };
    });

    renderPaletteList(entries);

    if (q.length < 2) return;
    var token = ++paletteSearchToken;
    fetch('/api/flow/search?q=' + encodeURIComponent(q), { credentials: 'same-origin' })
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (data) {
        if (!data || token !== paletteSearchToken) return;
        var merged = entries.slice();
        function add(list, type) {
          (list || []).forEach(function (x) {
            merged.push({ href: x.href, title: x.title, sub: x.subtitle || '', type: type });
          });
        }
        add(data.leads, t('typeLead', 'lead'));
        add(data.customers, t('typeCustomer', 'customer'));
        add(data.deals, t('typeDeal', 'deal'));
        add(data.routes, t('typeCommand', 'command'));
        renderPaletteList(merged.slice(0, 24));
      })
      .catch(function () { /* rutas locales ya mostradas */ });
  }

  function openPalette() {
    if (!palette) return;
    palette.hidden = false;
    palette.classList.add('is-open');
    renderPalette('');
    if (paletteInput) {
      paletteInput.value = '';
      paletteInput.focus();
    }
  }

  function closePalette() {
    if (!palette) return;
    palette.classList.remove('is-open');
    palette.hidden = true;
  }

  var openBtn = document.getElementById('flow-palette-open');
  if (openBtn) {
    openBtn.addEventListener('click', openPalette);
  }

  if (palette) {
    palette.addEventListener('click', function (e) {
      if (e.target === palette) closePalette();
    });
  }

  if (paletteInput) {
    paletteInput.addEventListener('input', function () {
      renderPalette(paletteInput.value);
    });
    paletteInput.addEventListener('keydown', function (e) {
      var links = paletteList.querySelectorAll('.flow-palette-item');
      if (e.key === 'Escape') {
        closePalette();
        return;
      }
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        selectedIndex = Math.min(selectedIndex + 1, links.length - 1);
        updateSelection(links);
      }
      if (e.key === 'ArrowUp') {
        e.preventDefault();
        selectedIndex = Math.max(selectedIndex - 1, 0);
        updateSelection(links);
      }
      if (e.key === 'Enter' && links[selectedIndex]) {
        e.preventDefault();
        window.location.href = links[selectedIndex].getAttribute('href');
      }
    });
  }

  function updateSelection(links) {
    for (var i = 0; i < links.length; i++) {
      links[i].classList.toggle('is-selected', i === selectedIndex);
    }
  }

  document.addEventListener('keydown', function (e) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
      e.preventDefault();
      if (palette && palette.classList.contains('is-open')) closePalette();
      else openPalette();
    }
  });

  var pageEnter = document.querySelector('.flow-page-enter');
  if (pageEnter && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    pageEnter.style.opacity = '0';
    pageEnter.style.transform = 'translateY(4px)';
    requestAnimationFrame(function () {
      pageEnter.style.transition = 'opacity 200ms ease, transform 200ms ease';
      pageEnter.style.opacity = '1';
      pageEnter.style.transform = 'translateY(0)';
    });
  }
})();
