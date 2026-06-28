// Field-builder option modal — reads/writes options_json for each FieldType.
(function () {
    'use strict';

    var modalEl = document.getElementById('field-options-modal');
    if (!modalEl) return;

    var bsModal = new bootstrap.Modal(modalEl);
    var currentForm = null;   // the <form> that triggered the modal

    // ── Init ──────────────────────────────────────────────────────────────────

    // Wire open buttons
    document.querySelectorAll('.fo-open-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            currentForm = btn.closest('form');
            if (!currentForm) return;
            openModal();
        });
    });

    // Apply button
    document.getElementById('fo-apply-btn').addEventListener('click', applyOptions);

    // Choices: Enter key + add button
    var choiceInput = document.getElementById('fo-choice-input');
    choiceInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); addChoice(); }
    });
    document.getElementById('fo-choice-add').addEventListener('click', addChoice);

    // Relation: reload display-field dropdown when target type changes
    document.getElementById('fo-target-type').addEventListener('change', function () {
        loadDisplayFields(this.value, null);
    });

    // On page load: render summary for any pre-filled options_json
    document.querySelectorAll('#fo-raw-json').forEach(function (ta) {
        var form = ta.closest('form');
        if (!form) return;
        var summary = form.querySelector('.field-options-summary');
        if (summary && ta.value.trim()) {
            var typeName = getTypeName(form);
            try {
                var data = JSON.parse(ta.value);
                renderSummary(summary, typeName, data);
            } catch (_) {}
        }
    });

    // ── Open modal ────────────────────────────────────────────────────────────

    function openModal() {
        var typeName = getTypeName(currentForm);
        showGroup(typeName);

        var rawJson = currentForm.querySelector('#fo-raw-json');
        var json = rawJson ? rawJson.value.trim() : '';
        populateFromJson(typeName, json);

        bsModal.show();
    }

    function getTypeName(form) {
        var sel = form ? form.querySelector('[name="FieldType"]') : null;
        if (!sel) return '';
        var opt = sel.options[sel.selectedIndex];
        return opt ? opt.text : '';
    }

    // ── Group visibility ──────────────────────────────────────────────────────

    function showGroup(typeName) {
        modalEl.querySelectorAll('.fo-group').forEach(function (g) {
            var types = (g.getAttribute('data-types') || '').split(' ');
            g.style.display = types.indexOf(typeName) !== -1 ? '' : 'none';
        });
        // min/max only for MultiRelation
        var minmax = document.getElementById('fo-relation-minmax');
        if (minmax) minmax.style.display = typeName === 'MultiRelation' ? '' : 'none';
    }

    // ── Populate inputs from existing JSON ───────────────────────────────────

    function populateFromJson(typeName, json) {
        // Reset
        modalEl.querySelectorAll('.fo-input').forEach(function (el) {
            el.tagName === 'SELECT' ? (el.selectedIndex = 0) : (el.value = '');
        });
        document.getElementById('fo-choices-list').innerHTML = '';

        // Relation dropdowns must always be loaded (even for a brand-new field with no JSON yet).
        if (typeName === 'Relation' || typeName === 'MultiRelation') {
            var relData = null;
            try { if (json && json !== '{}') relData = JSON.parse(json); } catch (_) {}
            loadContentTypes(function () {
                if (relData && relData.target_content_type_id) {
                    setInput('target_content_type_id', relData.target_content_type_id);
                    loadDisplayFields(relData.target_content_type_id, function () {
                        if (relData.display_field_slug) setInput('display_field_slug', relData.display_field_slug);
                    });
                }
            });
            if (relData && typeName === 'MultiRelation') {
                setInput('min_items', relData.min_items);
                setInput('max_items', relData.max_items);
            }
            return;
        }

        if (!json || json === '{}') return;

        var data;
        try { data = JSON.parse(json); } catch (_) { return; }

        if (typeName === 'Text') {
            setInput('max_length', data.max_length);
            setInput('placeholder', data.placeholder);
        } else if (typeName === 'RichText' || typeName === 'Markdown') {
            setInput('placeholder', data.placeholder);
        } else if (typeName === 'Number' || typeName === 'Decimal') {
            setInput('min', data.min);
            setInput('max', data.max);
            setInput('decimals', data.decimals);
        } else if (typeName === 'Rating') {
            setInput('max', data.max || 5);
        } else if (typeName === 'Select' || typeName === 'MultiSelect') {
            if (Array.isArray(data.choices)) {
                data.choices.forEach(function (c) { appendChoiceChip(String(c)); });
            }
        } else if (['Image','Video','Audio','File','Gallery'].indexOf(typeName) !== -1) {
            setInput('max_size_mb', data.max_size_mb);
            if (Array.isArray(data.allowed_formats)) {
                setInput('allowed_formats', data.allowed_formats.join(', '));
            }
        } else if (typeName === 'CodeSnippet') {
            setInput('language', data.language);
        }
    }

    function setInput(key, value) {
        if (value === undefined || value === null) return;
        var el = modalEl.querySelector('.fo-input[data-key="' + key + '"]');
        if (el) el.value = value;
    }

    // ── Serialize & apply ─────────────────────────────────────────────────────

    function applyOptions() {
        if (!currentForm) return;
        var typeName = getTypeName(currentForm);
        var data = serializeGroup(typeName);

        var rawJson = currentForm.querySelector('#fo-raw-json');
        if (rawJson) rawJson.value = data ? JSON.stringify(data) : '';

        var summary = currentForm.querySelector('.field-options-summary');
        if (summary) renderSummary(summary, typeName, data);

        bsModal.hide();
    }

    function serializeGroup(typeName) {
        var obj = {};
        if (typeName === 'Text') {
            var ml = inputVal('max_length');
            var ph = inputVal('placeholder');
            if (ml) obj.max_length = parseInt(ml, 10);
            if (ph) obj.placeholder = ph;
        } else if (typeName === 'RichText' || typeName === 'Markdown') {
            var ph2 = inputVal('placeholder');
            if (ph2) obj.placeholder = ph2;
        } else if (typeName === 'Number' || typeName === 'Decimal') {
            var mn = inputVal('min'), mx = inputVal('max'), dc = inputVal('decimals');
            if (mn !== '') obj.min = parseFloat(mn);
            if (mx !== '') obj.max = parseFloat(mx);
            if (dc !== '') obj.decimals = parseInt(dc, 10);
        } else if (typeName === 'Rating') {
            obj.max = parseInt(inputVal('max') || '5', 10);
        } else if (typeName === 'Select' || typeName === 'MultiSelect') {
            var chips = document.querySelectorAll('#fo-choices-list .fo-choice-chip');
            var choices = Array.from(chips).map(function (c) { return c.getAttribute('data-value'); }).filter(Boolean);
            if (choices.length) obj.choices = choices;
        } else if (['Image','Video','Audio','File','Gallery'].indexOf(typeName) !== -1) {
            var sz = inputVal('max_size_mb');
            var fmts = inputVal('allowed_formats');
            if (sz) obj.max_size_mb = parseInt(sz, 10);
            if (fmts) {
                obj.allowed_formats = fmts.split(',')
                    .map(function (f) { return f.trim().toLowerCase().replace(/^\./, ''); })
                    .filter(Boolean);
            }
        } else if (typeName === 'CodeSnippet') {
            var lang = inputVal('language');
            if (lang) obj.language = lang;
        } else if (typeName === 'Relation' || typeName === 'MultiRelation') {
            var tid = inputVal('target_content_type_id');
            if (!tid) return null;
            obj.target_content_type_id = parseInt(tid, 10);
            var dfs = inputVal('display_field_slug');
            if (dfs) obj.display_field_slug = dfs;
            if (typeName === 'MultiRelation') {
                var minI = inputVal('min_items'), maxI = inputVal('max_items');
                if (minI !== '') obj.min_items = parseInt(minI, 10);
                if (maxI !== '') obj.max_items = parseInt(maxI, 10);
            }
        }
        return Object.keys(obj).length ? obj : null;
    }

    function inputVal(key) {
        var el = modalEl.querySelector('.fo-input[data-key="' + key + '"]');
        return el ? el.value.trim() : '';
    }

    // ── Summary line ──────────────────────────────────────────────────────────

    function renderSummary(el, typeName, data) {
        if (!data) { el.textContent = ''; return; }
        var parts = [];
        if (data.max_length) parts.push('max: ' + data.max_length + ' karakter');
        if (data.placeholder) parts.push('placeholder');
        if (data.min !== undefined) parts.push('min: ' + data.min);
        if (data.max !== undefined && typeName === 'Rating') parts.push('/' + data.max + '★');
        else if (data.max !== undefined) parts.push('max: ' + data.max);
        if (data.decimals !== undefined) parts.push(data.decimals + ' ondalık');
        if (data.choices) parts.push(data.choices.length + ' seçenek');
        if (data.max_size_mb) parts.push(data.max_size_mb + ' MB');
        if (data.allowed_formats && data.allowed_formats.length) parts.push(data.allowed_formats.join('/'));
        if (data.language) parts.push(data.language);
        if (data.target_content_type_id) parts.push('hedef: #' + data.target_content_type_id);
        if (data.display_field_slug) parts.push('etiket: ' + data.display_field_slug);
        if (data.min_items !== undefined) parts.push('min: ' + data.min_items);
        if (data.max_items !== undefined) parts.push('max: ' + data.max_items);
        el.textContent = parts.join(' · ');
    }

    // ── Choices editor ────────────────────────────────────────────────────────

    function addChoice() {
        var val = choiceInput.value.trim();
        if (!val) return;
        appendChoiceChip(val);
        choiceInput.value = '';
        choiceInput.focus();
    }

    function appendChoiceChip(value) {
        var list = document.getElementById('fo-choices-list');
        var chip = document.createElement('span');
        chip.className = 'badge bg-secondary fo-choice-chip d-inline-flex align-items-center gap-1';
        chip.setAttribute('data-value', value);
        var text = document.createTextNode(value);
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn-close btn-close-white';
        btn.style.cssText = 'font-size:.65em';
        btn.addEventListener('click', function () { chip.remove(); });
        chip.appendChild(text);
        chip.appendChild(btn);
        list.appendChild(chip);
    }

    // ── Relation: async dropdowns ─────────────────────────────────────────────

    var _ctCache = null;

    function loadContentTypes(callback) {
        var sel = document.getElementById('fo-target-type');
        var url = sel.getAttribute('data-types-url');
        if (!url) { if (callback) callback(); return; }

        if (_ctCache) {
            populateSelect(sel, _ctCache, 'id', 'name');
            if (callback) callback();
            return;
        }

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                _ctCache = data;
                populateSelect(sel, data, 'id', 'name');
                if (callback) callback();
            })
            .catch(function () { if (callback) callback(); });
    }

    function loadDisplayFields(targetId, callback) {
        var sel = document.getElementById('fo-display-field');
        var tmpl = document.getElementById('fo-target-type').getAttribute('data-fields-url-template');
        sel.innerHTML = '<option value=""></option>';
        if (!targetId || !tmpl) { if (callback) callback(); return; }

        var url = tmpl.replace('{id}', targetId);
        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                data.forEach(function (f) {
                    var opt = document.createElement('option');
                    opt.value = f.slug;
                    opt.textContent = f.name + ' (' + f.slug + ')';
                    sel.appendChild(opt);
                });
                if (callback) callback();
            })
            .catch(function () { if (callback) callback(); });
    }

    function populateSelect(sel, items, valKey, labelKey) {
        var prev = sel.value;
        sel.innerHTML = '<option value="">—</option>';
        items.forEach(function (item) {
            var opt = document.createElement('option');
            opt.value = item[valKey];
            opt.textContent = item[labelKey];
            sel.appendChild(opt);
        });
        if (prev) sel.value = prev;
    }

})();
