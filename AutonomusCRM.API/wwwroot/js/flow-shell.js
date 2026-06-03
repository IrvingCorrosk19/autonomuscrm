/* AutonomusFlow — Shell & Command Palette v1 */
(function () {
  'use strict';

  var routes = [
    { name: 'Command', path: '/', group: 'Command' },
    { name: 'Trust Inbox', path: '/TrustInbox', group: 'Command' },
    { name: 'Workforce (Agentes)', path: '/Agents', group: 'Command' },
    { name: 'AI Command Center', path: '/AiCommandCenter', group: 'Command' },
    { name: 'Pipeline', path: '/Deals', group: 'Revenue' },
    { name: 'Leads', path: '/Leads', group: 'Commerce' },
    { name: 'Deals', path: '/Deals', group: 'Commerce' },
    { name: 'Clientes', path: '/Customers', group: 'Customers' },
    { name: 'Customer 360', path: '/Customer360', group: 'Customers' },
    { name: 'Integraciones', path: '/Integrations', group: 'Platform' },
    { name: 'Voice', path: '/VoiceCalls', group: 'Platform' },
    { name: 'Configuración', path: '/Settings', group: 'Admin' },
    { name: 'Usuarios', path: '/Users', group: 'Admin' },
    { name: 'Auditoría', path: '/Audit', group: 'Admin' },
    { name: 'Políticas', path: '/Policies', group: 'Admin' },
    { name: 'Tareas', path: '/Tasks', group: 'Operación' },
    { name: 'Workflows', path: '/Workflows', group: 'Operación' },
    { name: 'Soporte', path: '/Support', group: 'Operación' }
  ];

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

  function renderPalette(filter) {
    if (!paletteList) return;
    var q = (filter || '').toLowerCase().trim();
    var items = routes.filter(function (r) {
      return !q || r.name.toLowerCase().indexOf(q) >= 0 || r.group.toLowerCase().indexOf(q) >= 0;
    });
    selectedIndex = 0;
    paletteList.innerHTML = items.map(function (r, i) {
      return '<a class="flow-palette-item' + (i === 0 ? ' is-selected' : '') + '" href="' + r.path + '" role="option">' +
        r.name + '<small>' + r.group + '</small></a>';
    }).join('');
    if (items.length === 0) {
      paletteList.innerHTML = '<div style="padding:16px;color:var(--flow-text-muted);">Sin resultados</div>';
    }
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
