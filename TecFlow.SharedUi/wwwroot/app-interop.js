window.tecFlowAppInterop = {
    copyToClipboard: async function (text) {
        if (window.tecFlowClipboard && typeof window.tecFlowClipboard.copyText === "function") {
            return window.tecFlowClipboard.copyText(text);
        }

        return false;
    },

    shareLink: async function (title, text, url) {
        const payload = {
            title: title || "TecFlow",
            text: text || "",
            url: url || ""
        };

        if (navigator.share) {
            try {
                await navigator.share(payload);
                return { success: true, method: "native" };
            } catch (error) {
                if (error && error.name === "AbortError") {
                    return { success: false, cancelled: true, method: "native" };
                }
            }
        }

        return { success: false, cancelled: false, method: "fallback" };
    },

    scrollToTop: function () {
        window.scrollTo({ top: 0, left: 0, behavior: "smooth" });
        return true;
    },

    resetScroll: function () {
        window.scrollTo({ top: 0, left: 0, behavior: "instant" });
        document.querySelectorAll(".main-content").forEach(function (element) {
            element.scrollTop = 0;
        });
        return true;
    },

    scrollIntoViewById: function (elementId, block) {
        const element = document.getElementById(elementId);
        if (!element) {
            return false;
        }

        element.scrollIntoView({
            behavior: "smooth",
            block: block || "nearest"
        });
        return true;
    },

    getInputValueById: function (elementId) {
        const element = document.getElementById(elementId);
        if (!element || typeof element.value !== "string") {
            return "";
        }

        return element.value;
    },

    insertAtCursor: function (elementId, text) {
        const element = document.getElementById(elementId);
        const snippet = text || "";
        if (!element || typeof element.value !== "string") {
            return snippet;
        }

        const start = typeof element.selectionStart === "number" ? element.selectionStart : element.value.length;
        const end = typeof element.selectionEnd === "number" ? element.selectionEnd : start;
        const next = element.value.slice(0, start) + snippet + element.value.slice(end);
        element.value = next;
        const caret = start + snippet.length;
        if (typeof element.setSelectionRange === "function") {
            element.focus();
            element.setSelectionRange(caret, caret);
        }

        return next;
    },

    getPreference: function (key) {
        try {
            return window.localStorage.getItem(key || "") || "";
        } catch (error) {
            return "";
        }
    },

    toggleSidebar: function () {
        const sidebar = document.getElementById("portal-sidebar");
        const backdrop = document.getElementById("portal-sidebar-backdrop");
        if (!sidebar) {
            return false;
        }

        const open = sidebar.classList.toggle("is-open");
        if (backdrop) {
            backdrop.classList.toggle("is-visible", open);
        }

        return open;
    },

    closeSidebar: function () {
        const sidebar = document.getElementById("portal-sidebar");
        const backdrop = document.getElementById("portal-sidebar-backdrop");
        if (sidebar) {
            sidebar.classList.remove("is-open");
        }

        if (backdrop) {
            backdrop.classList.remove("is-visible");
        }

        return true;
    },

    setPreference: function (key, value) {
        try {
            window.localStorage.setItem(key || "", value == null ? "" : String(value));
            return true;
        } catch (error) {
            return false;
        }
    }
};

window.scrollToTop = window.tecFlowAppInterop.scrollToTop;

/* Drag & drop da matriz de menus (/master/menu-permissions): linhas com data-menu-row.
   Faixa superior = antes, inferior = depois, centro = dentro (pasta) ou nova pasta (item). */
window.tecFlowMenuDnd = (function () {
    const zoneClasses = ["menu-drop-before", "menu-drop-after", "menu-drop-into"];
    let lastZone = "after";

    const rowOf = function (target) {
        return target && typeof target.closest === "function" ? target.closest("[data-menu-row]") : null;
    };

    const clearZones = function () {
        document.querySelectorAll("[data-menu-row][data-zone]").forEach(function (row) {
            row.classList.remove.apply(row.classList, zoneClasses);
            delete row.dataset.zone;
        });
    };

    const zoneFor = function (row, clientY) {
        const rect = row.getBoundingClientRect();
        const ratio = (clientY - rect.top) / Math.max(rect.height, 1);
        if (ratio < 0.3) {
            return "before";
        }

        return ratio > 0.7 ? "after" : "into";
    };

    const endDrag = function () {
        clearZones();
        document.querySelectorAll("[data-menu-row].menu-dragging").forEach(function (row) {
            row.classList.remove("menu-dragging");
        });
    };

    document.addEventListener("dragstart", function (event) {
        const row = rowOf(event.target);
        if (!row || row.getAttribute("draggable") !== "true") {
            return;
        }

        event.dataTransfer.effectAllowed = "move";
        try {
            event.dataTransfer.setData("text/plain", row.dataset.menuRow || "");
        } catch (error) {
            /* alguns navegadores bloqueiam setData fora de contexto seguro */
        }
        row.classList.add("menu-dragging");
    });

    document.addEventListener("dragover", function (event) {
        const row = rowOf(event.target);
        if (!row || !document.querySelector("[data-menu-row].menu-dragging") || row.classList.contains("menu-dragging")) {
            return;
        }

        event.preventDefault();
        event.dataTransfer.dropEffect = "move";
        const zone = zoneFor(row, event.clientY);
        lastZone = zone;
        if (row.dataset.zone !== zone) {
            clearZones();
            row.classList.add("menu-drop-" + zone);
            row.dataset.zone = zone;
        }
    });

    document.addEventListener("dragleave", function (event) {
        const row = rowOf(event.target);
        if (row && !row.contains(event.relatedTarget)) {
            row.classList.remove.apply(row.classList, zoneClasses);
            delete row.dataset.zone;
        }
    });

    document.addEventListener("drop", function (event) {
        if (rowOf(event.target)) {
            event.preventDefault();
            window.setTimeout(endDrag, 0);
        }
    });

    document.addEventListener("dragend", endDrag);

    return {
        zone: function () {
            return lastZone;
        }
    };
})();

(function registerProfileMenuDismiss() {
    const closeAll = function () {
        document.querySelectorAll("details[data-profile-menu][open]").forEach(function (menu) {
            menu.removeAttribute("open");
        });
    };

    document.addEventListener("click", function (event) {
        const menu = event.target.closest ? event.target.closest("details[data-profile-menu]") : null;
        if (!menu) {
            closeAll();
            return;
        }

        if (event.target.closest("a")) {
            menu.removeAttribute("open");
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeAll();
        }
    });
})();

(function registerEnhancedNavigationScrollReset() {
    let lastPath = window.location.pathname;
    const attach = function () {
        if (!window.Blazor || typeof window.Blazor.addEventListener !== "function") {
            return false;
        }

        window.Blazor.addEventListener("enhancedload", function () {
            if (window.location.pathname !== lastPath && !window.location.hash) {
                window.tecFlowAppInterop.resetScroll();
            }

            lastPath = window.location.pathname;
        });
        return true;
    };

    if (!attach()) {
        document.addEventListener("DOMContentLoaded", attach);
    }
})();
