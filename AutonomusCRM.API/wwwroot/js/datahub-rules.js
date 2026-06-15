(function () {
  'use strict';

  function reindexRules(list) {
    var cards = list.querySelectorAll('.dh-rule-card');
    cards.forEach(function (card, index) {
      card.querySelectorAll('[name]').forEach(function (input) {
        var field = input.getAttribute('name');
        if (!field) return;
        var suffix = field.substring(field.indexOf('.') + 1);
        input.setAttribute('name', 'Rules[' + index + '].' + suffix);
      });
      var priority = card.querySelector('.dh-rule-priority');
      if (priority) priority.value = index + 1;
    });
  }

  function wireCard(card, list) {
    var removeBtn = card.querySelector('.dh-rule-remove');
    if (removeBtn) {
      removeBtn.addEventListener('click', function () {
        card.remove();
        reindexRules(list);
      });
    }
    card.addEventListener('dragstart', function (e) {
      card.classList.add('is-dragging');
      e.dataTransfer.effectAllowed = 'move';
    });
    card.addEventListener('dragend', function () {
      card.classList.remove('is-dragging');
      reindexRules(list);
    });
  }

  function initDragDrop(list) {
    list.addEventListener('dragover', function (e) {
      e.preventDefault();
      var dragging = list.querySelector('.is-dragging');
      var after = getDragAfterElement(list, e.clientY);
      if (!dragging) return;
      if (after == null) list.appendChild(dragging);
      else list.insertBefore(dragging, after);
    });
  }

  function getDragAfterElement(container, y) {
    var elements = [].slice.call(container.querySelectorAll('.dh-rule-card:not(.is-dragging)'));
    return elements.reduce(function (closest, child) {
      var box = child.getBoundingClientRect();
      var offset = y - box.top - box.height / 2;
      if (offset < 0 && offset > closest.offset) return { offset: offset, element: child };
      return closest;
    }, { offset: Number.NEGATIVE_INFINITY }).element;
  }

  function init() {
    var list = document.getElementById('dh-rule-list');
    var addBtn = document.getElementById('dh-add-rule');
    var template = document.getElementById('dh-rule-template');
    if (!list || !addBtn || !template) return;

    list.querySelectorAll('.dh-rule-card').forEach(function (card) { wireCard(card, list); });
    initDragDrop(list);

    addBtn.addEventListener('click', function () {
      var clone = template.content.firstElementChild.cloneNode(true);
      var index = list.querySelectorAll('.dh-rule-card').length;
      clone.querySelectorAll('[data-name]').forEach(function (el) {
        var name = el.getAttribute('data-name');
        el.setAttribute('name', 'Rules[' + index + '].' + name);
        el.removeAttribute('data-name');
      });
      var priority = clone.querySelector('.dh-rule-priority');
      if (priority) {
        priority.setAttribute('name', 'Rules[' + index + '].Priority');
        priority.value = index + 1;
      }
      var active = clone.querySelector('[name$=".IsActive"]');
      if (active) active.checked = true;
      wireCard(clone, list);
      list.appendChild(clone);
    });
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
