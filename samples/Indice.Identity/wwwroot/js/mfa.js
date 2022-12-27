var indice = indice || {};

(function () {
    indice.mfaViewModelFactory = function (viewModelParams) {
        var self;
        var host = window.location.protocol + '//' + window.location.host;
        return {
            self: undefined,
            init: function () {
                self = this;
                debugger
                if (viewModelParams.deliveryChannel === 'PushNotification') {
                    self.startSignalRConnection();
                }
            },
            onConnected: function (connection) {
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
            },
            startSignalRConnection: function () {
                console.debug('SignalR connection starting.');
                const connection = new signalR
                    .HubConnectionBuilder()
                    .withUrl('/mfa')
                    .build();
                connection.on('LoginApproved', function (otpCode) {
                    console.debug('Login approved by user.');
                    viewModelParams.$otpCodeField.val(otpCode);
                    viewModelParams.$mfaForm.submit();
                });
                connection.on('LoginRejected', function () {
                    console.debug('Login rejected by user.');
                });
                connection.start()
                    .then(() => onConnected(connection))
                    .catch(error => console.error(error.message));
            }
        };
    };
})();
