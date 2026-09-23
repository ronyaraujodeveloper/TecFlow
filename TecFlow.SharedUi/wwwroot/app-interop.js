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
    }
};
