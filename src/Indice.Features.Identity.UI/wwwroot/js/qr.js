(function () {
    function render() {
        var target = document.getElementById('qrCode');
        var data = document.getElementById('qrCodeData');
        if (!target || !data || typeof window.QRCode !== 'function') {
            return;
        }
        target.innerHTML = '';
        new window.QRCode(target, {
            text: data.getAttribute('data-url'),
            width: 180,
            height: 180,
            correctLevel: window.QRCode.CorrectLevel ? window.QRCode.CorrectLevel.M : 0
        });
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', render);
    } else {
        render();
    }
})();
