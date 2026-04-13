// ── _Layout.cshtml — global confirm modal + tooltips ──────────────────────
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        new bootstrap.Tooltip(el);
    });

    document.querySelectorAll('[data-confirm]').forEach(function (el) {
        el.addEventListener('click', function (e) {
            e.preventDefault();
            var message = el.getAttribute('data-confirm');
            var title = el.getAttribute('data-confirm-title') || 'Potvrdiť akciu';
            document.getElementById('confirmModalTitle').textContent = title;
            document.getElementById('confirmModalBody').textContent = message;
            var modal = new bootstrap.Modal(document.getElementById('confirmModal'));
            var actionBtn = document.getElementById('confirmModalAction');
            var newBtn = actionBtn.cloneNode(true);
            actionBtn.parentNode.replaceChild(newBtn, actionBtn);
            newBtn.addEventListener('click', function () {
                modal.hide();
                if (el.tagName === 'A') {
                    window.location.href = el.href;
                } else if (el.closest('form')) {
                    el.closest('form').submit();
                } else if (el.dataset.url) {
                    fetch(el.dataset.url, { method: 'POST', headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' } })
                        .then(r => r.json())
                        .then(d => { if (d.success) location.reload(); else alert(d.message); });
                }
            });
            modal.show();
        });
    });
});

// ── Groups/Index.cshtml ───────────────────────────────────────────────────
(function () {
    if (!document.querySelector('.btn-delete[data-id]') &&
        !document.querySelector('.btn-archive') &&
        !document.querySelector('.btn-unarchive')) return;

    function confirmAction(title, body, onConfirm) {
        document.getElementById('confirmModalTitle').textContent = title;
        document.getElementById('confirmModalBody').textContent = body;
        var modalEl = document.getElementById('confirmModal');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        var actionBtn = document.getElementById('confirmModalAction');
        var newBtn = actionBtn.cloneNode(true);
        actionBtn.parentNode.replaceChild(newBtn, actionBtn);
        newBtn.addEventListener('click', function () {
            modal.hide();
            onConfirm();
        });
        modal.show();
    }

    document.querySelectorAll('.btn-delete').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var id = this.dataset.id;
            var name = this.dataset.name;
            var students = parseInt(this.dataset.students) || 0;
            var msg = 'Natárvalo vymazať "' + name + '"? Túto akciu nemožno vrátiť.';
            if (students > 0)
                msg += '\n\n⚠ Týmto sa tiež vymaže ' + students + ' študent' + (students > 1 ? 'ov' : '') + ' a všetky ich zadania, hodnotenia, záznamy dochádzky a história žrebovania.';
            confirmAction('Vymazať skupinu', msg, function () {
                fetch('/Groups/Delete/' + id, { method: 'POST' })
                    .then(function (r) { return r.json(); })
                    .then(function (d) {
                        if (d.success) location.reload();
                        else alert(d.message);
                    });
            });
        });
    });

    document.querySelectorAll('.btn-unarchive').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var id = this.dataset.id;
            var name = this.dataset.name;
            confirmAction('Zrušiť archív skupiny', 'Obnoviť "' + name + '" z archívu?', function () {
                fetch('/Groups/Unarchive/' + id, { method: 'POST' })
                    .then(function (r) { return r.json(); })
                    .then(function (d) {
                        if (d.success) location.reload();
                        else alert(d.message);
                    });
            });
        });
    });

    document.querySelectorAll('.btn-archive').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var id = this.dataset.id;
            var name = this.dataset.name;
            confirmAction('Archivovať skupinu', 'Naozaj chcete archivovať "' + name + '"?', function () {
                fetch('/Groups/Archive/' + id, { method: 'POST' })
                    .then(function (r) { return r.json(); })
                    .then(function (d) {
                        if (d.success) location.reload();
                        else alert(d.message);
                    });
            });
        });
    });
})();

// ── Activities/Index.cshtml ───────────────────────────────────────────────
(function () {
    var selectAll = document.getElementById('selectAll');
    if (!selectAll || !document.getElementById('bulkAssignBtn')) return;
    var countLabel    = document.getElementById('selectionCount');
    var bulkAssignBtn = document.getElementById('bulkAssignBtn');
    var bulkDrawBtn   = document.getElementById('bulkDrawBtn');
    var bulkDeleteBtn = document.getElementById('bulkDeleteBtn');

    function getChecked() {
        return Array.from(document.querySelectorAll('.row-check:checked'));
    }

    function updateToolbar() {
        var checked = getChecked();
        var count = checked.length;
        var hasSelection = count > 0;
        countLabel.textContent = count + ' vybraných';
        countLabel.classList.toggle('d-none', !hasSelection);
        bulkAssignBtn.disabled = !hasSelection;
        bulkDrawBtn.disabled   = !hasSelection;
        bulkDeleteBtn.disabled = !hasSelection;
        var total = document.querySelectorAll('.row-check').length;
        selectAll.checked = count === total && total > 0;
        selectAll.indeterminate = count > 0 && count < total;
    }

    selectAll.addEventListener('change', function () {
        document.querySelectorAll('.row-check').forEach(function (cb) {
            cb.checked = selectAll.checked;
        });
        updateToolbar();
    });

    document.querySelectorAll('.row-check').forEach(function (cb) {
        cb.addEventListener('change', updateToolbar);
    });

    bulkAssignBtn.addEventListener('click', function () {
        var checked = getChecked();
        if (checked.length === 0) return;
        var names = checked.map(function (cb) { return cb.dataset.name; }).join(', ');
        var msg = checked.length === 1
            ? 'Auto-priradiť študentov k "' + names + '"?'
            : 'Rovnomerne rozložiť študentov medzi ' + checked.length + ' aktivít: ' + names + '?';
        var modalEl = document.getElementById('confirmModal');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        document.getElementById('confirmModalTitle').textContent = 'Auto-priradenie';
        document.getElementById('confirmModalBody').textContent = msg + ' Týmto sa prepíšu existujúce priradenia.';
        var actionBtn = document.getElementById('confirmModalAction');
        var newBtn = actionBtn.cloneNode(true);
        actionBtn.parentNode.replaceChild(newBtn, actionBtn);
        newBtn.addEventListener('click', function () {
            modal.hide();
            var ids = checked.map(function (cb) { return parseInt(cb.value); });
            bulkAssignBtn.disabled = true;
            fetch('/Activities/BulkAssign', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(ids)
            }).then(function (r) {
                if (!r.ok) {
                    r.text().then(function (t) { alert('Auto-priradenie zlyhalo: ' + t); });
                    bulkAssignBtn.disabled = false;
                } else {
                    location.reload();
                }
            }).catch(function (err) {
                alert('Chyba auto-priradenia: ' + err);
                bulkAssignBtn.disabled = false;
            });
        });
        modal.show();
    });

    document.querySelectorAll('.btn-delete-activity').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var id = this.dataset.id;
            var name = this.dataset.name;
            var modalEl = document.getElementById('confirmModal');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            document.getElementById('confirmModalTitle').textContent = 'Vymazať aktivitu';
            document.getElementById('confirmModalBody').textContent =
                'Natárvalo vymazať "' + name + '" a všetky jej úlohy, priradenia a hodnotenia? Túto akciu nemožno vrátiť.';
            var actionBtn = document.getElementById('confirmModalAction');
            var newBtn = actionBtn.cloneNode(true);
            actionBtn.parentNode.replaceChild(newBtn, actionBtn);
            newBtn.addEventListener('click', function () {
                modal.hide();
                fetch('/Activities/Delete/' + id, { method: 'POST' })
                    .then(function (r) { return r.json(); })
                    .then(function (d) {
                        if (d.success) location.reload();
                        else alert(d.message);
                    })
                    .catch(function () { alert('Vymazanie zlyhalo.'); });
            });
            modal.show();
        });
    });

    document.querySelectorAll('.btn-duplicate-activity').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var id = this.dataset.id;
            var name = this.dataset.name;
            var modalEl = document.getElementById('confirmModal');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            document.getElementById('confirmModalTitle').textContent = 'Duplikovať aktivitu';
            document.getElementById('confirmModalBody').textContent =
                'Vytvoriť kópiu "' + name + '" so všetkými úlohami, prezentáciami a atribútmi?';
            var actionBtn = document.getElementById('confirmModalAction');
            var newBtn = actionBtn.cloneNode(true);
            actionBtn.parentNode.replaceChild(newBtn, actionBtn);
            newBtn.addEventListener('click', function () {
                modal.hide();
                fetch('/Activities/Duplicate/' + id, { method: 'POST' })
                    .then(function (r) { return r.json(); })
                    .then(function (d) {
                        if (d.success) location.reload();
                        else alert(d.message || 'Duplikovanie zlyhalo.');
                    })
                    .catch(function () { alert('Duplikovanie zlyhalo.'); });
            });
            modal.show();
        });
    });

    bulkDeleteBtn.addEventListener('click', function () {
        var checked = getChecked();
        if (checked.length === 0) return;
        var names = checked.map(function (cb) { return cb.dataset.name; }).join(', ');
        var msg = checked.length === 1
            ? 'Natárvalo vymazať "' + names + '" a všetky jej úlohy, priradenia a hodnotenia?'
            : 'Natárvalo vymazať ' + checked.length + ' aktivít: ' + names + '?\nVšetky úlohy, priradenia a hodnotenia budú odstránené.';
        var modalEl = document.getElementById('confirmModal');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        document.getElementById('confirmModalTitle').textContent = 'Vymazať aktivity';
        document.getElementById('confirmModalBody').textContent = msg + ' Túto akciu nemožno vrátiť.';
        var actionBtn = document.getElementById('confirmModalAction');
        var newBtn = actionBtn.cloneNode(true);
        actionBtn.parentNode.replaceChild(newBtn, actionBtn);
        newBtn.addEventListener('click', function () {
            modal.hide();
            var ids = checked.map(function (cb) { return parseInt(cb.value); });
            bulkDeleteBtn.disabled = true;
            fetch('/Activities/BulkDelete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(ids)
            }).then(function (r) { return r.json(); })
              .then(function (d) {
                  if (d.success) location.reload();
                  else alert(d.message || 'Vymazanie zlyhalo.');
              })
              .catch(function () { alert('Vymazanie zlyhalo.'); });
        });
        modal.show();
    });

    bulkDrawBtn.addEventListener('click', function () {
        var checked = getChecked();
        if (checked.length === 0) return;
        var ids = checked.map(function (cb) { return cb.value; }).join(',');
        window.location.href = '/Draw?activityIds=' + ids;
    });
})();

// ── Activities/Details.cshtml ─────────────────────────────────────────────
(function () {
    var form = document.getElementById('activityAssignForm');
    if (!form) return;
    var activityId = form.dataset.activityId;
    var token = form.querySelector('input[name="__RequestVerificationToken"]').value;

    form.querySelectorAll('input[type="checkbox"]').forEach(function (cb) {
        cb.addEventListener('change', function () {
            var checked = Array.from(form.querySelectorAll('input[type="checkbox"]:checked'));
            var body = '__RequestVerificationToken=' + encodeURIComponent(token)
                + '&activityId=' + activityId;
            checked.forEach(function (c) { body += '&studentIds=' + encodeURIComponent(c.value); });
            fetch('/Activities/SetActivityAssignments', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: body
            }).then(function (r) {
                if (r.ok) {
                    var badgeArea = document.getElementById('activity-badge-area');
                    if (checked.length === 0) {
                        badgeArea.innerHTML = '<p class="text-muted small mb-0">Žiadni študenti zatiaľ neboli priradení.</p>';
                    } else {
                        var html = '<div class="d-flex flex-wrap gap-1">';
                        checked.forEach(function (c) {
                            html += '<span class="badge bg-info text-dark">' + c.dataset.name + '</span>';
                        });
                        html += '</div>';
                        badgeArea.innerHTML = html;
                    }
                } else { alert('Nepodarilo sa uložiť priradenie.'); }
            });
        });
    });
})();

(function () {
    var addTaskBtn = document.getElementById('addTaskBtn');
    if (!addTaskBtn) return;
    var activityId = addTaskBtn.dataset.activityId;

    addTaskBtn.addEventListener('click', function () {
        var title = document.getElementById('taskTitle').value.trim();
        if (!title) return;
        fetch('/Tasks/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'title=' + encodeURIComponent(title) + '&activityId=' + activityId + '&isPresentation=false'
        })
        .then(function (r) { return r.json(); })
        .then(function (d) { if (d.success) location.reload(); else alert(d.message); });
    });

    document.getElementById('taskTitle').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); addTaskBtn.click(); }
    });
})();

document.querySelectorAll('.btn-delete-task').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var taskId = this.dataset.id;
        var modalEl = document.getElementById('confirmModal');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        document.getElementById('confirmModalTitle').textContent = 'Vymazať položku';
        document.getElementById('confirmModalBody').textContent = 'Vymazať túto položku? Túto akciu nemožno vrátiť.';
        var actionBtn = document.getElementById('confirmModalAction');
        var newBtn = actionBtn.cloneNode(true);
        actionBtn.parentNode.replaceChild(newBtn, actionBtn);
        newBtn.addEventListener('click', function () {
            modal.hide();
            fetch('/Tasks/Delete/' + taskId, { method: 'POST' })
                .then(function (r) { return r.json(); })
                .then(function (d) { if (d.success) location.reload(); else alert(d.message); });
        });
        modal.show();
    });
});

(function () {
    var addPresBtn = document.getElementById('addPresBtn');
    if (!addPresBtn) return;
    var activityId = addPresBtn.dataset.activityId;

    addPresBtn.addEventListener('click', function () {
        var title = document.getElementById('presTitle').value.trim();
        if (!title) return;
        var date = document.getElementById('presDate').value;
        var body = 'title=' + encodeURIComponent(title) + '&activityId=' + activityId + '&isPresentation=true';
        if (date) body += '&presentationDate=' + encodeURIComponent(date);
        fetch('/Tasks/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: body
        })
        .then(function (r) { return r.json(); })
        .then(function (d) { if (d.success) location.reload(); else alert(d.message); });
    });

    document.getElementById('presTitle').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); addPresBtn.click(); }
    });
})();

document.querySelectorAll('.pres-title-input').forEach(function (input) {
    input.addEventListener('blur', function () {
        var title = this.value.trim();
        if (!title) { this.value = this.defaultValue; return; }
        fetch('/Tasks/SetTitle', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'id=' + this.dataset.id + '&title=' + encodeURIComponent(title)
        });
    });
    input.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); this.blur(); }
    });
});

document.querySelectorAll('.pres-date-input').forEach(function (input) {
    input.addEventListener('change', function () {
        fetch('/Tasks/SetDate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'id=' + this.dataset.id + '&presentationDate=' + encodeURIComponent(this.value)
        });
    });
});

(function () {
    var addBtn = document.getElementById('addAttrBtn');
    if (!addBtn) return;
    var activityId = addBtn.dataset.activityId;
    var nameInput = document.getElementById('newAttrName');

    addBtn.addEventListener('click', function () {
        var name = nameInput.value.trim();
        if (!name) return;
        fetch('/ActivityAttributes/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'activityId=' + activityId + '&name=' + encodeURIComponent(name)
        }).then(function (r) { return r.json(); })
          .then(function (d) { if (d.success) location.reload(); else alert(d.message); });
    });

    nameInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); addBtn.click(); }
    });
})();

document.querySelectorAll('.other-value-pick').forEach(function (link) {
    link.addEventListener('click', function (e) {
        e.preventDefault();
        var studentId   = this.dataset.studentId;
        var attrId      = this.dataset.attrId;
        var optionId    = this.dataset.optionId || '';
        var optionName  = this.dataset.optionName || '—';
        var optionColor = this.dataset.optionColor || '';
        fetch('/ActivityAttributes/SetValue', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'studentId=' + studentId + '&attributeId=' + attrId
                + (optionId ? '&optionId=' + optionId : '')
        }).then(function (r) { return r.json(); })
          .then(function (d) {
              if (!d.success) return;
              var cell = document.querySelector('tr[data-student-id="' + studentId + '"] td[data-attr-id="' + attrId + '"]');
              if (!cell) return;
              var btn = cell.querySelector('.dropdown-toggle');
              if (!btn) return;
              btn.textContent = optionName;
              btn.className = 'btn btn-sm dropdown-toggle '
                  + (optionId ? 'btn-' + optionColor : 'btn-outline-secondary');
          });
    });
});

(function () {
    var modal = document.getElementById('manageStatesModal');
    if (!modal) return;
    var bsModal = new bootstrap.Modal(modal);
    var currentAttrId = null;
    var modalAttrName  = document.getElementById('modalAttrName');
    var modalSaveBtn   = document.getElementById('modalSaveBtn');
    var modalDeleteBtn = document.getElementById('modalDeleteAttrBtn');
    var statesList     = document.getElementById('statesList');
    var newStateName   = document.getElementById('newStateName');
    var newStateColor  = document.getElementById('newStateColor');
    var addStateBtn    = document.getElementById('addStateBtn');

    function colorLabel(c) {
        var map = { primary: 'Modrá', success: 'Zelená', danger: 'Červená', warning: 'Žltá', info: 'Tyrkysová', secondary: 'Šedá', dark: 'Tmavá' };
        return map[c] || c;
    }

    function escHtml(s) {
        return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function colorOptions(selected) {
        var colors = ['secondary', 'primary', 'success', 'danger', 'warning', 'info', 'dark'];
        return colors.map(function (c) {
            return '<option value="' + c + '"' + (c === selected ? ' selected' : '') + '>' + colorLabel(c) + '</option>';
        }).join('');
    }

    function renderStates(options) {
        if (!options.length) {
            statesList.innerHTML = '<p class="text-muted small">Žiadne stavy.</p>';
            return;
        }
        statesList.innerHTML = options.map(function (o) {
            return '<div class="d-flex align-items-center gap-2 mb-1" id="state-row-' + o.id + '">'
                + '<span class="badge bg-' + o.color + ' flex-shrink-0">' + escHtml(o.name) + '</span>'
                + '<input type="text" class="form-control form-control-sm state-name-input" style="max-width:130px" value="' + escHtml(o.name) + '" data-id="' + o.id + '" maxlength="200" />'
                + '<select class="form-select form-select-sm state-color-select" style="max-width:110px" data-id="' + o.id + '">'
                + colorOptions(o.color)
                + '</select>'
                + '<button type="button" class="btn btn-sm btn-outline-danger state-delete-btn" data-id="' + o.id + '"><i class="bi bi-trash"></i></button>'
                + '</div>';
        }).join('');

        statesList.querySelectorAll('.state-color-select').forEach(function (sel) {
            sel.addEventListener('change', function () {
                var row = document.getElementById('state-row-' + this.dataset.id);
                var badge = row.querySelector('.badge');
                badge.className = 'badge bg-' + this.value + ' flex-shrink-0';
            });
        });

        statesList.querySelectorAll('.state-name-input').forEach(function (input) {
            input.addEventListener('input', function () {
                var row = document.getElementById('state-row-' + this.dataset.id);
                var badge = row.querySelector('.badge');
                badge.textContent = this.value || '\u200b';
            });
        });

        statesList.querySelectorAll('.state-delete-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var stateId = this.dataset.id;
                var cModalEl = document.getElementById('confirmModal');
                var cModal = bootstrap.Modal.getOrCreateInstance(cModalEl);
                document.getElementById('confirmModalTitle').textContent = 'Vymazať stav';
                document.getElementById('confirmModalBody').textContent = 'Vymazať tento stav? Túto akciu nemožno vrátiť.';
                var cActionBtn = document.getElementById('confirmModalAction');
                var cNewBtn = cActionBtn.cloneNode(true);
                cActionBtn.parentNode.replaceChild(cNewBtn, cActionBtn);
                cNewBtn.addEventListener('click', function () {
                    cModal.hide();
                    fetch('/ActivityAttributes/DeleteOption', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: 'id=' + stateId
                    }).then(function () { location.reload(); });
                });
                cModal.show();
            });
        });
    }

    document.querySelectorAll('.btn-manage-attr').forEach(function (btn) {
        btn.addEventListener('click', function () {
            currentAttrId = this.dataset.attrId;
            modalAttrName.value = this.dataset.attrName;
            newStateName.value = '';
            newStateColor.value = 'secondary';
            var picks = document.querySelectorAll('.other-value-pick[data-attr-id="' + currentAttrId + '"][data-option-id]:not([data-option-id=""])');
            var seen = new Map();
            Array.from(picks).forEach(function (a) {
                if (!seen.has(a.dataset.optionId)) {
                    seen.set(a.dataset.optionId, { id: a.dataset.optionId, name: a.dataset.optionName, color: a.dataset.optionColor });
                }
            });
            renderStates(Array.from(seen.values()));
            bsModal.show();
        });
    });

    modalSaveBtn.addEventListener('click', function () {
        if (!currentAttrId) return;
        var promises = [];
        var newName = modalAttrName.value.trim();
        if (newName) {
            promises.push(fetch('/ActivityAttributes/Rename', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'id=' + currentAttrId + '&name=' + encodeURIComponent(newName)
            }));
        }
        statesList.querySelectorAll('[id^="state-row-"]').forEach(function (row) {
            var id    = row.id.replace('state-row-', '');
            var name  = row.querySelector('.state-name-input').value.trim();
            var color = row.querySelector('.state-color-select').value;
            if (!name) return;
            promises.push(fetch('/ActivityAttributes/EditOption', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'id=' + id + '&name=' + encodeURIComponent(name) + '&color=' + color
            }));
        });
        Promise.all(promises).then(function () { location.reload(); });
    });

    modalDeleteBtn.addEventListener('click', function () {
        if (!currentAttrId) return;
        var cModalEl = document.getElementById('confirmModal');
        var cModal = bootstrap.Modal.getOrCreateInstance(cModalEl);
        document.getElementById('confirmModalTitle').textContent = 'Vymazať atribút';
        document.getElementById('confirmModalBody').textContent = 'Vymazať tento atribút a všetky hodnoty študentov? Túto akciu nemožno vrátiť.';
        var cActionBtn = document.getElementById('confirmModalAction');
        var cNewBtn = cActionBtn.cloneNode(true);
        cActionBtn.parentNode.replaceChild(cNewBtn, cActionBtn);
        cNewBtn.addEventListener('click', function () {
            cModal.hide();
            fetch('/ActivityAttributes/Delete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'id=' + currentAttrId
            }).then(function () { location.reload(); });
        });
        cModal.show();
    });

    addStateBtn.addEventListener('click', function () {
        var name  = newStateName.value.trim();
        var color = newStateColor.value;
        if (!name || !currentAttrId) return;
        fetch('/ActivityAttributes/AddOption', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'attributeId=' + currentAttrId + '&name=' + encodeURIComponent(name) + '&color=' + color
        }).then(function () { location.reload(); });
    });

    newStateName.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); addStateBtn.click(); }
    });
})();

(function () {
    function getSibling(dropdown) {
        var taskId    = dropdown.dataset.taskId;
        var otherRole = dropdown.dataset.role === '0' ? '1' : '0';
        return document.querySelector(
            '.pres-role-dropdown[data-task-id="' + taskId + '"][data-role="' + otherRole + '"]'
        );
    }

    function syncDisabled(sourceDropdown) {
        var sibling = getSibling(sourceDropdown);
        if (!sibling) return;
        var taken = new Set(
            Array.from(sourceDropdown.querySelectorAll('.pres-student-cb:checked'))
                .map(function (cb) { return cb.value; })
        );
        sibling.querySelectorAll('.pres-student-cb').forEach(function (cb) {
            cb.disabled = taken.has(cb.value);
            cb.closest('label').classList.toggle('opacity-50', cb.disabled);
        });
    }

    document.querySelectorAll('.pres-role-dropdown').forEach(function (d) { syncDisabled(d); });

    document.querySelectorAll('.pres-role-dropdown').forEach(function (dropdown) {
        var taskId     = dropdown.dataset.taskId;
        var role       = dropdown.dataset.role;
        var badgeId    = dropdown.dataset.badgeArea;
        var badgeClass = role === '0' ? 'bg-primary' : 'bg-warning text-dark';
        dropdown.querySelectorAll('.pres-student-cb').forEach(function (cb) {
            cb.addEventListener('change', function () {
                syncDisabled(dropdown);
                var checked = Array.from(dropdown.querySelectorAll('.pres-student-cb:checked'));
                var body = 'taskId=' + taskId + '&role=' + role;
                checked.forEach(function (c) { body += '&studentIds=' + c.value; });
                fetch('/Tasks/SetPresentationStudentsByRole', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: body
                }).then(function (r) {
                    if (r.ok) {
                        var badgeArea = document.getElementById(badgeId);
                        if (checked.length === 0) {
                            badgeArea.innerHTML = '<span class="text-muted small">Žiadni</span>';
                        } else {
                            badgeArea.innerHTML = checked.map(function (c) {
                                return '<span class="badge ' + badgeClass + '">' + c.dataset.name + '</span>';
                            }).join('');
                        }
                    } else { alert('Nepodarilo sa uložiť priradenie študentov.'); }
                });
            });
        });
    });
})();

document.querySelectorAll('.btn-draw-pres').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var role = this.dataset.role || 'both';
        window.location.href = '/Draw?presentationIds=' + this.dataset.id + '&presRole=' + role;
    });
});

(function () {
    var presSelectAll = document.getElementById('presSelectAll');
    if (!presSelectAll) return;
    var countLabel  = document.getElementById('presSelectionCount');
    var presDrawBtn = document.getElementById('presDrawBtn');

    function getPresChecked() {
        return Array.from(document.querySelectorAll('.pres-row-check:checked'));
    }

    function updatePresToolbar() {
        var checked = getPresChecked();
        var count = checked.length;
        countLabel.textContent = count > 0 ? count + ' vybraných' : '';
        presDrawBtn.disabled = count === 0;
        var total = document.querySelectorAll('.pres-row-check').length;
        presSelectAll.checked = count === total && total > 0;
        presSelectAll.indeterminate = count > 0 && count < total;
    }

    presSelectAll.addEventListener('change', function () {
        document.querySelectorAll('.pres-row-check').forEach(function (cb) {
            cb.checked = presSelectAll.checked;
        });
        updatePresToolbar();
    });

    document.querySelectorAll('.pres-row-check').forEach(function (cb) {
        cb.addEventListener('change', updatePresToolbar);
    });

    presDrawBtn.addEventListener('click', function () {
        var checked = getPresChecked();
        if (checked.length === 0) return;
        var ids = checked.map(function (cb) { return cb.value; }).join(',');
        window.location.href = '/Draw?presentationIds=' + ids + '&presRole=both';
    });
})();

// ── Activities/DrawResult.cshtml ──────────────────────────────────────────
(function () {
    var dataEl = document.getElementById('draw-result-data');
    if (!dataEl) return;
    const drawnNames = JSON.parse(dataEl.dataset.drawn);
    const allNames   = JSON.parse(dataEl.dataset.all);

    const slotDisplay   = document.getElementById('slot-display');
    const slotName      = document.getElementById('slot-name');
    const progressLabel = document.getElementById('progress-label');
    const resultsList   = document.getElementById('results-list');
    const completeSection = document.getElementById('complete-section');

    function buildPool() {
        let pool = [...allNames];
        while (pool.length < 20) pool = [...pool, ...allNames];
        return pool.sort(() => Math.random() - 0.5);
    }

    function addResult(name) {
        const card = document.createElement('div');
        card.className = 'student-card';
        card.textContent = name;
        resultsList.appendChild(card);
        requestAnimationFrame(() => requestAnimationFrame(() => card.classList.add('visible')));
    }

    function showComplete() {
        progressLabel.textContent = '';
        completeSection.classList.add('visible');
    }

    function revealStudent(index) {
        if (index >= drawnNames.length) { showComplete(); return; }
        const target = drawnNames[index];
        progressLabel.textContent = drawnNames.length === 1
            ? 'Žrebuje sa študent…'
            : `Žrebuje sa študent ${index + 1} z ${drawnNames.length}…`;
        slotDisplay.classList.remove('locked');
        slotDisplay.classList.add('spinning');
        slotName.innerHTML = '';
        const pool = buildPool();
        let poolIndex = 0;
        const schedule = [[800, 55], [600, 95], [400, 160], [300, 260], [200, 370]];
        let scheduleIdx = 0;
        let phaseEnd = Date.now() + schedule[0][0];

        function tick() {
            slotName.textContent = pool[poolIndex % pool.length];
            poolIndex++;
            const now = Date.now();
            if (now >= phaseEnd) {
                scheduleIdx++;
                if (scheduleIdx >= schedule.length) {
                    slotDisplay.classList.remove('spinning');
                    slotDisplay.classList.add('locked');
                    slotName.textContent = target;
                    setTimeout(() => {
                        addResult(target);
                        setTimeout(() => revealStudent(index + 1), drawnNames.length > 1 ? 900 : 600);
                    }, 1000);
                    return;
                }
                phaseEnd = now + schedule[scheduleIdx][0];
            }
            setTimeout(tick, schedule[scheduleIdx][1]);
        }

        setTimeout(tick, index === 0 ? 600 : 400);
    }

    revealStudent(0);
})();

// ── Attendance/Record.cshtml ──────────────────────────────────────────────
(function () {
    var dateInput = document.getElementById('dateInput');
    if (!dateInput) return;
    dateInput.addEventListener('change', function () {
        var gid = document.querySelector('input[name="GroupId"]').value;
        var date = this.value;
        window.location.href = '/Attendance/Record?groupId=' + gid + '&date=' + date;
    });
})();

(function () {
    function normalize(str) {
        return (str || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
    }
    var input = document.getElementById('studentSearch');
    if (!input) return;
    input.addEventListener('input', function () {
        var filter = normalize(this.value.trim());
        document.querySelectorAll('#attendanceTable tbody tr').forEach(function (row) {
            var name = normalize(row.cells[0]?.textContent.trim() ?? '');
            row.style.display = (!filter || name.includes(filter)) ? '' : 'none';
        });
    });
})();

// ── Evaluations/Index.cshtml ──────────────────────────────────────────────
(function () {
    function normalize(str) {
        return (str || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
    }
    var input = document.getElementById('studentSearch');
    if (!input || !document.getElementById('evalTable')) return;
    input.addEventListener('input', function () {
        var filter = normalize(this.value.trim());
        document.querySelectorAll('#evalTable tbody tr').forEach(function (row) {
            var name = normalize(row.cells[0]?.textContent.trim() ?? '');
            row.style.display = (!filter || name.includes(filter)) ? '' : 'none';
        });
    });
})();

// ── Students/Index.cshtml ─────────────────────────────────────────────────
var updateToolbar;

(function () {
    function normalize(str) {
        return (str || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
    }
    var searchInput  = document.getElementById('studentSearch');
    var statusFilter = document.getElementById('statusFilter');
    var yearFilter   = document.getElementById('yearFilter');
    var clearBtn     = document.getElementById('clearFilters');
    var filterCount  = document.getElementById('filterCount');
    var noResults    = document.getElementById('noResultsMsg');
    if (!searchInput || !statusFilter) return;

    var allRows = Array.from(document.querySelectorAll('tbody tr'));

    function applyFilters() {
        var q      = normalize(searchInput.value.trim());
        var status = statusFilter.value;
        var year   = yearFilter.value;
        var visible = 0;
        allRows.forEach(function (row) {
            var matchSearch = !q ||
                normalize(row.dataset.name).includes(q) ||
                normalize(row.dataset.email).includes(q) ||
                normalize(row.dataset.card).includes(q);
            var matchStatus = !status || row.dataset.active === status;
            var matchYear   = !year   || row.dataset.year  === year;
            var show = matchSearch && matchStatus && matchYear;
            row.style.display = show ? '' : 'none';
            if (show) visible++;
        });
        var hasFilter = q || status || year;
        clearBtn.classList.toggle('d-none', !hasFilter);
        noResults.classList.toggle('d-none', visible > 0 || allRows.length === 0);
        filterCount.textContent = hasFilter ? visible + ' z ' + allRows.length + ' študentov' : '';
        if (typeof updateToolbar === 'function') updateToolbar();
    }

    function clearAll() {
        searchInput.value = '';
        statusFilter.value = '';
        yearFilter.value = '';
        applyFilters();
    }

    searchInput.addEventListener('input',   applyFilters);
    statusFilter.addEventListener('change', applyFilters);
    yearFilter.addEventListener('change',   applyFilters);
    clearBtn.addEventListener('click',      clearAll);
})();

(function () {
    var selectAll = document.getElementById('selectAll');
    if (!selectAll || !document.getElementById('bulkActivateBtn')) return;
    var countLabel        = document.getElementById('selectionCount');
    var bulkActivateBtn   = document.getElementById('bulkActivateBtn');
    var bulkDeactivateBtn = document.getElementById('bulkDeactivateBtn');
    var bulkDeleteBtn     = document.getElementById('bulkDeleteBtn');

    function getChecked() {
        return Array.from(document.querySelectorAll('.row-check:checked'));
    }

    updateToolbar = function () {
        var checked = getChecked();
        var count = checked.length;
        var hasSelection = count > 0;
        countLabel.textContent = count + ' vybraných';
        countLabel.classList.toggle('d-none', !hasSelection);
        bulkActivateBtn.disabled   = !hasSelection;
        bulkDeactivateBtn.disabled = !hasSelection;
        bulkDeleteBtn.disabled     = !hasSelection;
        var visibleBoxes = Array.from(document.querySelectorAll('.row-check'))
            .filter(function (cb) { return cb.closest('tr').style.display !== 'none'; });
        var visibleChecked = visibleBoxes.filter(function (cb) { return cb.checked; });
        selectAll.checked = visibleBoxes.length > 0 && visibleChecked.length === visibleBoxes.length;
        selectAll.indeterminate = visibleChecked.length > 0 && visibleChecked.length < visibleBoxes.length;
    };

    selectAll.addEventListener('change', function () {
        var visibleBoxes = Array.from(document.querySelectorAll('.row-check'))
            .filter(function (cb) { return cb.closest('tr').style.display !== 'none'; });
        visibleBoxes.forEach(function (cb) { cb.checked = selectAll.checked; });
        updateToolbar();
    });

    document.querySelectorAll('.row-check').forEach(function (cb) {
        cb.addEventListener('change', updateToolbar);
    });

    function bulkSetActive(active) {
        var checked = getChecked();
        if (checked.length === 0) return;
        var names  = checked.map(function (cb) { return cb.dataset.name; }).join(', ');
        var action = active ? 'Aktivovať' : 'Deaktivovať';
        var msg = checked.length === 1
            ? action + ' "' + names + '"?'
            : action + ' ' + checked.length + ' študentov: ' + names + '?';
        var modalEl = document.getElementById('confirmModal');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        document.getElementById('confirmModalTitle').textContent = action + ' študentov';
        document.getElementById('confirmModalBody').textContent = msg;
        var actionBtn = document.getElementById('confirmModalAction');
        var newBtn = actionBtn.cloneNode(true);
        actionBtn.parentNode.replaceChild(newBtn, actionBtn);
        newBtn.addEventListener('click', function () {
            modal.hide();
            var ids = checked.map(function (cb) { return parseInt(cb.value); });
            bulkActivateBtn.disabled   = true;
            bulkDeactivateBtn.disabled = true;
            fetch('/Students/BulkSetActive', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ studentIds: ids, active: active })
            }).then(function (r) { return r.json(); })
              .then(function (d) {
                  if (d.success) location.reload();
                  else alert(d.message || action + ' zlyhalo.');
              })
              .catch(function () { alert(action + ' zlyhalo.'); });
        });
        modal.show();
    }

    bulkActivateBtn.addEventListener('click',   function () { bulkSetActive(true);  });
    bulkDeactivateBtn.addEventListener('click', function () { bulkSetActive(false); });

    bulkDeleteBtn.addEventListener('click', function () {
        var checked = getChecked();
        if (checked.length === 0) return;
        var names = checked.map(function (cb) { return cb.dataset.name; }).join(', ');
        var msg = checked.length === 1
            ? 'Natárvalo vymazať "' + names + '"? Túto akciu nemožno vrátiť.'
            : 'Natárvalo vymazať ' + checked.length + ' študentov: ' + names + '? Túto akciu nemožno vrátiť.';
        var modalEl = document.getElementById('confirmModal');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        document.getElementById('confirmModalTitle').textContent = 'Vymazať študentov';
        document.getElementById('confirmModalBody').textContent = msg;
        var actionBtn = document.getElementById('confirmModalAction');
        var newBtn = actionBtn.cloneNode(true);
        actionBtn.parentNode.replaceChild(newBtn, actionBtn);
        newBtn.addEventListener('click', function () {
            modal.hide();
            var ids = checked.map(function (cb) { return parseInt(cb.value); });
            bulkDeleteBtn.disabled = true;
            fetch('/Students/BulkDelete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(ids)
            }).then(function (r) { return r.json(); })
              .then(function (d) {
                  if (d.success) location.reload();
                  else alert(d.message || 'Vymazanie zlyhalo.');
              })
              .catch(function () { alert('Vymazanie zlyhalo.'); });
        });
        modal.show();
    });

    document.querySelectorAll('.btn-delete-student').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var studentId   = this.dataset.studentId;
            var studentName = this.dataset.studentName;
            var modalEl = document.getElementById('confirmModal');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            document.getElementById('confirmModalTitle').textContent = 'Vymazať študenta';
            document.getElementById('confirmModalBody').textContent =
                'Naozaj chcete vymazať "' + studentName + '"? Túto akciu nemožno vrátiť.';
            var actionBtn = document.getElementById('confirmModalAction');
            var newBtn = actionBtn.cloneNode(true);
            actionBtn.parentNode.replaceChild(newBtn, actionBtn);
            newBtn.addEventListener('click', function () {
                modal.hide();
                fetch('/Students/Delete/' + studentId, { method: 'POST' })
                    .then(function (r) { return r.json(); })
                    .then(function (d) {
                        if (d.success) location.reload();
                        else alert(d.message || 'Vymazanie zlyhalo.');
                    })
                    .catch(function () { alert('Vymazanie zlyhalo.'); });
            });
            modal.show();
        });
    });
})();

// ── Students/ImportPreview.cshtml ─────────────────────────────────────────
(function () {
    document.querySelectorAll('.col-toggle').forEach(function (cb) {
        cb.addEventListener('change', function () {
            var col = this.dataset.col;
            var visible = this.checked;
            document.querySelectorAll('.' + col).forEach(function (el) {
                el.style.display = visible ? '' : 'none';
            });
        });
    });
})();

// ── CustomExport/Index.cshtml ─────────────────────────────────────────────
(function () {
    document.querySelectorAll('.section-cb').forEach(function (cb) {
        var cardId = cb.dataset.card;
        var card = document.querySelector('.section-card[data-card="' + cardId + '"]');
        if (!card) return;
        var icon = card.querySelector('.check-icon');
        var fieldLabels = card.querySelectorAll('.student-field-label');

        function syncFields(enabled) {
            fieldLabels.forEach(function (lbl) {
                lbl.classList.toggle('disabled', !enabled);
                var fcb = lbl.querySelector('.student-field-cb');
                if (fcb) fcb.disabled = !enabled;
            });
        }

        function sync() {
            if (cb.checked) {
                card.classList.add('active');
                icon.className = 'bi bi-check-circle-fill ms-auto text-primary check-icon';
                icon.style.fontSize = '1.2rem';
            } else {
                card.classList.remove('active');
                icon.className = 'bi bi-circle ms-auto text-muted check-icon';
                icon.style.fontSize = '1.2rem';
            }
            syncFields(cb.checked);
        }

        cb.closest('label').addEventListener('click', function (e) {
            e.preventDefault();
            cb.checked = !cb.checked;
            sync();
        });

        fieldLabels.forEach(function (lbl) {
            lbl.addEventListener('click', function (e) {
                e.stopPropagation();
                var fcb = lbl.querySelector('.student-field-cb');
                if (fcb && !fcb.disabled) {
                    fcb.checked = !fcb.checked;
                    var badge = lbl.querySelector('.student-field-badge');
                    if (badge) badge.style.borderColor = fcb.checked ? '#1e3a5f' : 'transparent';
                }
            });
        });

        sync();
    });
})();
