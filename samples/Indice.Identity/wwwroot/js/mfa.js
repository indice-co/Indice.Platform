$(document).ready(function () {
    var host = window.location.protocol + '//' + window.location.host;
    var $otpCodeField = $('#OtpCode');
    var $mfaForm = $('#mfa-form');

    function onConnected(connection) {
        console.debug('SignalR connection started.');
        var requestToken = $("[name='__RequestVerificationToken']").val();
        $.ajax({
            url: host + '/login/mfa/notify',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                connectionId: connection.connectionId
            }),
            headers: {
                'X-XSRF-TOKEN': requestToken
            },
            dataType: 'json',
            success: function (data, textStatus, jqXHR) { },
            error: function (jqXHR, textStatus, errorThrown) { }
        });
    }

    const connection = new signalR
        .HubConnectionBuilder()
        .withUrl('/mfa')
        .build();

    connection.on('LoginApproved', function (otpCode) {
        console.debug('Login approved by user.');
        $otpCodeField.val(otpCode);
        $mfaForm.submit();
    });

    connection.on('LoginRejected', function () {
        console.debug('Login rejected by user.');
    });

    connection.start()
        .then(() => onConnected(connection))
        .catch(error => console.error(error.message));
});