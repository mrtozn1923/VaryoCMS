// Searchable media picker for Image/Video/Audio/File (single) and Gallery (multiple).
// Single fields store one media id in the hidden input; Gallery stores a JSON array of ids.
(function () {
    function debounce(fn, ms) {
        let t;
        return function (...a) { clearTimeout(t); t = setTimeout(() => fn.apply(this, a), ms); };
    }

    function init(picker) {
        const multiple = picker.dataset.multiple === 'true';
        const mediaType = picker.dataset.mediaType || '';
        const searchUrl = picker.dataset.searchUrl;
        const hidden = picker.querySelector('.mp-value');
        const chips = picker.querySelector('.mp-chips');
        const search = picker.querySelector('.mp-search');
        const results = picker.querySelector('.mp-results');

        function ids() {
            const raw = (hidden.value || '').trim();
            if (!raw) return [];
            if (!multiple) return [raw];
            try { const a = JSON.parse(raw); return Array.isArray(a) ? a.map(String) : []; }
            catch (e) { return []; }
        }
        function setIds(list) {
            if (!multiple) { hidden.value = list.length ? String(list[0]) : ''; return; }
            hidden.value = list.length ? JSON.stringify(list.map(Number)) : '';
        }

        function addChip(m) {
            const span = document.createElement('span');
            span.className = 'mp-chip border rounded p-1 d-inline-flex align-items-center';
            span.dataset.id = m.id;
            if (m.mediaType === 'image' && m.url) {
                const img = document.createElement('img');
                img.src = m.url; img.alt = '';
                img.style.cssText = 'height:32px;width:32px;object-fit:cover;';
                img.className = 'me-1';
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

        function select(m) {
            const id = String(m.id);
            const current = ids();
            if (!multiple) { chips.innerHTML = ''; setIds([]); }
            if (current.indexOf(id) !== -1) { results.style.display = 'none'; return; }
            addChip(m);
            setIds(ids().concat(id));
            results.style.display = 'none';
            search.value = '';
        }

        chips.addEventListener('click', function (e) {
            if (!e.target.classList.contains('mp-remove')) return;
            e.preventDefault();
            const chip = e.target.closest('.mp-chip');
            const id = String(chip.dataset.id);
            chip.remove();
            setIds(ids().filter(x => x !== id));
        });

        const doSearch = debounce(function () {
            const url = searchUrl + '?q=' + encodeURIComponent(search.value.trim())
                + (mediaType ? '&type=' + encodeURIComponent(mediaType) : '');
            fetch(url, { headers: { 'Accept': 'application/json' } })
                .then(r => r.ok ? r.json() : [])
                .then(items => {
                    results.innerHTML = '';
                    if (!items || !items.length) { results.style.display = 'none'; return; }
                    items.forEach(m => {
                        const a = document.createElement('a');
                        a.href = '#';
                        a.className = 'list-group-item list-group-item-action py-1 d-flex align-items-center';
                        if (m.mediaType === 'image' && m.url) {
                            const img = document.createElement('img');
                            img.src = m.url; img.alt = '';
                            img.style.cssText = 'height:28px;width:28px;object-fit:cover;';
                            img.className = 'me-2';
                            a.appendChild(img);
                        }
                        const t = document.createElement('span');
                        t.className = 'small';
                        t.textContent = (m.originalName || ('#' + m.id)) + '  (' + m.mediaType + ')';
                        a.appendChild(t);
                        a.addEventListener('click', function (e) { e.preventDefault(); select(m); });
                        results.appendChild(a);
                    });
                    results.style.display = 'block';
                })
                .catch(() => { results.style.display = 'none'; });
        }, 250);

        search.addEventListener('input', doSearch);
        search.addEventListener('focus', function () { if (results.children.length) results.style.display = 'block'; });
        document.addEventListener('click', function (e) {
            if (!picker.contains(e.target)) results.style.display = 'none';
        });
    }

    document.querySelectorAll('.media-picker').forEach(init);
})();
