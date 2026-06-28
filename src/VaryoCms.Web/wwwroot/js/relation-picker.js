// Searchable relation picker. Wires every .relation-picker: type to search the target content type,
// click a result to add a chip, and keep the hidden "Relations[fieldId]" input as a CSV of ids.
(function () {
    function debounce(fn, ms) {
        let t;
        return function (...a) { clearTimeout(t); t = setTimeout(() => fn.apply(this, a), ms); };
    }

    function init(picker) {
        const multiple = picker.dataset.multiple === 'true';
        const searchUrl = picker.dataset.searchUrl;
        const displayField = picker.dataset.displayField || '';
        const lang = picker.dataset.lang || '';
        const maxItems = picker.dataset.max ? parseInt(picker.dataset.max, 10) : null;
        const hidden = picker.querySelector('.rp-value');
        const chips = picker.querySelector('.rp-chips');
        const search = picker.querySelector('.rp-search');
        const results = picker.querySelector('.rp-results');

        function ids() {
            return (hidden.value || '').split(',').map(s => s.trim()).filter(Boolean);
        }
        function setIds(list) { hidden.value = list.join(','); }

        function addChip(id, display) {
            const span = document.createElement('span');
            span.className = 'badge cms-badge-secondary me-1 rp-chip';
            span.dataset.id = id;
            span.innerHTML = '';
            span.textContent = display + ' ';
            const x = document.createElement('a');
            x.href = '#';
            x.className = 'rp-remove text-decoration-none';
            x.textContent = '×';
            span.appendChild(x);
            chips.appendChild(span);
        }

        function select(id, display) {
            id = String(id);
            const current = ids();
            if (!multiple) { chips.innerHTML = ''; setIds([]); }
            if (current.indexOf(id) !== -1) return;     // already selected
            if (multiple && maxItems !== null && current.length >= maxItems) {
                results.style.display = 'none';
                alert('You can select at most ' + maxItems + ' item(s).');
                return;
            }
            addChip(id, display);
            setIds(ids().concat(id));
            results.style.display = 'none';
            search.value = '';
        }

        chips.addEventListener('click', function (e) {
            if (!e.target.classList.contains('rp-remove')) return;
            e.preventDefault();
            const chip = e.target.closest('.rp-chip');
            const id = String(chip.dataset.id);
            chip.remove();
            setIds(ids().filter(x => x !== id));
        });

        const doSearch = debounce(function () {
            const q = search.value.trim();
            const url = searchUrl + '?q=' + encodeURIComponent(q) + '&displayField=' + encodeURIComponent(displayField)
                + (lang ? '&lang=' + encodeURIComponent(lang) : '');
            fetch(url, { headers: { 'Accept': 'application/json' } })
                .then(r => r.ok ? r.json() : [])
                .then(items => {
                    results.innerHTML = '';
                    if (!items || !items.length) { results.style.display = 'none'; return; }
                    items.forEach(it => {
                        const a = document.createElement('a');
                        a.href = '#';
                        a.className = 'list-group-item list-group-item-action py-1';
                        a.textContent = it.displayValue;
                        a.addEventListener('click', function (e) { e.preventDefault(); select(it.id, it.displayValue); });
                        results.appendChild(a);
                    });
                    results.style.display = 'block';
                })
                .catch(() => { results.style.display = 'none'; });
        }, 250);

        search.addEventListener('input', doSearch);
        search.addEventListener('focus', function () {
            if (results.children.length) {
                results.style.display = 'block';
            } else {
                doSearch();   // load all items on first open (empty query returns everything)
            }
        });
        document.addEventListener('click', function (e) {
            if (!picker.contains(e.target)) results.style.display = 'none';
        });
    }

    document.querySelectorAll('.relation-picker').forEach(init);
})();
