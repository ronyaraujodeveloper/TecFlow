window.tecflowActiveStore = window.tecflowActiveStore || {
    get: function () {
        try {
            return window.localStorage.getItem("tecflow.activeStoreId");
        } catch (e) {
            return null;
        }
    },
    set: function (value) {
        try {
            window.localStorage.setItem("tecflow.activeStoreId", value);
        } catch (e) {
            // Ignorado em contextos restritos (modo privado, etc.).
        }
    },
    clear: function () {
        try {
            window.localStorage.removeItem("tecflow.activeStoreId");
        } catch (e) {
            // Ignorado.
        }
    }
};
