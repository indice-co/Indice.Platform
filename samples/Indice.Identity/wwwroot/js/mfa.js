$(document).ready(function () {
    var host = window.location.protocol + '//' + window.location.host;

    function onConnected(connection) {
        console.debug('SignalR connection started.');
        $.ajax({
            url: host + '/login/mfa/notify',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                connectionId: connection.connectionId
            }),
            dataType: 'json',
            success: function (data, textStatus, jqXHR) { },
            error: function (jqXHR, textStatus, errorThrown) { }
        });
    }

    const connection = new signalR
        .HubConnectionBuilder()
        .withUrl('/mfa')
        .build();

    connection.start()
        .then(() => onConnected(connection))
        .catch(error => console.error(error.message));
});