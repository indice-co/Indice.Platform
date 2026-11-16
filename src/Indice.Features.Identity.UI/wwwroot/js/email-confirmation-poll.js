// Polls an email confirmation status endpoint while the user confirms the email link sent to them.
// Usage: <div data-email-poll-url="/path?handler=EmailStatus"></div>. The endpoint returns { confirmed, redirectUrl }.
(function () {
    'use strict';

    var element = document.querySelector('[data-email-poll-url]');
    if (!element || !window.fetch) return;

    var url = element.getAttribute('data-email-poll-url');
    var interval = parseInt(element.getAttribute('data-email-poll-interval'), 10) || 3000;
    var deadline = Date.now() + 10 * 60 * 1000;
    var timer = null;
    var busy = false;

    function schedule() {
        clearTimeout(timer);
        if (Date.now() >= deadline || document.hidden) return;
        timer = setTimeout(poll, interval);
    }

    function poll() {
        if (busy) return;
        busy = true;
        fetch(url, { credentials: 'same-origin', cache: 'no-store', headers: { 'Accept': 'application/json' } })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (result) {
                if (result && result.confirmed && result.redirectUrl) {
                    window.location.assign(result.redirectUrl);
                    return;
                }
                busy = false;
                schedule();
            })
            .catch(function () {
                busy = false;
                schedule();
            });
    }

    document.addEventListener('visibilitychange', function () {
        if (document.hidden) {
            clearTimeout(timer);
        } else if (!busy) {
            poll();
        }
    });

    schedule();
})();
