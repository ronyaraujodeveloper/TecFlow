window.ofertioActiveStore = window.ofertioActiveStore || {
    get: function () {
        try {
            return window.localStorage.getItem("ofertio.activeStoreId");
        } catch (e) {
            return null;
        }
    },
    set: function (value) {
        try {
            window.localStorage.setItem("ofertio.activeStoreId", value);
        } catch (e) {
            // Ignorado em contextos restritos (modo privado, etc.).
        }
    },
    clear: function () {
        try {
            window.localStorage.removeItem("ofertio.activeStoreId");
        } catch (e) {
            // Ignorado.
        }
    }
};
