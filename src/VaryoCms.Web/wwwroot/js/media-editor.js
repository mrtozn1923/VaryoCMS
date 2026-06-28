// Image crop UI: Cropper.js modal → POST /admin/media/{id}/crop
// Rename UI: details modal → POST /admin/media/{id}/meta (AJAX, updates card in-place)
(function () {
    function getCsrf() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    if (typeof bootstrap === 'undefined') return;

    // ==================== RENAME ====================
    var renameModalEl = document.getElementById('renameModal');
    if (!renameModalEl) return;

    var renameModal   = new bootstrap.Modal(renameModalEl);
    var renameBase    = document.getElementById('rename-base');
    var renameExt     = document.getElementById('rename-ext');
    var renameAlt     = document.getElementById('rename-alt');
    var renameAltRow  = document.getElementById('rename-alt-row');
    var renameErr     = document.getElementById('rename-error');
    var renameSaveBtn = document.getElementById('rename-save-btn');
    var renameDimsRow = document.getElementById('rename-dims-row');
    var currentId     = null;

    document.querySelectorAll('.rename-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            currentId = btn.dataset.id;
            renameBase.value = btn.dataset.base || '';
            renameExt.textContent = btn.dataset.ext || '';
            renameAlt.value = btn.dataset.alt || '';
            renameErr.classList.add('d-none');
            renameErr.textContent = '';

            renameAltRow.style.display = btn.dataset.type === 'image' ? '' : 'none';

            document.getElementById('rename-info-type').textContent = btn.dataset.type || '';
            document.getElementById('rename-info-size').textContent = btn.dataset.size || '—';
            document.getElementById('rename-info-date').textContent = btn.dataset.date || '';

            var dims = btn.dataset.dims || '';
            document.getElementById('rename-info-dims').textContent = dims || '—';
            renameDimsRow.style.display = dims ? '' : 'none';

            renameModal.show();
            setTimeout(function () { renameBase.focus(); renameBase.select(); }, 300);
        });
    });

    renameSaveBtn.addEventListener('click', async function () {
        if (!currentId) return;
        var baseName = renameBase.value.trim();
        if (!baseName) { showErr('Ad boş olamaz.'); return; }

        renameSaveBtn.disabled = true;
        renameErr.classList.add('d-none');

        var fd = new FormData();
        fd.append('__RequestVerificationToken', getCsrf());
        fd.append('baseName', baseName);
        fd.append('altText', renameAlt.value.trim());

        try {
            var resp = await fetch('/admin/media/' + currentId + '/meta', { method: 'POST', body: fd });
            if (resp.ok) {
                var newName = baseName + renameExt.textContent;
                var nameEl  = document.getElementById('media-name-' + currentId);
                if (nameEl) { nameEl.textContent = newName; nameEl.title = newName; }
                renameModal.hide();
            } else {
                var msg = await resp.text();
                showErr(msg || 'Güncelleme başarısız.');
            }
        } catch (_) {
            showErr('Bağlantı hatası.');
        } finally {
            renameSaveBtn.disabled = false;
        }
    });

    renameBase.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); renameSaveBtn.click(); }
    });

    function showErr(msg) {
        renameErr.textContent = msg;
        renameErr.classList.remove('d-none');
    }

    // ==================== CROP ====================
    var cropModalEl = document.getElementById('cropModal');
    if (!cropModalEl || typeof Cropper === 'undefined') return;

    var img       = document.getElementById('cropImage');
    var form      = document.getElementById('cropForm');
    var cropModal = new bootstrap.Modal(cropModalEl);
    var cropper   = null;

    document.querySelectorAll('.crop-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            form.setAttribute('action', '/admin/media/' + btn.dataset.id + '/crop');
            img.src = btn.dataset.url;
            cropModal.show();
        });
    });

    cropModalEl.addEventListener('shown.bs.modal', function () {
        if (cropper) cropper.destroy();
        cropper = new Cropper(img, { viewMode: 1, autoCropArea: 0.8, background: false });
    });

    cropModalEl.addEventListener('hidden.bs.modal', function () {
        if (cropper) { cropper.destroy(); cropper = null; }
    });

    form.addEventListener('submit', function (e) {
        if (!cropper) { e.preventDefault(); return; }
        var d = cropper.getData(true);
        document.getElementById('cropX').value = Math.max(0, d.x);
        document.getElementById('cropY').value = Math.max(0, d.y);
        document.getElementById('cropW').value = Math.max(1, d.width);
        document.getElementById('cropH').value = Math.max(1, d.height);
    });
})();
