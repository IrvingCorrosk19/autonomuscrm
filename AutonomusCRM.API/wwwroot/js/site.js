// AutonomusCRM — AdminLTE helpers
$(function () {
  if (typeof $.fn.tooltip !== 'undefined') {
    $('[data-toggle="tooltip"]').tooltip();
  }

  // Wrap raw tables for horizontal fallback.
  $('table.table').each(function () {
    var $table = $(this);
    if ($table.parent('.table-responsive').length === 0) {
      $table.wrap('<div class="table-responsive"></div>');
    }
  });

  // Add mobile labels from table headers.
  $('table.table').each(function () {
    var headers = [];
    $(this).find('thead th').each(function () {
      headers.push($(this).text().trim());
    });
    $(this).find('tbody tr').each(function () {
      $(this).find('td').each(function (idx) {
        if (!$(this).attr('data-label') && headers[idx]) {
          $(this).attr('data-label', headers[idx]);
        }
      });
    });
  });

  // Make bootstrap modals scroll-safe on small devices.
  $('.modal-dialog').addClass('modal-dialog-scrollable');

  // Keyboard row navigation for operational tables.
  $('table.table tbody tr').attr('tabindex', '0');
  $('table.table tbody tr').on('keydown', function (event) {
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
    event.preventDefault();
    var $rows = $(this).closest('tbody').find('tr');
    var currentIndex = $rows.index(this);
    var nextIndex = event.key === 'ArrowDown' ? currentIndex + 1 : currentIndex - 1;
    if (nextIndex >= 0 && nextIndex < $rows.length) {
      $rows.eq(nextIndex).focus();
    }
  });

  // Density toggle — persisted for long-session operational tables.
  function applyTableDensity(mode) {
    var compact = mode === 'compact';
    document.body.classList.toggle('crm-density-compact', compact);
    var $toggle = $('.crm-density-toggle');
    if ($toggle.length) {
      $toggle.find('button').removeClass('active');
      $toggle.find('button').eq(compact ? 0 : 1).addClass('active');
    }
  }
  var savedDensity = localStorage.getItem('crm_table_density');
  if (savedDensity === 'compact' || savedDensity === 'comfortable') {
    applyTableDensity(savedDensity);
  }
  $('.crm-density-toggle button').on('click', function () {
    var idx = $(this).index();
    var mode = idx === 0 ? 'compact' : 'comfortable';
    localStorage.setItem('crm_table_density', mode);
    applyTableDensity(mode);
  });

  // Runtime cross-module continuity (sessionStorage, non-blocking).
  if (window.crmUi.initRuntimeBar) {
    window.crmUi.initRuntimeBar();
  }

  // Subtle page-enter affordance (reduced motion respected).
  var pageEnter = document.querySelector('.crm-page-enter');
  if (pageEnter && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    pageEnter.classList.add('is-active');
    setTimeout(function () {
      pageEnter.classList.remove('is-active');
    }, 280);
  }

  // Basic focus trap and close behavior for custom overlay modals.
  function trapFocus($modal) {
    var $focusables = $modal.find('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])').filter(':visible');
    if ($focusables.length === 0) return;
    var first = $focusables.get(0);
    var last = $focusables.get($focusables.length - 1);
    first.focus();

    $modal.off('keydown.crmFocusTrap').on('keydown.crmFocusTrap', function (e) {
      if (e.key === 'Escape') {
        $modal.css('display', 'none');
        return;
      }
      if (e.key !== 'Tab') return;
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    });
  }

  $('.crm-overlay-modal').on('click', function (e) {
    if (e.target === this) {
      $(this).css('display', 'none');
    }
  });

  // Observe modal display changes triggered by legacy scripts.
  $('.crm-overlay-modal').each(function () {
    var modal = this;
    var observer = new MutationObserver(function () {
      if (modal.style.display === 'flex') {
        trapFocus($(modal));
      } else {
        $(modal).off('keydown.crmFocusTrap');
      }
    });
    observer.observe(modal, { attributes: true, attributeFilter: ['style'] });
  });

  // Dismissible onboarding cards per page.
  $('[data-crm-onboarding-dismiss]').on('click', function () {
    var key = $(this).data('crm-onboarding-dismiss');
    if (!key) return;
    localStorage.setItem('crm_onboarding_hidden_' + key, '1');
    $('[data-crm-onboarding-card="' + key + '"]').slideUp(120);
  });
  $('[data-crm-onboarding-card]').each(function () {
    var key = $(this).data('crm-onboarding-card');
    if (localStorage.getItem('crm_onboarding_hidden_' + key) === '1') {
      $(this).hide();
    }
  });
  $('[data-crm-onboarding-reset]').on('click', function () {
    var key = $(this).data('crm-onboarding-reset');
    if (!key) return;
    localStorage.removeItem('crm_onboarding_hidden_' + key);
    $('[data-crm-onboarding-card="' + key + '"]').slideDown(120);
    window.crmUi.toast('Onboarding reactivado para este módulo.', 'success', 'Listo', { durationMs: 2200 });
  });
});

window.crmUi = window.crmUi || {};

window.crmUi.toast = function (message, type, title, options) {
  var stack = document.getElementById('crm-toast-stack');
  if (!stack) return;
  var opts = options || {};

  var toast = document.createElement('div');
  toast.className = 'crm-toast is-entering ' + (type || 'info');
  toast.setAttribute('role', 'status');
  toast.setAttribute('aria-live', 'polite');

  var safeTitle = title || (type === 'success' ? 'Éxito' : type === 'error' ? 'Error' : type === 'warning' ? 'Atención' : 'Información');
  var html = '<div class="crm-toast-title">' + safeTitle + '</div><div class="crm-toast-message">' + (message || '') + '</div>';
  if (opts.actionText && typeof opts.onAction === 'function') {
    html += '<div class="crm-toast-actions"><button type="button" class="crm-toast-action-btn" data-crm-toast-action="1">' + opts.actionText + '</button></div>';
  }
  toast.innerHTML = html;

  stack.appendChild(toast);
  requestAnimationFrame(function () {
    toast.classList.remove('is-entering');
  });

  var actionButton = toast.querySelector('[data-crm-toast-action="1"]');
  if (actionButton) {
    actionButton.addEventListener('click', function () {
      opts.onAction();
      toast.classList.add('is-leaving');
      setTimeout(function () { toast.remove(); }, 220);
    });
  }

  var duration = typeof opts.durationMs === 'number' ? opts.durationMs : 3600;
  if (duration <= 0) return;

  setTimeout(function () {
    toast.classList.add('is-leaving');
  }, duration);
  setTimeout(function () {
    toast.remove();
  }, duration + 220);
};

window.crmUi.setLoading = function (selector, isLoading) {
  var el = document.querySelector(selector);
  if (!el) return;
  if (isLoading) {
    el.setAttribute('aria-busy', 'true');
    el.classList.add('crm-skeleton');
  } else {
    el.removeAttribute('aria-busy');
    el.classList.remove('crm-skeleton');
  }
};

window.crmUi.moduleFromPath = function (path) {
  var p = (path || window.location.pathname || '/').toLowerCase();
  if (p === '/' || p === '/index') return { id: 'dashboard', label: 'Dashboard', href: '/' };
  if (p.indexOf('/leads') === 0) return { id: 'leads', label: 'Leads', href: '/Leads' };
  if (p.indexOf('/deals') === 0) return { id: 'deals', label: 'Pipeline', href: '/Deals' };
  if (p.indexOf('/customers') === 0) return { id: 'customers', label: 'Clientes', href: '/Customers' };
  if (p.indexOf('/workflows') === 0) return { id: 'workflows', label: 'Workflows', href: '/Workflows' };
  if (p.indexOf('/agents') === 0) return { id: 'agents', label: 'Agents', href: '/Agents' };
  if (p.indexOf('/audit') === 0) return { id: 'audit', label: 'Auditoría', href: '/Audit' };
  if (p.indexOf('/settings') === 0) return { id: 'settings', label: 'Configuración', href: '/Settings' };
  if (p.indexOf('/users') === 0) return { id: 'users', label: 'Usuarios', href: '/Users' };
  if (p.indexOf('/policies') === 0) return { id: 'policies', label: 'Políticas', href: '/Policies' };
  if (p.indexOf('/support') === 0) return { id: 'support', label: 'Soporte', href: '/Support' };
  if (p.indexOf('/tasks') === 0) return { id: 'tasks', label: 'Tareas', href: '/Tasks' };
  return { id: 'operation', label: 'Operación', href: p };
};

window.crmUi.initRuntimeBar = function () {
  var bar = document.getElementById('crm-runtime-bar');
  if (!bar) return;

  var current = window.crmUi.moduleFromPath(window.location.pathname);
  var lastRaw = sessionStorage.getItem('crm_runtime_last');
  var last = null;
  try {
    last = lastRaw ? JSON.parse(lastRaw) : null;
  } catch (e) {
    last = null;
  }

  if (last && last.id && last.id !== current.id && last.href) {
    var resume = document.getElementById('crm-runtime-resume');
    var resumeLabel = document.getElementById('crm-runtime-resume-label');
    if (resume && resumeLabel) {
      resume.href = last.href;
      resumeLabel.textContent = last.label || 'módulo anterior';
      resume.classList.remove('crm-hidden');
    }
  }

  sessionStorage.setItem('crm_runtime_last', JSON.stringify({
    id: current.id,
    label: current.label,
    href: window.location.pathname || current.href
  }));

  bar.querySelectorAll('[data-crm-module]').forEach(function (link) {
    if (link.getAttribute('data-crm-module') === current.id) {
      link.classList.add('active');
      link.setAttribute('aria-current', 'page');
    }
  });

  var ctxLabel = document.getElementById('crm-runtime-context-label');
  if (ctxLabel && ctxLabel.textContent.trim() === 'Operación' && current.label) {
    ctxLabel.textContent = current.label;
  }
};

window.crmUi.trackOperation = function (operationName, promise, options) {
  if (!promise || typeof promise.then !== 'function') return promise;
  var operation = operationName || 'Operación';
  var opts = options || {};
  window.crmUi.toast(operation + ' en progreso...', 'info', 'Procesando', { durationMs: 1600 });
  return promise.then(function (result) {
    window.crmUi.toast(opts.successMessage || (operation + ' completada correctamente.'), 'success', 'Completado');
    return result;
  }).catch(function (error) {
    var message = opts.errorMessage || (operation + ' falló. Revisa los datos e intenta nuevamente.');
    window.crmUi.toast(message, 'error', 'Error operativo', {
      actionText: typeof opts.onRetry === 'function' ? 'Reintentar' : null,
      onAction: opts.onRetry
    });
    throw error;
  });
};
