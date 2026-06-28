// Searchable Bootstrap Icons picker (local font, no external redirect).
// Stores the chosen icon class (e.g. "bi-file-text") in a hidden input.
// Curated CMS-oriented icon set — extend ICONS below to expose more glyphs.
(function () {
    const ICONS = [
        'file-text', 'file-earmark', 'file-earmark-text', 'files', 'folder', 'folder2-open',
        'journal-text', 'journals', 'book', 'bookmark', 'card-text', 'card-list', 'card-heading',
        'collection', 'collection-fill', 'grid', 'grid-1x2', 'grid-3x3-gap', 'list-ul', 'list-task',
        'kanban', 'table', 'layout-text-window', 'layout-sidebar', 'columns-gap',
        'tag', 'tags', 'bookmarks', 'hash', 'paperclip', 'pin-angle', 'flag',
        'calendar', 'calendar-event', 'calendar3', 'clock', 'clock-history', 'alarm',
        'image', 'images', 'camera', 'film', 'camera-video', 'music-note-beamed', 'mic',
        'play-circle', 'collection-play', 'easel', 'palette', 'brush', 'vector-pen',
        'person', 'people', 'person-badge', 'person-circle', 'person-vcard', 'people-fill',
        'chat', 'chat-dots', 'chat-quote', 'envelope', 'envelope-paper', 'megaphone', 'bell',
        'star', 'star-fill', 'heart', 'hand-thumbs-up', 'award', 'trophy', 'patch-check',
        'globe', 'globe2', 'translate', 'geo-alt', 'map', 'compass', 'pin-map',
        'graph-up', 'bar-chart', 'pie-chart', 'speedometer2', 'activity', 'clipboard-data',
        'box', 'box-seam', 'boxes', 'archive', 'bag', 'cart', 'basket', 'shop', 'truck',
        'tag-fill', 'cash', 'cash-stack', 'credit-card', 'currency-dollar', 'wallet2', 'receipt',
        'gear', 'gear-wide-connected', 'sliders', 'tools', 'wrench', 'toggles', 'ui-checks',
        'key', 'shield-lock', 'shield-check', 'lock', 'unlock', 'fingerprint', 'incognito',
        'code-slash', 'terminal', 'braces', 'bug', 'cpu', 'hdd-stack', 'database', 'diagram-3',
        'link-45deg', 'box-arrow-up-right', 'cloud', 'cloud-upload', 'cloud-download', 'rss',
        'house', 'building', 'buildings', 'shop-window', 'briefcase', 'mortarboard',
        'lightbulb', 'puzzle', 'rocket-takeoff', 'magic', 'stars', 'gem', 'fire',
        'question-circle', 'info-circle', 'exclamation-triangle', 'check-circle', 'x-circle',
        'search', 'funnel', 'sort-down', 'arrow-repeat', 'eye', 'pencil', 'trash', 'plus-circle'
    ];

    function debounce(fn, ms) {
        let t;
        return function (...a) { clearTimeout(t); t = setTimeout(() => fn.apply(this, a), ms); };
    }

    function init(picker) {
        const hidden = picker.querySelector('.ip-value');
        const trigger = picker.querySelector('.ip-trigger');
        const triggerIcon = picker.querySelector('.ip-trigger-icon');
        const triggerLabel = picker.querySelector('.ip-trigger-label');
        const popover = picker.querySelector('.ip-popover');
        const search = picker.querySelector('.ip-search');
        const grid = picker.querySelector('.ip-grid');
        const clearBtn = picker.querySelector('.ip-clear');
        const placeholder = picker.dataset.placeholder || 'bi-collection';

        function renderTrigger() {
            const cls = (hidden.value || '').trim();
            if (cls) {
                triggerIcon.className = 'bi ' + cls + ' ip-trigger-icon';
                triggerLabel.textContent = cls;
                triggerLabel.classList.remove('text-muted');
                clearBtn.hidden = false;
            } else {
                triggerIcon.className = 'bi bi-collection ip-trigger-icon';
                triggerLabel.textContent = placeholder;
                triggerLabel.classList.add('text-muted');
                clearBtn.hidden = true;
            }
        }

        function renderGrid(filter) {
            const term = (filter || '').trim().toLowerCase();
            const selected = (hidden.value || '').trim();
            grid.innerHTML = '';
            const matches = term
                ? ICONS.filter(n => n.includes(term))
                : ICONS;
            if (matches.length === 0) {
                const empty = document.createElement('div');
                empty.className = 'ip-empty text-muted small p-2';
                empty.textContent = '—';
                grid.appendChild(empty);
                return;
            }
            matches.forEach(name => {
                const cls = 'bi-' + name;
                const cell = document.createElement('button');
                cell.type = 'button';
                cell.className = 'ip-cell' + (cls === selected ? ' is-selected' : '');
                cell.title = cls;
                cell.innerHTML = '<i class="bi ' + cls + '"></i>';
                cell.addEventListener('click', () => {
                    hidden.value = cls;
                    renderTrigger();
                    close();
                });
                grid.appendChild(cell);
            });
        }

        function open() {
            renderGrid('');
            search.value = '';
            popover.hidden = false;
            search.focus();
        }
        function close() { popover.hidden = true; }

        trigger.addEventListener('click', () => { popover.hidden ? open() : close(); });
        clearBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            hidden.value = '';
            renderTrigger();
        });
        search.addEventListener('input', debounce(() => renderGrid(search.value), 120));
        document.addEventListener('click', (e) => {
            if (!picker.contains(e.target)) close();
        });
        picker.addEventListener('keydown', (e) => { if (e.key === 'Escape') close(); });

        renderTrigger();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-icon-picker]').forEach(init);
    });
})();
