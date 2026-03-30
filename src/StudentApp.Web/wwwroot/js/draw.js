document.addEventListener('DOMContentLoaded', function () {
    var drawData     = document.getElementById('draw-data');
    var cardsGrid    = document.getElementById('draw-cards');
    var addCardBtn   = document.getElementById('add-card-btn');
    var drawAllBtn   = document.getElementById('draw-all-btn');

    if (!drawData || !cardsGrid) return;

    var allNames      = JSON.parse(drawData.dataset.allNames  || '[]');
    var activities    = JSON.parse(drawData.dataset.activities || '[]');
    var initialIds    = JSON.parse(drawData.dataset.initialIds || '[]');

    var cardCounter   = 0;
    var isDrawingAll  = false;

    // Five-phase slow-down schedule: [phase duration ms, tick interval ms]
    var schedule = [
        [800,  55],
        [600,  95],
        [400, 160],
        [300, 260],
        [200, 370]
    ];

    // ── Pool builder ──────────────────────────────────────────────────────────
    function buildPool() {
        var pool = allNames.slice();
        while (pool.length < 20) pool = pool.concat(allNames);
        return pool.sort(function () { return Math.random() - 0.5; });
    }

    // ── Global toolbar state ──────────────────────────────────────────────────
    function updateAllButtons() {
        var hasValid = Array.from(cardsGrid.querySelectorAll('.card-activity-select'))
            .some(function (sel) { return sel.value !== ''; });
        if (drawAllBtn) drawAllBtn.disabled = isDrawingAll || !hasValid;

        Array.from(cardsGrid.children).forEach(function (col) {
            if (typeof col._updateCardBtn === 'function') col._updateCardBtn();
        });
    }

    // ── Card creation ─────────────────────────────────────────────────────────
    function createCard(preselectedActivityId) {
        var id = ++cardCounter;

        var opts = '<option value="">— Vyberte aktivitu —</option>';
        activities.forEach(function (a) {
            var sel = (preselectedActivityId && String(a.Id) === String(preselectedActivityId))
                ? ' selected' : '';
            opts += '<option value="' + a.Id + '"' + sel + '>'
                 + escapeHtml(a.Name) + '</option>';
        });

        var preselectedName = '';
        if (preselectedActivityId) {
            var found = activities.find(function (a) {
                return String(a.Id) === String(preselectedActivityId);
            });
            if (found) preselectedName = found.Name;
        }

        var wrapper = document.createElement('div');
        wrapper.className = 'col-md-6 col-xl-4';
        wrapper.id = 'draw-card-' + id;
        wrapper.innerHTML =
            '<div class="card h-100 border shadow-sm draw-card-inner">'
          +   '<div class="card-header d-flex align-items-center gap-2 py-2">'
          +     '<i class="bi bi-dice-3 text-primary"></i>'
          +     '<span class="fw-semibold small">Žrebovanie</span>'
          +     '<button type="button" class="btn-close ms-auto remove-card-btn" title="Odstrániť kartu"></button>'
          +   '</div>'
          +   '<div class="card-body d-flex flex-column gap-3">'

          // Activity selector
          +     '<div>'
          +       '<label class="form-label small fw-semibold mb-1">Aktivita</label>'
          +       '<select class="form-select form-select-sm card-activity-select">' + opts + '</select>'
          +     '</div>'

          // Eligible students list
          +     '<div class="card-eligible-wrap">'
          +       '<div class="d-flex justify-content-between align-items-center mb-1">'
          +         '<label class="form-label small fw-semibold mb-0">Oprávnení študenti</label>'
          +         '<span class="badge bg-secondary card-eligible-badge" style="display:none"></span>'
          +       '</div>'
          +       '<div class="form-check mb-1">'
          +         '<input class="form-check-input card-include-assigned" type="checkbox" />'
          +         '<label class="form-check-label small text-muted">'
          +           'Zahrnúť študentov už priradených k iným aktivitám'
          +         '</label>'
          +       '</div>'
          +       '<div class="card-eligible-list border rounded p-2" '
          +            'style="max-height:110px;overflow-y:auto;min-height:34px;background:#f8f9fa">'
          +         '<span class="text-muted small fst-italic">Najskôr vyberte aktivitu</span>'
          +       '</div>'
          +     '</div>'

          // Count input
          +     '<div>'
          +       '<label class="form-label small fw-semibold mb-1">Počet študentov na žrebovanie</label>'
          +       '<div class="input-group input-group-sm">'
          +         '<input type="number" class="form-control card-count-input" min="1" value="1" disabled />'
          +         '<span class="input-group-text text-muted card-count-max">/ —</span>'
          +       '</div>'
          +     '</div>'

          // Slot drum
          +     '<div class="slot-outer slot-outer-compact">'
          +       '<div class="slot-display card-slot-display">'
          +         '<span class="slot-name card-slot-name" style="font-size:1.1rem;color:#6c757d">'
          +           escapeHtml(preselectedName || 'Vyberte aktivitu')
          +         '</span>'
          +       '</div>'
          +     '</div>'

          // Result pills
          +     '<div class="card-results text-center" style="min-height:28px">'
          +       '<div class="card-results-list d-flex flex-wrap justify-content-center gap-2"></div>'
          +     '</div>'

          // Complete message
          +     '<p class="card-complete-msg text-success fw-semibold mb-0 text-center" style="opacity:0;transition:opacity .5s">'
          +       '<i class="bi bi-check-circle-fill me-1"></i>Hotovo!'
          +     '</p>'
          +   '</div>'

          // Card footer with Draw button
          +   '<div class="card-footer d-flex justify-content-end py-2">'
          +     '<button type="button" class="btn btn-sm btn-primary card-draw-btn" disabled>'
          +       '<i class="bi bi-shuffle me-1"></i>Žrebovať'
          +     '</button>'
          +   '</div>'
          + '</div>';

        // ── Element refs ──────────────────────────────────────────────────────
        var actSel           = wrapper.querySelector('.card-activity-select');
        var slotName         = wrapper.querySelector('.card-slot-name');
        var removeBtn        = wrapper.querySelector('.remove-card-btn');
        var cardDrawBtn      = wrapper.querySelector('.card-draw-btn');
        var countInput       = wrapper.querySelector('.card-count-input');
        var countMax         = wrapper.querySelector('.card-count-max');
        var eligibleList     = wrapper.querySelector('.card-eligible-list');
        var eligibleBadge    = wrapper.querySelector('.card-eligible-badge');
        var includeAssignedCb = wrapper.querySelector('.card-include-assigned');
        var isCardDrawing    = false;

        // While typing: only clamp the upper bound so the field can be cleared/retyped freely
        countInput.addEventListener('input', function () {
            var max = parseInt(this.max, 10);
            var val = parseInt(this.value, 10);
            if (!isNaN(max) && max > 0 && !isNaN(val) && val > max) {
                this.value = max;
            }
        });
        // On blur: enforce the lower bound as well
        countInput.addEventListener('blur', function () {
            var max = parseInt(this.max, 10);
            var val = parseInt(this.value, 10);
            if (isNaN(val) || val < 1) this.value = 1;
            if (!isNaN(max) && max > 0 && val > max) this.value = max;
        });

        // ── Per-card button updater ───────────────────────────────────────────
        wrapper._updateCardBtn = function () {
            cardDrawBtn.disabled = !actSel.value || isCardDrawing || isDrawingAll
                || parseInt(countInput.max || '0', 10) === 0;
        };

        // ── Eligible students loader ──────────────────────────────────────────
        function loadEligible(activityId) {
            eligibleList.innerHTML = '<span class="text-muted small">'
                + '<span class="spinner-border spinner-border-sm me-1" role="status"></span>'
                + 'Načítavanie\u2026</span>';
            eligibleBadge.style.display = 'none';
            countInput.disabled = true;
            countInput.max = 0;
            countMax.textContent = '/ —';
            wrapper._updateCardBtn();

            var include = includeAssignedCb.checked ? '&includeAlreadyAssigned=true' : '';
            fetch('/Activities/GetEligibleStudents?activityId=' + activityId + include)
                .then(function (r) { return r.json(); })
                .then(function (students) {
                    eligibleBadge.textContent = students.length;
                    eligibleBadge.style.display = '';
                    eligibleBadge.className = 'badge card-eligible-badge '
                        + (students.length > 0 ? 'bg-primary' : 'bg-secondary');

                    if (students.length === 0) {
                        eligibleList.innerHTML =
                            '<span class="text-muted small fst-italic">Všetci študenti sú už priradení.</span>';
                        countInput.disabled = true;
                        countInput.max = 0;
                        countMax.textContent = '/ 0';
                    } else {
                        eligibleList.innerHTML = students.map(function (s) {
                            return '<span class="badge bg-light text-dark border me-1 mb-1 py-1 px-2">'
                                 + escapeHtml(s.fullName) + '</span>';
                        }).join('');
                        countInput.max = students.length;
                        countInput.value = Math.min(parseInt(countInput.value, 10) || 1, students.length);
                        countInput.disabled = false;
                        countMax.textContent = '/ ' + students.length;
                    }
                    wrapper._updateCardBtn();
                    updateAllButtons();
                })
                .catch(function () {
                    eligibleList.innerHTML =
                        '<span class="text-danger small">Nepodarilo sa načítať študentov.</span>';
                    countInput.disabled = true;
                    wrapper._updateCardBtn();
                });
        }

        // Store reload fn so Draw All can refresh after completing
        wrapper._reloadEligible = function () {
            if (actSel.value) loadEligible(parseInt(actSel.value, 10));
        };

        // ── Include-assigned checkbox ─────────────────────────────────────────
        includeAssignedCb.addEventListener('change', function () {
            if (actSel.value) loadEligible(parseInt(actSel.value, 10));
        });

        // ── Activity select change ────────────────────────────────────────────
        actSel.addEventListener('change', function () {
            if (!this.value) {
                slotName.style.color    = '#6c757d';
                slotName.style.fontSize = '1.1rem';
                slotName.textContent    = 'Vyberte aktivitu';
                wrapper.querySelector('.card-slot-display').classList.remove('spinning', 'locked');
                eligibleList.innerHTML  =
                    '<span class="text-muted small fst-italic">Najskôr vyberte aktivitu</span>';
                eligibleBadge.style.display = 'none';
                countInput.disabled = true;
                countInput.max = 0;
                countMax.textContent = '/ —';
            } else {
                loadEligible(parseInt(this.value, 10));
            }
            wrapper._updateCardBtn();
            updateAllButtons();
        });

        // ── Remove card ───────────────────────────────────────────────────────
        removeBtn.addEventListener('click', function () {
            wrapper.remove();
            updateAllButtons();
        });

        // ── Individual Draw button ────────────────────────────────────────────
        cardDrawBtn.addEventListener('click', async function () {
            if (isCardDrawing || isDrawingAll) return;
            isCardDrawing = true;
            wrapper._updateCardBtn();
            if (drawAllBtn) drawAllBtn.disabled = true;

            await runCardDraw(wrapper);

            // Refresh eligible list after draw
            wrapper._reloadEligible();
            isCardDrawing = false;
            updateAllButtons();
        });

        cardsGrid.appendChild(wrapper);

        // Auto-load eligible students if pre-selected
        if (preselectedActivityId) {
            loadEligible(parseInt(preselectedActivityId, 10));
        }

        updateAllButtons();
        return wrapper;
    }

    // ── Animation: reveal one name on a specific card ─────────────────────────
    function revealOneOnCard(cardEl, name, index, total) {
        return new Promise(function (resolve) {
            var slotDisplay = cardEl.querySelector('.card-slot-display');
            var slotName    = cardEl.querySelector('.card-slot-name');
            var resultsList = cardEl.querySelector('.card-results-list');

            slotName.style.fontSize = '0.85rem';
            slotName.style.color    = '#adb5bd';
            slotName.textContent    = total === 1
                ? 'Drawing\u2026'
                : 'Drawing ' + (index + 1) + ' of ' + total + '\u2026';

            slotDisplay.classList.remove('locked');
            slotDisplay.classList.add('spinning');

            var pool      = buildPool();
            var poolIdx   = 0;
            var schedIdx  = 0;
            var phaseEnd  = Date.now() + schedule[0][0];

            function tick() {
                slotName.style.fontSize = '';
                slotName.style.color    = '';
                slotName.textContent    = pool[poolIdx % pool.length];
                poolIdx++;

                var now = Date.now();
                if (now >= phaseEnd) {
                    schedIdx++;
                    if (schedIdx >= schedule.length) {
                        slotDisplay.classList.remove('spinning');
                        slotDisplay.classList.add('locked');
                        slotName.style.fontSize = '';
                        slotName.style.color    = '';
                        slotName.textContent    = name;
                        setTimeout(function () {
                            var pill = document.createElement('div');
                            pill.className = 'student-card';
                            pill.textContent = name;
                            resultsList.appendChild(pill);
                            requestAnimationFrame(function () {
                                requestAnimationFrame(function () { pill.classList.add('visible'); });
                            });
                            setTimeout(resolve, total > 1 ? 700 : 400);
                        }, 900);
                        return;
                    }
                    phaseEnd = now + schedule[schedIdx][0];
                }
                setTimeout(tick, schedule[schedIdx][1]);
            }

            setTimeout(tick, 300);
        });
    }

    // ── Full draw sequence for one card ───────────────────────────────────────
    async function runCardDraw(cardEl) {
        var actSel           = cardEl.querySelector('.card-activity-select');
        var countInput       = cardEl.querySelector('.card-count-input');
        var slotDisplay      = cardEl.querySelector('.card-slot-display');
        var slotName         = cardEl.querySelector('.card-slot-name');
        var resultsList      = cardEl.querySelector('.card-results-list');
        var completeMsg      = cardEl.querySelector('.card-complete-msg');
        var includeAssignedCb = cardEl.querySelector('.card-include-assigned');

        var activityId = parseInt(actSel.value, 10);
        if (!activityId) return;
        var count = Math.max(1, parseInt(countInput.value, 10) || 1);
        var includeAssigned = includeAssignedCb && includeAssignedCb.checked;

        resultsList.innerHTML    = '';
        completeMsg.style.opacity = '0';
        slotDisplay.classList.remove('locked', 'spinning');

        try {
            var resp = await fetch('/Draw/DrawForActivity', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'activityId=' + activityId + '&count=' + count
                    + (includeAssigned ? '&includeAlreadyAssigned=true' : '')
            });
            var data = await resp.json();

            if (!data.success || !data.drawnNames || data.drawnNames.length === 0) {
                slotName.style.fontSize = '0.9rem';
                slotName.style.color    = '#dc3545';
                slotName.textContent    = data.message || 'Žiadni oprávnení študenti.';
                return;
            }

            var names = data.drawnNames;
            for (var i = 0; i < names.length; i++) {
                await revealOneOnCard(cardEl, names[i], i, names.length);
            }

            completeMsg.style.opacity = '1';
        } catch (e) {
            slotName.style.color    = '#dc3545';
            slotName.textContent    = 'Chyba – skúste znova.';
        }
    }

    // ── Draw All ──────────────────────────────────────────────────────────────
    if (drawAllBtn) {
        drawAllBtn.addEventListener('click', async function () {
            if (isDrawingAll) return;
            isDrawingAll = true;
            updateAllButtons();

            var validCards = Array.from(cardsGrid.children).filter(function (col) {
                var sel = col.querySelector('.card-activity-select');
                return sel && sel.value;
            });

            await Promise.all(validCards.map(function (col) { return runCardDraw(col); }));

            // Refresh eligible lists for all drawn cards
            validCards.forEach(function (col) {
                if (typeof col._reloadEligible === 'function') col._reloadEligible();
            });

            isDrawingAll = false;
            updateAllButtons();
        });
    }

    // ── Add card button ───────────────────────────────────────────────────────
    if (addCardBtn) {
        addCardBtn.addEventListener('click', function () { createCard(null); });
    }

    // ── Seed initial cards ────────────────────────────────────────────────────
    if (initialIds && initialIds.length > 0) {
        initialIds.forEach(function (actId) { createCard(actId); });
    } else {
        createCard(null);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────
    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }
});
