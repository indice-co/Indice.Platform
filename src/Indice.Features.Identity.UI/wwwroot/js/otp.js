window.addEventListener("load", function () {
    var button = document.getElementById("otp-resend");
    var countdownEls = document.querySelectorAll(".countdown");
    if (!button) return;

    // disable the button immediately
    button.disabled = true;

    var otpTimer = "2:01";

    var interval = setInterval(function () {
        // parse minutes and seconds
        var parts = otpTimer.split(':').map(function(p) { return parseInt(p, 10); });
        var minutes = parts[0], seconds = parts[1];

        // decrement
        seconds--;
        if (seconds < 0) {
            seconds = 59;
            minutes--;
        }

        // if time’s up
        if (minutes < 0) {
            clearInterval(interval);
            button.disabled = false;
            // clear all countdown displays
            countdownEls.forEach(function(el) { el.textContent = ""; });
            return;
        }

        // format back to MM:SS
        var secStr = seconds < 10 ? '0' + seconds : seconds;
        var timeStr = minutes + ':' + secStr;
        otpTimer = timeStr;

        // update every countdown span
        countdownEls.forEach(function(el) { el.textContent = timeStr; });
    }, 1000);
});
