window.addEventListener("load", function () {
    var button = document.getElementById("otp-resend");
    if (button) {
        var innerText = button.innerText;
        button.setAttribute('disabled', '');
        var otpTimer = "2:01";
        var interval = setInterval(function () {
            var timer = otpTimer.split(':');
            //by parsing integer, I avoid all extra string processing
            var minutes = parseInt(timer[0], 10);
            var seconds = parseInt(timer[1], 10);
            --seconds;
            minutes = (seconds < 0) ? --minutes : minutes;
            if (minutes < 0) {
                clearInterval(interval);
                minutes = 0;
                button.removeAttribute('disabled');
                button.innerText = innerText;
                return;
                // Make Ajax call to controller.
            }
            seconds = (seconds < 0) ? 59 : seconds;
            seconds = (seconds < 10) ? '0' + seconds : seconds;
            //minutes = (minutes < 10) ? minutes : minutes;
            $('.countdown').html(minutes + ':' + seconds);
            otpTimer = minutes + ':' + seconds;
            button.innerText = `${innerText} ${otpTimer}`;
        }, 1000);
    }
});