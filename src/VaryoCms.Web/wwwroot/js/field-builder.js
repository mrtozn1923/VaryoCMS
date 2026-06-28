// Field builder: drag-to-reorder fields, persisted via PATCH .../fields/reorder.
(function () {
    "use strict";

    const list = document.getElementById("field-list");
    if (!list || typeof Sortable === "undefined") return;

    const reorderUrl = list.dataset.reorderUrl;
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    Sortable.create(list, {
        handle: ".drag-handle",
        animation: 150,
        onEnd: persistOrder
    });

    async function persistOrder() {
        const fieldIds = Array.from(list.querySelectorAll("li[data-id]"))
            .map(li => parseInt(li.dataset.id, 10));

        try {
            const res = await fetch(reorderUrl, {
                method: "PATCH",
                headers: {
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": tokenInput ? tokenInput.value : ""
                },
                body: JSON.stringify({ fieldIds: fieldIds })
            });
            if (!res.ok) {
                console.error("Reorder failed:", res.status);
                alert("Could not save the new order. Please refresh and try again.");
            }
        } catch (e) {
            console.error("Reorder error:", e);
            alert("Could not save the new order. Please refresh and try again.");
        }
    }
})();
