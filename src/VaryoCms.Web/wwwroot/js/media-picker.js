// Modal-based media picker for Image / Video / Audio / File (single) and Gallery (multiple).
// One shared Bootstrap modal is reused by every .media-picker on the page.
// Hidden .mp-value encoding: single = bare id string; Gallery = JSON array of ids (unchanged).
(function () {
    if (typeof bootstrap === 'undefined') return;

    // ── Utilities ──────────────────────────────────────────────────────────────────
    function debounce(fn, ms) {
        let t;
        return function (...a) { clearTimeout(t); t = setTimeout(() => fn.apply(this, a), ms); };
    }

    function getCsrf() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function showError(container, text) {
        container.innerHTML = '';
        const d = document.createElement('div');
        d.className = 'alert alert-danger py-2 small mb-0';
        d.appendChild(document.createTextNode(text));
        container.appendChild(d);
    }

    function clearStatus(container) { container.innerHTML = ''; }

    // ── Modal elements ─────────────────────────────────────────────────────────────
    const modalEl = document.getElementById('mediaPickerModal');
    if (!modalEl) return;   // no media fields on this page

    const modal = new bootstrap.Modal(modalEl);
    const mpmSearch    = document.getElementById('mpm-search');
    const mpmGrid      = document.getElementById('mpm-grid');
    const mpmEmpty     = document.getElementById('mpm-empty');
    const mpmFile      = document.getElementById('mpm-file');
    const mpmUploadBtn = document.getElementById('mpm-upload-btn');
    const mpmStatus    = document.getElementById('mpm-status');
    const mpmDone      = document.getElementById('mpm-done');
    const mpmFieldHint = document.getElementById('mpm-field-hint');
    const tabLibraryEl = document.getElementById('mpm-tab-library');

    // Localized messages stored on the modal root element so this external JS can read them.
    const MSGS = {
        toolarge:   modalEl.dataset.msgToolarge   || 'File is too large.',
        format:     modalEl.dataset.msgFormat     || 'This file type is not allowed.',
        uploadfail: modalEl.dataset.msgUploadfail || 'Upload failed.',
        connection: modalEl.dataset.msgConnection || 'Connection error.',
    };

    // ── Per-invocation state ───────────────────────────────────────────────────────
    let activePicker    = null;   // the .media-picker div that opened the modal
    let activeMultiple  = false;
    let activeMediaType = '';
    let activeMaxMb     = null;
    let activeFormats   = [];
    let activeSearchUrl = '/admin/media/search';
    // Working set for Gallery (not committed until Done is clicked).
    // Map<String(id), {id, url, mediaType, originalName}>
    let workingSet = new Map();

    // ── Per-picker value helpers (operate on activePicker) ────────────────────────
    function ids() {
        const hidden = activePicker.querySelector('.mp-value');
        const raw = (hidden.value || '').trim();
        if (!raw) return [];
        if (!activeMultiple) return [raw];
        try { const a = JSON.parse(raw); return Array.isArray(a) ? a.map(String) : []; }
        catch (e) { return []; }
    }

    function setIds(list) {
        const hidden = activePicker.querySelector('.mp-value');
        if (!activeMultiple) { hidden.value = list.length ? String(list[0]) : ''; return; }
        hidden.value = list.length ? JSON.stringify(list.map(Number)) : '';
    }

    function addChip(m) {
        const chips = activePicker.querySelector('.mp-chips');
        const span = document.createElement('span');
        span.className = 'mp-chip p-1 d-inline-flex align-items-center';
        span.dataset.id = m.id;
        span.dataset.url = m.url || '';
        span.dataset.mediaType = m.mediaType || '';
        span.dataset.name = m.originalName || '';
        if ((m.mediaType || '').startsWith('image') && m.url) {
            const img = document.createElement('img');
            img.src = m.url; img.alt = '';
            img.className = 'cms-mp-thumb me-1';
            span.appendChild(img);
        }
        const label = document.createElement('span');
        label.className = 'small text-truncate';
        label.style.maxWidth = '160px';
        label.textContent = m.originalName || ('#' + m.id);
        span.appendChild(label);
        const x = document.createElement('a');
        x.href = '#'; x.className = 'mp-remove text-danger ms-1 text-decoration-none'; x.textContent = '×';
        span.appendChild(x);
        chips.appendChild(span);
    }

    // ── Commit helpers ─────────────────────────────────────────────────────────────
    function commitSingle(m) {
        // Clear existing chips.
        activePicker.querySelector('.mp-chips').innerHTML = '';
        addChip(m);
        setIds([String(m.id)]);
        modal.hide();
    }

    function commitGallery() {
        const chips = activePicker.querySelector('.mp-chips');
        chips.innerHTML = '';
        const ordered = [...workingSet.values()];
        ordered.forEach(addChip);
        setIds(ordered.map(m => String(m.id)));
        modal.hide();
    }

    // ── Grid rendering ─────────────────────────────────────────────────────────────
    function renderGrid(items) {
        mpmGrid.innerHTML = '';
        if (!items || !items.length) {
            mpmEmpty.style.display = '';
            return;
        }
        mpmEmpty.style.display = 'none';
        items.forEach(function (m) {
            const card = document.createElement('div');
            card.className = 'mpm-card' + (workingSet.has(String(m.id)) ? ' selected' : '');
            card.dataset.id = m.id;

            if (m.mediaType === 'image' && m.url) {
                const img = document.createElement('img');
                img.src = m.url; img.alt = m.originalName || '';
                img.className = 'mpm-thumb';
                card.appendChild(img);
            } else {
                const ph = document.createElement('div');
                ph.className = 'mpm-placeholder';
                const icon = document.createElement('i');
                icon.className = mediaTypeIcon(m.mediaType);
                ph.appendChild(icon);
                card.appendChild(ph);
            }

            const name = document.createElement('div');
            name.className = 'mpm-card-name small text-truncate';
            name.textContent = m.originalName || ('#' + m.id);
            card.appendChild(name);

            const check = document.createElement('i');
            check.className = 'bi bi-check-circle-fill mpm-selected-badge';
            card.appendChild(check);

            card.addEventListener('click', function () {
                if (!activeMultiple) {
                    commitSingle(m);
                } else {
                    const key = String(m.id);
                    if (workingSet.has(key)) {
                        workingSet.delete(key);
                        card.classList.remove('selected');
                    } else {
                        workingSet.set(key, m);
                        card.classList.add('selected');
                    }
                }
            });

            mpmGrid.appendChild(card);
        });
    }

    function mediaTypeIcon(type) {
        if (type === 'video') return 'bi bi-film';
        if (type === 'audio') return 'bi bi-music-note-beamed';
        return 'bi bi-file-earmark';
    }

    // ── Library search ─────────────────────────────────────────────────────────────
    const doSearch = debounce(function () {
        const q = mpmSearch.value.trim();
        const url = activeSearchUrl
            + '?q=' + encodeURIComponent(q)
            + (activeMediaType ? '&type=' + encodeURIComponent(activeMediaType) : '');
        fetch(url, { headers: { 'Accept': 'application/json' } })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(renderGrid)
            .catch(function () { mpmEmpty.style.display = ''; });
    }, 250);

    mpmSearch.addEventListener('input', doSearch);

    // ── Open modal for a specific picker ───────────────────────────────────────────
    function openFor(picker) {
        activePicker    = picker;
        activeMultiple  = picker.dataset.multiple === 'true';
        activeMediaType = picker.dataset.mediaType || '';
        activeMaxMb     = picker.dataset.maxSizeMb ? parseFloat(picker.dataset.maxSizeMb) : null;
        activeFormats   = picker.dataset.allowedFormats
            ? picker.dataset.allowedFormats.split(',').map(function (f) { return f.trim().replace(/^\./, '').toLowerCase(); }).filter(Boolean)
            : [];
        activeSearchUrl = picker.dataset.searchUrl || '/admin/media/search';

        // Reset UI.
        mpmSearch.value = '';
        mpmGrid.innerHTML = '';
        mpmEmpty.style.display = 'none';
        mpmFile.value = '';
        clearStatus(mpmStatus);

        // Per-field hint in upload tab.
        if (mpmFieldHint) {
            const parts = [];
            if (activeMaxMb)            parts.push('max ' + activeMaxMb + ' MB');
            if (activeFormats.length)   parts.push(activeFormats.join(', '));
            if (parts.length) {
                mpmFieldHint.textContent = parts.join(' · ');
                mpmFieldHint.style.display = '';
            } else {
                mpmFieldHint.style.display = 'none';
            }
        }

        // Gallery: seed working set from existing chips.
        workingSet = new Map();
        if (activeMultiple) {
            picker.querySelectorAll('.mp-chip').forEach(function (chip) {
                const key = String(chip.dataset.id);
                workingSet.set(key, {
                    id: chip.dataset.id,
                    url: chip.dataset.url || '',
                    mediaType: chip.dataset.mediaType || '',
                    originalName: chip.dataset.name || ('#' + chip.dataset.id),
                });
            });
            mpmDone.style.display = '';
        } else {
            mpmDone.style.display = 'none';
        }

        // Switch to Library tab and fire initial search.
        bootstrap.Tab.getOrCreateInstance(tabLibraryEl).show();
        doSearch();
        modal.show();
    }

    // ── Gallery Done button ────────────────────────────────────────────────────────
    mpmDone.addEventListener('click', commitGallery);

    // ── Upload ─────────────────────────────────────────────────────────────────────
    mpmUploadBtn.addEventListener('click', function () {
        const file = mpmFile.files && mpmFile.files[0];
        if (!file) return;

        // Client-side per-field validation (server also enforces).
        if (activeMaxMb && file.size > activeMaxMb * 1024 * 1024) {
            showError(mpmStatus, MSGS.toolarge);
            return;
        }
        if (activeFormats.length) {
            const ext = (file.name.split('.').pop() || '').toLowerCase();
            if (!activeFormats.includes(ext)) {
                showError(mpmStatus, MSGS.format);
                return;
            }
        }

        clearStatus(mpmStatus);
        mpmUploadBtn.disabled = true;

        const fd = new FormData();
        fd.append('file', file);
        fd.append('__RequestVerificationToken', getCsrf());
        if (activeMaxMb)          fd.append('maxSizeMb', String(activeMaxMb));
        if (activeFormats.length) fd.append('allowedFormats', activeFormats.join(','));

        fetch('/admin/media/upload', { method: 'POST', body: fd })
            .then(function (r) {
                return r.text().then(function (txt) {
                    return { ok: r.ok, body: txt };
                });
            })
            .then(function (res) {
                mpmUploadBtn.disabled = false;
                if (!res.ok) {
                    // Server returns either a plain error string or JSON.
                    var msg = MSGS.uploadfail;
                    try { var d = JSON.parse(res.body); if (typeof d === 'string') msg = d; } catch (e) { }
                    showError(mpmStatus, msg);
                    return;
                }
                var data;
                try { data = JSON.parse(res.body); } catch (e) { showError(mpmStatus, MSGS.uploadfail); return; }
                if (!data || !data.id) { showError(mpmStatus, MSGS.uploadfail); return; }

                if (!activeMultiple) {
                    commitSingle(data);
                } else {
                    // Add to working set, switch back to Library tab and refresh grid.
                    workingSet.set(String(data.id), data);
                    mpmFile.value = '';
                    clearStatus(mpmStatus);
                    bootstrap.Tab.getOrCreateInstance(tabLibraryEl).show();
                    doSearch();
                }
            })
            .catch(function () {
                mpmUploadBtn.disabled = false;
                showError(mpmStatus, MSGS.connection);
            });
    });

    // ── Full reset on close (isolate pickers from each other) ─────────────────────
    modalEl.addEventListener('hidden.bs.modal', function () {
        activePicker    = null;
        activeMultiple  = false;
        activeMediaType = '';
        activeMaxMb     = null;
        activeFormats   = [];
        workingSet      = new Map();
        mpmGrid.innerHTML   = '';
        mpmSearch.value     = '';
        mpmFile.value       = '';
        clearStatus(mpmStatus);
        mpmEmpty.style.display = 'none';
        mpmDone.style.display  = 'none';
    });

    // ── Per-picker initialisation ──────────────────────────────────────────────────
    document.querySelectorAll('.media-picker').forEach(function (picker) {
        // Chip remove handler.
        picker.querySelector('.mp-chips').addEventListener('click', function (e) {
            if (!e.target.classList.contains('mp-remove')) return;
            e.preventDefault();
            const chip = e.target.closest('.mp-chip');
            const pickerHidden = picker.querySelector('.mp-value');
            const multiple = picker.dataset.multiple === 'true';
            const raw = (pickerHidden.value || '').trim();
            const id = String(chip.dataset.id);
            chip.remove();
            if (!multiple) {
                pickerHidden.value = '';
            } else {
                try {
                    const arr = JSON.parse(raw);
                    pickerHidden.value = JSON.stringify(arr.filter(function (x) { return String(x) !== id; }).map(Number));
                } catch (ex) { pickerHidden.value = ''; }
            }
        });

        // Open modal button.
        const btn = picker.querySelector('.mp-open');
        if (btn) btn.addEventListener('click', function () { openFor(picker); });
    });
})();
