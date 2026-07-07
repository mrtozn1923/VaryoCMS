// richtext-editor.js — TinyMCE WYSIWYG init for RichText fields
(function () {
    'use strict';

    function initEditors() {
        document.querySelectorAll('textarea.cms-richtext').forEach(function (el) {
            if (el.dataset.tinymceInitialized) return;
            el.dataset.tinymceInitialized = 'true';

            tinymce.init({
                target: el,
                height: 420,
                menubar: false,
                plugins: [
                    'advlist', 'autolink', 'lists', 'link', 'image',
                    'charmap', 'preview', 'anchor', 'searchreplace',
                    'visualblocks', 'code', 'fullscreen', 'insertdatetime',
                    'media', 'table', 'help', 'wordcount'
                ],
                toolbar:
                    'undo redo | blocks | ' +
                    'bold italic underline strikethrough | ' +
                    'bullist numlist | outdent indent | ' +
                    'link image | code fullscreen | removeformat help',
                // "Source" field's Browse button in the Image dialog opens the CMS's own
                // media library (media-picker.js) instead of a bare URL/upload prompt, so
                // inserted images stay tracked media_assets rows, not orphaned uploads.
                file_picker_types: 'image',
                file_picker_callback: function (callback, value, meta) {
                    if (meta.filetype !== 'image' || !window.VaryoMediaPicker) return;
                    window.VaryoMediaPicker.openForCallback({
                        mediaType: 'image',
                        onSelect: function (m) {
                            callback(m.url, { alt: m.originalName || '' });
                        }
                    });
                },
                // TinyMCE's default relative_urls:true rewrites any URL we insert (even an
                // absolute one, e.g. from MediaSettings.PublicBaseUrl) to be relative to this
                // edit page's own path (../../../../../uploads/...) — broken as soon as the
                // saved HTML is rendered anywhere but this exact admin URL (public site, API
                // consumers). Keep whatever URL the media picker hands back untouched.
                relative_urls: false,
                remove_script_host: false,
                content_style: 'body { font-family: Inter, -apple-system, sans-serif; font-size: 14px; line-height: 1.6; }',
                skin: 'oxide-dark',
                content_css: 'dark',
                promotion: false,
                branding: false,
                setup: function (editor) {
                    // Sync editor content back to the hidden textarea on every change
                    // so the form POST picks up the latest value.
                    editor.on('change input', function () {
                        editor.save();
                    });
                }
            });
        });
    }

    if (typeof tinymce !== 'undefined') {
        initEditors();
    } else {
        document.addEventListener('DOMContentLoaded', initEditors);
    }
})();
