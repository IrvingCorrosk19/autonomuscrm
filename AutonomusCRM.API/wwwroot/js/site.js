// AutonomusCRM — AdminLTE helpers
function flowI18n(key, fallback) {
  var strings = window.__flowI18n && window.__flowI18n.strings;
  return (strings && strings[key]) || fallback || key;
}

$(function () {
  if (typeof $.fn.tooltip !== 'undefined') {
    $('[data-toggle="tooltip"]').tooltip();
  }

  // Wrap raw tables for horizontal fallback.
  $('table.table, table.flow-datatable, table.flow-table-minimal').each(function () {
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

  function trapFocus($modal) {
    var $focusables = $modal.find('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])').filter(':visible');
    if ($focusables.length === 0) return;
    var first = $focusables.get(0);
    var last = $focusables.get($focusables.length - 1);
    first.focus();

    $modal.off('keydown.crmFocusTrap').on('keydown.crmFocusTrap', function (e) {
      if (e.key === 'Escape') {
        window.crmModal.close($modal[0]);
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

  // Enterprise modal system — focus trap, ESC, scroll lock, aria.
  window.crmModal = window.crmModal || {
    open: function (id) {
      var modal = typeof id === 'string' ? document.getElementById(id) : id;
      if (!modal) return;
      modal.style.display = 'flex';
      modal.setAttribute('aria-hidden', 'false');
      modal.classList.add('is-open');
      document.body.classList.add('crm-modal-open');
      trapFocus($(modal));
    },
    close: function (id) {
      var modal = typeof id === 'string' ? document.getElementById(id) : id;
      if (!modal) return;
      modal.style.display = 'none';
      modal.setAttribute('aria-hidden', 'true');
      modal.classList.remove('is-open');
      $(modal).off('keydown.crmFocusTrap');
      if (!document.querySelector('.crm-overlay-modal.is-open, .crm-overlay-modal[aria-hidden="false"]')) {
        document.body.classList.remove('crm-modal-open');
      }
    }
  };

  function bindModal(modal) {
    if (!modal || modal.dataset.crmModalBound === '1') return;
    modal.dataset.crmModalBound = '1';
    if (!modal.hasAttribute('aria-hidden')) modal.setAttribute('aria-hidden', 'true');
    if (!modal.hasAttribute('role')) modal.setAttribute('role', 'dialog');
    if (!modal.hasAttribute('aria-modal')) modal.setAttribute('aria-modal', 'true');

    modal.addEventListener('click', function (e) {
      if (e.target === modal) window.crmModal.close(modal);
    });

    var observer = new MutationObserver(function () {
      if (modal.style.display === 'flex' || modal.classList.contains('is-open')) {
        modal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('crm-modal-open');
        trapFocus($(modal));
      } else {
        $(modal).off('keydown.crmFocusTrap');
      }
    });
    observer.observe(modal, { attributes: true, attributeFilter: ['style', 'class'] });
  }

  document.querySelectorAll('.crm-overlay-modal').forEach(bindModal);

  $(document).on('click', '[data-crm-modal-open]', function (e) {
    e.preventDefault();
    var id = $(this).data('crm-modal-open');
    if (id) window.crmModal.open(id);
  });

  $(document).on('click', '[data-crm-modal-close]', function (e) {
    e.preventDefault();
    var modal = $(this).closest('.crm-overlay-modal')[0];
    if (modal) window.crmModal.close(modal);
  });

  // Legacy jQuery handlers (kept for compatibility).
  $('.crm-overlay-modal').on('click', function (e) {
    if (e.target === this) {
      window.crmModal.close(this);
    }
  });

  $('.crm-overlay-modal').each(function () {
    bindModal(this);
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
    window.crmUi.toast(flowI18n('toastOnboardingReset', 'Onboarding re-enabled for this module.'), 'success', flowI18n('toastReady', 'Ready'), { durationMs: 2200 });
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

  var safeTitle = title || (type === 'success' ? flowI18n('toastSuccess', 'Success') : type === 'error' ? flowI18n('toastError', 'Error') : type === 'warning' ? flowI18n('toastWarning', 'Warning') : flowI18n('toastInfo', 'Information'));
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
  if (p === '/' || p === '/index') return { id: 'dashboard', label: flowI18n('moduleDashboard', 'Dashboard'), href: '/' };
  if (p.indexOf('/leads') === 0) return { id: 'leads', label: flowI18n('typeLead', 'Leads'), href: '/Leads' };
  if (p.indexOf('/deals') === 0) return { id: 'deals', label: flowI18n('typeDeal', 'Pipeline'), href: '/Deals' };
  if (p.indexOf('/customers') === 0) return { id: 'customers', label: flowI18n('typeCustomer', 'Customers'), href: '/Customers' };
  if (p.indexOf('/workflows') === 0) return { id: 'workflows', label: flowI18n('typeRoute', 'Workflows'), href: '/Workflows' };
  if (p.indexOf('/agents') === 0) return { id: 'agents', label: flowI18n('typeCommand', 'Agents'), href: '/Agents' };
  if (p.indexOf('/audit') === 0) return { id: 'audit', label: flowI18n('moduleOperation', 'Audit'), href: '/Audit' };
  if (p.indexOf('/settings') === 0) return { id: 'settings', label: flowI18n('moduleOperation', 'Settings'), href: '/Settings' };
  if (p.indexOf('/users') === 0) return { id: 'users', label: flowI18n('typeCustomer', 'Users'), href: '/Users' };
  if (p.indexOf('/policies') === 0) return { id: 'policies', label: flowI18n('moduleOperation', 'Policies'), href: '/Policies' };
  if (p.indexOf('/support') === 0) return { id: 'support', label: flowI18n('moduleOperation', 'Support'), href: '/Support' };
  if (p.indexOf('/tasks') === 0) return { id: 'tasks', label: flowI18n('moduleOperation', 'Tasks'), href: '/Tasks' };
  return { id: 'operation', label: flowI18n('moduleOperationDefault', 'Operation'), href: p };
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
      resumeLabel.textContent = last.label || flowI18n('modulePrevious', 'previous module');
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
  if (ctxLabel && ctxLabel.textContent.trim() === flowI18n('moduleOperationDefault', 'Operation') && current.label) {
    ctxLabel.textContent = current.label;
  }
};

window.crmUi.trackOperation = function (operationName, promise, options) {
  if (!promise || typeof promise.then !== 'function') return promise;
  var operation = operationName || flowI18n('moduleOperationDefault', 'Operation');
  var opts = options || {};
  window.crmUi.toast(operation + ' ' + flowI18n('operationInProgress', 'in progress...'), 'info', flowI18n('operationProcessing', 'Processing'), { durationMs: 1600 });
  return promise.then(function (result) {
    window.crmUi.toast(opts.successMessage || (operation + ' ' + flowI18n('operationCompleted', 'completed successfully.')), 'success', flowI18n('operationCompletedTitle', 'Completed'));
    return result;
  }).catch(function (error) {
    var message = opts.errorMessage || (operation + ' ' + flowI18n('operationFailed', 'failed. Check the data and try again.'));
    window.crmUi.toast(message, 'error', flowI18n('operationErrorTitle', 'Operation error'), {
      actionText: typeof opts.onRetry === 'function' ? flowI18n('operationRetry', 'Retry') : null,
      onAction: opts.onRetry
    });
    throw error;
  });
};

// AutonomusCRM University — progress, badges, ranking (localStorage)
window.flowUniversity = (function () {
  var STORAGE_KEY = 'autonomus-university-progress';

  function load() {
    try {
      var raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return defaultState();
      return Object.assign(defaultState(), JSON.parse(raw));
    } catch (e) {
      return defaultState();
    }
  }

  function defaultState() {
    return {
      points: 0,
      completedLessons: [],
      certifications: {},
      badges: [],
      visitedLessons: []
    };
  }

  function save(state) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }

  function getUserLabel() {
    var el = document.getElementById('flow-topbar-user-btn');
    return (el && el.textContent.trim()) || 'You';
  }

  return {
    load: load,
    completeLesson: function (lessonId, points, pathId) {
      var state = load();
      if (state.completedLessons.indexOf(lessonId) === -1) {
        state.completedLessons.push(lessonId);
        state.points += points || 0;
      }
      save(state);
      this.refreshUI();
    },
    markLessonVisited: function (lessonId) {
      var state = load();
      if (state.visitedLessons.indexOf(lessonId) === -1) {
        state.visitedLessons.push(lessonId);
        save(state);
      }
    },
    completeCertification: function (certId, score) {
      var state = load();
      state.certifications[certId] = { score: score, at: new Date().toISOString() };
      state.points += 500;
      if (state.badges.indexOf('cert-' + certId) === -1) {
        state.badges.push('cert-' + certId);
      }
      save(state);
      this.refreshUI();
    },
    calcProgress: function (lessonIds) {
      var state = load();
      if (!lessonIds || !lessonIds.length) return 0;
      var done = lessonIds.filter(function (id) {
        return state.completedLessons.indexOf(id) !== -1;
      }).length;
      return Math.round((done / lessonIds.length) * 100);
    },
    initDashboard: function (config) {
      var self = this;
      var state = load();
      var pointsEl = document.getElementById('uni-points');
      var progressEl = document.getElementById('uni-progress');
      var badgesEl = document.getElementById('uni-badges');
      if (pointsEl) pointsEl.textContent = state.points;
      if (progressEl) {
        progressEl.textContent = self.calcProgress(config.lessonIds) + '%';
      }
      if (badgesEl) badgesEl.textContent = state.badges.length;

      (config.paths || []).forEach(function (path) {
        var ids = path.units.map(function (u) { return u.id; });
        var pct = self.calcProgress(ids);
        var fill = document.querySelector('[data-path-fill="' + path.id + '"]');
        var pctEl = document.querySelector('[data-path-pct="' + path.id + '"]');
        if (fill) fill.style.width = pct + '%';
        if (pctEl) pctEl.textContent = pct + '%';
      });

      document.querySelectorAll('[data-lesson-id]').forEach(function (link) {
        if (state.completedLessons.indexOf(link.dataset.lessonId) !== -1) {
          link.classList.add('is-complete');
        }
      });

      var badgeRow = document.getElementById('flow-university-badges');
      if (badgeRow && config.badges) {
        badgeRow.innerHTML = '';
        config.badges.forEach(function (b) {
          var earned = state.badges.indexOf(b.id) !== -1 ||
            (config.paths && config.paths.some(function (p) {
              return p.badge === b.id && self.calcProgress(p.units.map(function (u) { return u.id; })) === 100;
            }));
          var div = document.createElement('div');
          div.className = 'flow-university-badge' + (earned ? ' is-earned' : '');
          div.innerHTML = '<span class="flow-university-badge-icon">🏆</span><span class="flow-university-badge-title">' + b.title + '</span>';
          badgeRow.appendChild(div);
        });
      }

      var tbody = document.querySelector('#flow-university-leaderboard tbody');
      if (tbody) {
        var rows = [
          { name: getUserLabel(), points: state.points, badges: state.badges.length },
          { name: 'sales1@autonomuscrm.local', points: 2400, badges: 4 },
          { name: 'manager@autonomuscrm.local', points: 3100, badges: 5 },
          { name: 'support@autonomuscrm.local', points: 1800, badges: 3 }
        ].sort(function (a, b) { return b.points - a.points; });
        tbody.innerHTML = rows.map(function (r, i) {
          return '<tr><td>' + (i + 1) + '</td><td>' + r.name + '</td><td>' + r.points + '</td><td>' + r.badges + '</td></tr>';
        }).join('');
      }
    },
    refreshUI: function () {
      /* Re-init if dashboard config stored */
    }
  };
})();
