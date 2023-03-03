var indice = indice || {};

(function () {
    indice.utilities = indice.utilities || {};

    indice.utilities.debounce = function (callback, ms, immediate) {
        var timeout;
        return function () {
            var context = this, args = arguments;
            var later = function () {
                timeout = null;
                if (!immediate) {
                    callback.apply(context, args);
                }
            };
            var callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, ms);
            if (callNow) {
                callback.apply(context, args);
            }
        };
    };
})();