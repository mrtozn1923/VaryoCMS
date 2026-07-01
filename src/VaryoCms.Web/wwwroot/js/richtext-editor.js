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
