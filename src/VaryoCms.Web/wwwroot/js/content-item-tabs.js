// Content item form — client-side language tabs. All active languages' inputs are already in the
// DOM at page load (ContentItemFormFactory loads every language up front); switching tabs never
// reloads the page, so unsaved input in the outgoing language is never lost (the bug this replaces:
// the old tabs were plain <a> links that did a full GET, discarding anything not yet saved).

(function () {
    "use strict";

    const tabs = document.querySelectorAll("[data-lang-tab]");
    const panels = document.querySelectorAll("[data-lang-panel]");
    if (tabs.length === 0 || panels.length === 0) return;

    // A `required` input inside a display:none panel blocks native form submission ("An invalid
    // form control is not focusable") even though the user can't see or fix it. Only the active
    // panel's required inputs may carry the attribute; move it back when a panel is (re)activated.
    function syncRequired(panel, isActive) {
        panel.querySelectorAll("[data-required-field]").forEach(function (el) {
            if (isActive) {
                el.setAttribute("required", "required");
            } else {
                el.removeAttribute("required");
            }
        });
    }

    // Repaint TinyMCE editors the first time their panel becomes visible — editors initialised
    // while hidden (display:none has no layout) can render at zero height.
    function repaintEditors(panel) {
        if (typeof tinymce === "undefined") return;
        panel.querySelectorAll("textarea.cms-richtext").forEach(function (el) {
            const editor = tinymce.get(el.id);
            if (!editor) return;
            try { editor.execCommand("mceRepaint"); } catch (e) { /* best-effort */ }
        });
    }

    function activate(lang) {
        tabs.forEach(function (tab) {
            tab.classList.toggle("active", tab.dataset.langTab === lang);
        });
        panels.forEach(function (panel) {
            const isActive = panel.dataset.langPanel === lang;
            panel.classList.toggle("d-none", !isActive);
            syncRequired(panel, isActive);
            if (isActive) repaintEditors(panel);
        });
    }

    // Mark every currently-required input so syncRequired can restore it later, then strip
    // `required` from every non-active panel up front (mirrors the state `activate` would set).
    panels.forEach(function (panel) {
        panel.querySelectorAll("[required]").forEach(function (el) {
            el.setAttribute("data-required-field", "true");
        });
    });
    tabs.forEach(function (tab) {
        tab.addEventListener("click", function () { activate(tab.dataset.langTab); });
    });

    const initialTab = document.querySelector("[data-lang-tab].active");
    panels.forEach(function (panel) {
        syncRequired(panel, !panel.classList.contains("d-none"));
    });
    if (initialTab) repaintEditors(document.querySelector('[data-lang-panel="' + initialTab.dataset.langTab + '"]'));
})();
