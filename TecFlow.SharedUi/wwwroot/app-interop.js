window.tecFlowAppInterop = {
    copyToClipboard: async function (text) {
        if (!text) {
            return false;
        }

        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch {
            // fallback abaixo
        }

        try {
            const area = document.createElement("textarea");
            area.value = text;
            area.setAttribute("readonly", "");
            area.style.position = "fixed";
            area.style.left = "-9999px";
            document.body.appendChild(area);
            area.select();
            const ok = document.execCommand("copy");
            document.body.removeChild(area);
            return ok;
        } catch {
            return false;
        }
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

        const message = encodeURIComponent(
            [payload.text, payload.url].filter(Boolean).join("\n")
        );
        const whatsAppUrl = "https://wa.me/?text=" + message;
        window.open(whatsAppUrl, "_blank", "noopener,noreferrer");
        return { success: true, method: "whatsapp" };
    },

    scrollIntoViewById: function (elementId) {
        const element = document.getElementById(elementId);
        if (!element) {
            return false;
        }

        element.scrollIntoView({ behavior: "smooth", block: "nearest" });
        return true;
    }
};
