document.addEventListener('DOMContentLoaded', function () {
    var drawData     = document.getElementById('draw-data');
    var cardsGrid    = document.getElementById('draw-cards');
    var addCardBtn   = document.getElementById('add-card-btn');
    var addPresCardBtn = document.getElementById('add-pres-card-btn');
    var drawAllBtn   = document.getElementById('draw-all-btn');

    if (!drawData || !cardsGrid) return;

    var allNames        = JSON.parse(drawData.dataset.allNames  || '[]');
    var activities      = JSON.parse(drawData.dataset.activities || '[]');
    var initialIds      = JSON.parse(drawData.dataset.initialIds || '[]');
    var presentations   = JSON.parse(drawData.dataset.presentations || '[]');
    var initialPresIds  = JSON.parse(drawData.dataset.initialPresIds || '[]');
    var initialPresRole = drawData.dataset.initialPresRole || '0';

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
        var hasValid = Array.from(cardsGrid.querySelectorAll('.card-item-select'))
            .some(function (sel) { return sel.value !== ''; });
        if (drawAllBtn) drawAllBtn.disabled = isDrawingAll || !hasValid;

        Array.from(cardsGrid.children).forEach(function (col) {
            if (typeof col._updateCardBtn === 'function') col._updateCardBtn();
        });
    }

    // ── Card creation ─────────────────────────────────────────────────────────
    // type: 'activity' | 'presentation'
    function createCard(type, preselectedId, preselectedRole) {
        var id = ++cardCounter;
        var isPresentation = type === 'presentation';
        var items = isPresentation ? presentations : activities;
        var placeholderText = isPresentation ? '— Vyberte prezentáciu —' : '— Vyberte aktivitu —';
        var labelText = isPresentation ? 'Prezentácia' : 'Aktivita';
        var headerLabel = isPresentation ? 'Žrebovanie (prezentácia)' : 'Žrebovanie';
        var headerIcon = isPresentation ? 'bi bi-easel text-success' : 'bi bi-dice-3 text-primary';
        var includeLabel = isPresentation
            ? 'Zahrnúť študentov už priradených k iným prezentáciám'
            : 'Zahrnúť študentov už priradených k iným aktivitám';
        var emptySelectText = isPresentation ? 'Najskôr vyberte prezentáciu' : 'Najskôr vyberte aktivitu';

        var opts = '<option value="">' + placeholderText + '</option>';
        items.forEach(function (item) {
            var itemId = item.Id;
            var itemName = isPresentation ? (item.Title + ' (' + item.ActivityName + ')') : item.Name;
            var sel = (preselectedId && String(itemId) === String(preselectedId))
                ? ' selected' : '';
            opts += '<option value="' + itemId + '"' + sel + '>'
                 + escapeHtml(itemName) + '</option>';
        });

        var preselectedName = '';
        if (preselectedId) {
            var found = items.find(function (item) {
                return String(item.Id) === String(preselectedId);
            });
            if (found) preselectedName = isPresentation ? found.Title : found.Name;
        }

        var wrapper = document.createElement('div');
        wrapper.className = 'col-md-6 col-xl-4';
        wrapper.id = 'draw-card-' + id;
        wrapper.dataset.cardType = type;
        wrapper.innerHTML =
            '<div class="card h-100 border shadow-sm draw-card-inner">'
          +   '<div class="card-header d-flex align-items-center gap-2 py-2">'
          +     '<i class="' + headerIcon + '"></i>'
          +     '<span class="fw-semibold small">' + headerLabel + '</span>'
          +     '<button type="button" class="btn-close ms-auto remove-card-btn" title="Odstrániť kartu"></button>'
          +   '</div>'
          +   '<div class="card-body d-flex flex-column gap-3">'

          // Item selector
          +     '<div>'
          +       '<label class="form-label small fw-semibold mb-1">' + labelText + '</label>'
          +       '<select class="form-select form-select-sm card-item-select">' + opts + '</select>'
          +     '</div>'

          // Role selector (presentations only)
          + (isPresentation
          ?   '<div>'
          +     '<label class="form-label small fw-semibold mb-1">Rola</label>'
          +     '<select class="form-select form-select-sm card-role-select">'
          +       '<option value="0"' + ((!preselectedRole || preselectedRole === '0') ? ' selected' : '') + '>Prezentujúci</option>'
          +       '<option value="1"' + (preselectedRole === '1' ? ' selected' : '') + '>Náhradník</option>'
          +       '<option value="both"' + (preselectedRole === 'both' ? ' selected' : '') + '>Obaja (prezentujúci + náhradník)</option>'
          +     '</select>'
          +   '</div>'
          : '')

          // Eligible students list
          +     '<div class="card-eligible-wrap">'
          +       '<div class="d-flex justify-content-between align-items-center mb-1">'
          +         '<label class="form-label small fw-semibold mb-0">Oprávnení študenti</label>'
          +         '<span class="badge bg-secondary card-eligible-badge" style="display:none"></span>'
          +       '</div>'
          +       '<div class="form-check mb-1">'
          +         '<input class="form-check-input card-include-assigned" type="checkbox" />'
          +         '<label class="form-check-label small text-muted">'
          +           includeLabel
          +         '</label>'
          +       '</div>'
          +       '<div class="card-eligible-list border rounded p-2" '
          +            'style="max-height:110px;overflow-y:auto;min-height:34px;background:#f8f9fa">'
          +         '<span class="text-muted small fst-italic">' + emptySelectText + '</span>'
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
          +           escapeHtml(preselectedName || (isPresentation ? 'Vyberte prezentáciu' : 'Vyberte aktivitu'))
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
        var actSel           = wrapper.querySelector('.card-item-select');
        var slotName         = wrapper.querySelector('.card-slot-name');
        var removeBtn        = wrapper.querySelector('.remove-card-btn');
        var cardDrawBtn      = wrapper.querySelector('.card-draw-btn');
        var countInput       = wrapper.querySelector('.card-count-input');
        var countMax         = wrapper.querySelector('.card-count-max');
        var eligibleList     = wrapper.querySelector('.card-eligible-list');
        var eligibleBadge    = wrapper.querySelector('.card-eligible-badge');
        var includeAssignedCb = wrapper.querySelector('.card-include-assigned');
        var roleSelect        = wrapper.querySelector('.card-role-select'); // null for activity cards
        var isCardDrawing    = false;
        var cardType         = type;

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

        // ── Eligible list renderer ────────────────────────────────────────────
        function syncEligibleCount() {
            var remaining = eligibleList.querySelectorAll('.eligible-tag').length;
            eligibleBadge.textContent = remaining;
            eligibleBadge.className = 'badge card-eligible-badge '
                + (remaining > 0 ? 'bg-primary' : 'bg-secondary');
            countInput.max = remaining;
            countMax.textContent = '/ ' + remaining;
            if (remaining === 0) {
                countInput.disabled = true;
                countInput.value = 0;
                eligibleList.innerHTML =
                    '<span class="text-muted small fst-italic">Všetci študenti sú už priradení.</span>';
            } else {
                countInput.disabled = false;
                if (parseInt(countInput.value, 10) > remaining) countInput.value = remaining;
            }
            wrapper._updateCardBtn();
            updateAllButtons();
        }

        function renderEligibleStudents(students) {
            eligibleList.innerHTML = '';
            students.forEach(function (s) {
                var tag = document.createElement('span');
                tag.className = 'eligible-tag badge d-inline-flex align-items-center gap-1 me-1 mb-1 px-2 py-1 bg-light text-dark border';
                tag.style.fontWeight = 'normal';
                tag.dataset.studentId = s.id;
                tag.innerHTML = escapeHtml(s.fullName)
                    + '<button type="button" class="btn-close btn-close-sm eligible-remove" '
                    + 'aria-label="Odstrániť" style="font-size:.6rem;margin-left:2px"></button>';
                tag.querySelector('.eligible-remove').addEventListener('click', function () {
                    tag.remove();
                    syncEligibleCount();
                });
                eligibleList.appendChild(tag);
            });
        }

        // ── Eligible students loader ──────────────────────────────────────────
        function loadEligible(itemId) {
            eligibleList.innerHTML = '<span class="text-muted small">'
                + '<span class="spinner-border spinner-border-sm me-1" role="status"></span>'
                + 'Načítavanie\u2026</span>';
            eligibleBadge.style.display = 'none';
            countInput.disabled = true;
            countInput.max = 0;
            countMax.textContent = '/ —';
            wrapper._updateCardBtn();

            var include = includeAssignedCb.checked ? '&includeAlreadyAssigned=true' : '';
            var roleParam = '';
            if (cardType === 'presentation' && roleSelect) {
                var rv = roleSelect.value;
                // For 'both', load eligible for presentee (role=0) — the union covers all assigned students
                roleParam = '&role=' + (rv === 'both' ? '0' : rv);
            }
            var url = cardType === 'presentation'
                ? '/Tasks/GetEligiblePresentationStudents?taskId=' + itemId + include + roleParam
                : '/Activities/GetEligibleStudents?activityId=' + itemId + include;
            return fetch(url)
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
                        renderEligibleStudents(students);
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
            if (actSel.value) return loadEligible(parseInt(actSel.value, 10));
            return Promise.resolve();
        };

        // ── Include-assigned checkbox ─────────────────────────────────────────
        includeAssignedCb.addEventListener('change', function () {
            if (actSel.value) loadEligible(parseInt(actSel.value, 10));
        });

        // ── Role select change (presentations only) ───────────────────────────
        if (roleSelect) {
            roleSelect.addEventListener('change', function () {
                if (actSel.value) loadEligible(parseInt(actSel.value, 10));
            });
        }

        // ── Item select change ─────────────────────────────────────────────────
        actSel.addEventListener('change', function () {
            if (!this.value) {
                slotName.style.color    = '#6c757d';
                slotName.style.fontSize = '1.1rem';
                slotName.textContent    = cardType === 'presentation' ? 'Vyberte prezentáciu' : 'Vyberte aktivitu';
                wrapper.querySelector('.card-slot-display').classList.remove('spinning', 'locked');
                eligibleList.innerHTML  =
                    '<span class="text-muted small fst-italic">' + emptySelectText + '</span>';
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
        if (preselectedId) {
            loadEligible(parseInt(preselectedId, 10));
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
        var actSel            = cardEl.querySelector('.card-item-select');
        var countInput        = cardEl.querySelector('.card-count-input');
        var slotDisplay       = cardEl.querySelector('.card-slot-display');
        var slotName          = cardEl.querySelector('.card-slot-name');
        var resultsList       = cardEl.querySelector('.card-results-list');
        var completeMsg       = cardEl.querySelector('.card-complete-msg');
        var includeAssignedCb = cardEl.querySelector('.card-include-assigned');
        var roleSel           = cardEl.querySelector('.card-role-select');
        var cType             = cardEl.dataset.cardType || 'activity';

        var selectedId = parseInt(actSel.value, 10);
        if (!selectedId) return;
        var count = Math.max(1, parseInt(countInput.value, 10) || 1);
        var includeAssigned = includeAssignedCb && includeAssignedCb.checked;
        var roleValue = (roleSel && cType === 'presentation') ? roleSel.value : null;

        // Collect the IDs of students still visible in the eligible list
        var eligibleListEl = cardEl.querySelector('.card-eligible-list');
        var allowedIds = Array.from(eligibleListEl.querySelectorAll('.eligible-tag[data-student-id]'))
            .map(function (tag) { return tag.dataset.studentId; });

        resultsList.innerHTML     = '';
        completeMsg.style.opacity = '0';
        slotDisplay.classList.remove('locked', 'spinning');

        // Helper: draw for a specific role and return names, or null on error
        async function drawForRole(role) {
            var drawUrl, drawBody;
            if (cType === 'presentation') {
                drawUrl  = '/Draw/DrawForPresentation';
                drawBody = 'taskId=' + selectedId + '&count=' + count + '&role=' + role
                    + (includeAssigned ? '&includeAlreadyAssigned=true' : '');
            } else {
                drawUrl  = '/Draw/DrawForActivity';
                drawBody = 'activityId=' + selectedId + '&count=' + count
                    + (includeAssigned ? '&includeAlreadyAssigned=true' : '');
            }
            allowedIds.forEach(function (sid) { drawBody += '&allowedStudentIds=' + encodeURIComponent(sid); });
            var resp = await fetch(drawUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: drawBody
            });
            return resp.json();
        }

        try {
            if (cType === 'presentation' && roleValue === 'both') {
                // ── Draw Prezentujúci (role 0) then Náhradník (role 1) ────────
                var dataP = await drawForRole(0);
                if (!dataP.success || !dataP.drawnNames || dataP.drawnNames.length === 0) {
                    slotName.style.fontSize = '0.9rem';
                    slotName.style.color    = '#dc3545';
                    slotName.textContent    = dataP.message || 'Žiadni oprávnení študenti (prezentujúci).';
                    return;
                }

                // Show a section label for presentee results
                var labelP = document.createElement('div');
                labelP.className = 'w-100 text-start';
                labelP.innerHTML = '<small class="fw-semibold text-primary">Prezentujúci</small>';
                resultsList.appendChild(labelP);

                for (var i = 0; i < dataP.drawnNames.length; i++) {
                    await revealOneOnCard(cardEl, dataP.drawnNames[i], i, dataP.drawnNames.length);
                }

                // Reset slot drum for the substitution draw
                slotDisplay.classList.remove('locked', 'spinning');

                // Fetch fresh substitution-eligible students (role=1).
                // The server already excludes students assigned to this presentation
                // in any role, so the just-drawn presentees are automatically excluded.
                var subInclude = includeAssigned ? '&includeAlreadyAssigned=true' : '';
                var subEligibleUrl = '/Tasks/GetEligiblePresentationStudents?taskId=' + selectedId
                    + '&role=1' + subInclude;
                var reloadedSubs = await fetch(subEligibleUrl).then(function (r) { return r.json(); });
                var subAllowedIds = reloadedSubs.map(function (s) { return String(s.id); });

                if (subAllowedIds.length === 0) {
                    var labelSNone = document.createElement('div');
                    labelSNone.className = 'w-100 text-start mt-1';
                    labelSNone.innerHTML = '<small class="text-muted">Náhradník: žiadni oprávnení študenti.</small>';
                    resultsList.appendChild(labelSNone);
                    completeMsg.style.opacity = '1';
                    return;
                }

                var drawBodyS = 'taskId=' + selectedId + '&count=' + count + '&role=1'
                    + (includeAssigned ? '&includeAlreadyAssigned=true' : '');
                subAllowedIds.forEach(function (sid) { drawBodyS += '&allowedStudentIds=' + encodeURIComponent(sid); });
                var respS = await fetch('/Draw/DrawForPresentation', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: drawBodyS
                });
                var dataS = await respS.json();

                if (dataS.success && dataS.drawnNames && dataS.drawnNames.length > 0) {
                    var labelS = document.createElement('div');
                    labelS.className = 'w-100 text-start mt-1';
                    labelS.innerHTML = '<small class="fw-semibold text-warning">Náhradník</small>';
                    resultsList.appendChild(labelS);

                    for (var j = 0; j < dataS.drawnNames.length; j++) {
                        await revealOneOnCard(cardEl, dataS.drawnNames[j], j, dataS.drawnNames.length);
                    }
                } else {
                    var labelSFail = document.createElement('div');
                    labelSFail.className = 'w-100 text-start mt-1';
                    labelSFail.innerHTML = '<small class="text-muted">Náhradník: žiadni oprávnení študenti.</small>';
                    resultsList.appendChild(labelSFail);
                }

            } else {
                // ── Single-role draw ──────────────────────────────────────────
                var role = (cType === 'presentation' && roleValue !== null) ? parseInt(roleValue, 10) : 0;
                var data = await drawForRole(role);

                if (!data.success || !data.drawnNames || data.drawnNames.length === 0) {
                    slotName.style.fontSize = '0.9rem';
                    slotName.style.color    = '#dc3545';
                    slotName.textContent    = data.message || 'Žiadni oprávnení študenti.';
                    return;
                }

                for (var k = 0; k < data.drawnNames.length; k++) {
                    await revealOneOnCard(cardEl, data.drawnNames[k], k, data.drawnNames.length);
                }
            }

            completeMsg.style.opacity = '1';
        } catch (e) {
            slotName.style.color = '#dc3545';
            slotName.textContent = 'Chyba – skúste znova.';
        }
    }

    // ── Draw All ──────────────────────────────────────────────────────────────
    if (drawAllBtn) {
        drawAllBtn.addEventListener('click', async function () {
            if (isDrawingAll) return;
            isDrawingAll = true;
            updateAllButtons();

            var validCards = Array.from(cardsGrid.children).filter(function (col) {
                var sel = col.querySelector('.card-item-select');
                return sel && sel.value;
            });

            // Run cards sequentially so each draw completes and eligible lists
            // refresh before the next card starts (avoids duplicate draws).
            for (var i = 0; i < validCards.length; i++) {
                var col = validCards[i];
                // Refresh eligible list before drawing so the pool reflects
                // students drawn by the previous card(s).
                if (typeof col._reloadEligible === 'function') {
                    await col._reloadEligible();
                }
                await runCardDraw(col);
            }

            // Final refresh of all eligible lists
            validCards.forEach(function (col) {
                if (typeof col._reloadEligible === 'function') col._reloadEligible();
            });

            isDrawingAll = false;
            updateAllButtons();
        });
    }

    // ── Add card button ───────────────────────────────────────────────────────
    if (addCardBtn) {
        addCardBtn.addEventListener('click', function () { createCard('activity', null); });
    }

    // ── Add presentation card button ──────────────────────────────────────────
    if (addPresCardBtn) {
        addPresCardBtn.addEventListener('click', function () { createCard('presentation', null); });
    }

    // ── Seed initial cards ────────────────────────────────────────────────────
    var hasInitial = false;
    if (initialIds && initialIds.length > 0) {
        initialIds.forEach(function (actId) { createCard('activity', actId); });
        hasInitial = true;
    }
    if (initialPresIds && initialPresIds.length > 0) {
        initialPresIds.forEach(function (presId) { createCard('presentation', presId, initialPresRole); });
        hasInitial = true;
    }
    if (!hasInitial) {
        createCard('activity', null);
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
