// Reads MFA settings from data-* attributes on the #mfa-bootstrap element and wires up the MFA view model.
// Keeps server-provided values (browser family, delivery channel, hub URL) out of inline scripts.
(function () {
    var bootstrapElement = document.getElementById('mfa-bootstrap');
    if (!bootstrapElement || !window.indice || typeof indice.mfaViewModelFactory !== 'function') {
        return;
    }
    var data = bootstrapElement.dataset;
    var mode = data.mode || 'mfa';

    $(document).ready(function () {
        var viewModelParameters = {
            $deviceIdInput: $('#Input_DeviceId'),
            $mfaForm: $('#mfa-form'),
            $mfaFormReject: $('#mfa-form-reject'),
            $otpCodeInput: $('#Input_OtpCode'),
            $rememberClientCheckbox: $('#Input_RememberClient'),
            browserFamily: data.browserFamily || '',
            deliveryChannel: data.deliveryChannel || '',
            hubConnectionUrl: data.hubConnectionUrl || ''
        };
        var viewModel = new indice.mfaViewModelFactory(viewModelParameters);
        viewModel.init();

        if (mode === 'login') {
            viewModel.calculateDeviceId();
            return;
        }

        $('button[name="ChangeSelectedMethodCode"]').on('click', function () {
            $('input[name="Input.SelectedAuthenticationMethodCode"]').val($(this).val());
            $('input[name="Input.ResendOtp"]').val('true');
        });

        var $mainPanel = $('#panel-main'),
            $chanPanel = $('#panel-channels');

        function showElement($element) {
            $element.removeClass('d-none');
            $element.css('display', '');
            $element.show();
        }

        function hideElement($element) {
            $element.hide();
            $element.css('display', 'none');
        }

        $(document).on('click', '#show-channels, #otp-resend:not([name])', function (e) {
            e.preventDefault();
            hideElement($mainPanel);
            showElement($chanPanel);
        });

        $('#go-back').on('click', function () {
            hideElement($chanPanel);
            showElement($mainPanel);
        });

        $('.channel-btn').on('click', function () {
            $('.channel-btn').removeClass('active');
            $(this).addClass('active');
        });
    });
})();
