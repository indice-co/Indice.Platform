(function () {
    var data = document.getElementById('recovery-codes-data');
    var btn = document.getElementById('download-recovery-codes');
    if (!data || !btn) {
        return;
    }
    btn.addEventListener('click', function () {
        var codesAttr = data.getAttribute('data-codes') || '[]';
        var headerTemplate = data.getAttribute('data-header') || '';
        var email = data.getAttribute('data-email') || '';
        var codes;
        try {
            codes = JSON.parse(codesAttr);
        } catch (e) {
            codes = [];
        }
        if (!codes.length) {
            return;
        }
        var header = headerTemplate.replace('{0}', email);
        var content = header + '\r\n\r\n' + codes.join('\r\n') + '\r\n';
        var blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
        var url = URL.createObjectURL(blob);
        var anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = 'recovery-codes.txt';
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        URL.revokeObjectURL(url);
    });
})();
