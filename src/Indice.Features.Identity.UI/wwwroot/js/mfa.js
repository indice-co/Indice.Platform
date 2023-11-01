var indice = indice || {};

(function () {
    indice.mfaViewModelFactory = function (viewModelParams) {
        var self;
        var host = window.location.protocol + '//' + window.location.host;
        return {
            self: undefined,
            browserId: undefined,
            init: function () {
                self = this;
                if (viewModelParams.deliveryChannel === 'PushNotification') {
                    self.startSignalRConnection();
                }
                if (viewModelParams.$rememberClientCheckbox) {
                    viewModelParams.$rememberClientCheckbox.change(function () {
                        if (!self.browserId && this.checked) {
                            self.calculateDeviceId();
                        }
                    });
                }
            },
            calculateDeviceId: function () {
                var fpPromise = FingerprintJS.load();
                fpPromise.then(fp => fp.get()).then(result => {
                    self.browserId = result.visitorId;
                    viewModelParams.$deviceIdInput.val(self.browserId + '.' + viewModelParams.browserFamily);
                });
            },
            onConnected: function (connection) {
                console.log('SignalR connection id for MFA approval: ' + connection.connectionId);
                self.sendPushNotification(connection.connectionId);
            },
            sendPushNotification: function (connectionId) {
                var requestToken = $("[name='__RequestVerificationToken']").val();
                $.ajax({
                    url: host + '/login/mfa/notify',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        connectionId: connectionId
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
                var connection = new signalR
                    .HubConnectionBuilder()
                    .withUrl(viewModelParams.hubConnectionUrl)
                    .build();
                connection.on('LoginApproved', function (otpCode) {
                    viewModelParams.$otpCodeInput.val(otpCode);
                    viewModelParams.$mfaForm.submit();
                });
                connection.on('LoginRejected', function () {
                    viewModelParams.$mfaForm.addClass('d-none');
                    viewModelParams.$mfaFormReject.removeClass('d-none');
                });
                connection.start()
                    .then(() => self.onConnected(connection))
                    .catch(error => console.error(error.message));
            }
        }
    }
})();
