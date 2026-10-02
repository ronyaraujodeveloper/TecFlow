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

    setPreference: function (key, value) {
        try {
            window.localStorage.setItem(key || "", value == null ? "" : String(value));
            return true;
        } catch (error) {
            return false;
        }
    }
};
