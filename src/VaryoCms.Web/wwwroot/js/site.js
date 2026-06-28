// Varyo CMS admin shell — vanilla JS only (no jQuery).

(function () {
    "use strict";

    // Mobile sidebar toggle
    const sidebar = document.getElementById("cms-sidebar");
    const toggle = document.getElementById("cms-sidebar-toggle");
    const backdrop = document.getElementById("cms-sidebar-backdrop");

    function openSidebar() {
        sidebar?.classList.add("open");
        backdrop?.classList.add("show");
    }
    function closeSidebar() {
        sidebar?.classList.remove("open");
        backdrop?.classList.remove("show");
    }

    toggle?.addEventListener("click", function () {
        sidebar?.classList.contains("open") ? closeSidebar() : openSidebar();
    });
    backdrop?.addEventListener("click", closeSidebar);

    // Auto-generate kebab-case slug from the Name field.
    // Applies to any form that has both an input[name="Name"] and a .slug-field.
    // Stops auto-filling once the user manually edits the slug.
    (function () {
        var TR_MAP = { 'ğ':'g','ü':'u','ş':'s','ı':'i','ö':'o','ç':'c',
                       'Ğ':'g','Ü':'u','Ş':'s','İ':'i','Ö':'o','Ç':'c' };
        function toSlug(str) {
            return str
                .replace(/[ğüşıöçĞÜŞİÖÇ]/g, function(c) { return TR_MAP[c] || c; })
                .toLowerCase()
                .replace(/[^a-z0-9\s-]/g, '')
                .trim()
                .replace(/[\s]+/g, '-')
                .replace(/-+/g, '-');
        }
        // Wire both content-type Name fields and content-item Title fields to auto-generate the slug.
        document.querySelectorAll('input[name="Name"], .title-source').forEach(function (nameInput) {
            var form = nameInput.closest('form');
            if (!form) return;
            var slugInput = form.querySelector('.slug-field');
            if (!slugInput) return;
            var manuallyEdited = slugInput.value !== '';
            slugInput.addEventListener('input', function () { manuallyEdited = true; });
            nameInput.addEventListener('input', function () {
                if (!manuallyEdited) slugInput.value = toSlug(nameInput.value);
            });
        });
    }());

    // Collapsible nav groups — persist open/closed state per group in localStorage.
    document.querySelectorAll(".cms-nav-group").forEach(function (group) {
        var id = group.dataset.groupId;
        var isOpen = localStorage.getItem("cms-nav-" + id) !== "false";
        if (!isOpen) group.classList.add("collapsed");

        var btn = group.querySelector(".cms-nav-group-toggle");
        btn && btn.addEventListener("click", function () {
            group.classList.toggle("collapsed");
            localStorage.setItem("cms-nav-" + id, !group.classList.contains("collapsed"));
        });
    });

    // Mark the active sidebar item: the one whose href is the longest prefix of the current path.
    const path = window.location.pathname.toLowerCase();
    let best = null, bestLen = -1;
    document.querySelectorAll(".cms-nav-item").forEach(function (link) {
        const href = (link.getAttribute("href") || "").toLowerCase();
        if (href && href !== "/" && path.startsWith(href) && href.length > bestLen) {
            best = link;
            bestLen = href.length;
        }
    });
    best?.classList.add("active");

    // Ensure the parent group of the active item is expanded.
    if (best) {
        var parentGroup = best.closest(".cms-nav-group");
        if (parentGroup) {
            parentGroup.classList.remove("collapsed");
            localStorage.setItem("cms-nav-" + parentGroup.dataset.groupId, "true");
        }
    }
})();
