var indice = indice || {};

(function () {
    indice.PasswordRulesViewModelFactory = function (viewModelParams) {
        var self;

        function updateFormValidity() {
            var formIsValid = $(viewModelParams.form)
                .validate()
                .checkForm();

            var passwordRulesAreValid = self.passwordRules()
                .every(function (rule) {
                    return rule.isValid;
                });

            self.isFormValid(formIsValid && passwordRulesAreValid);
        }

        return {
            self: undefined,
            init: function () {
                self = this;
            },
            passwordChanged: indice.utilities.debounce(function (viewModel, event) {
                var password = event.currentTarget.value;
                var request = {
                    token: viewModelParams.userId,
                    password: password,
                    userName: viewModelParams.userName || viewModelParams.userNameInputSelector.val()
                };
                $.ajax({
                    url: '/api/account/validate-password',
                    type: 'post',
                    contentType: 'application/json',
                    dataType: 'json',
                    data: JSON.stringify(request),
                    success: function (data) {
                        self.passwordRules(data.passwordRules);
                        updateFormValidity();
                    },
                    error: function () {
                        self.passwordRules([]);
                        updateFormValidity();
                    }
                });
            }, 500),
            formChanged: function () {
                updateFormValidity();
            },
            passwordRules: ko.observableArray([]),
            isFormValid: ko.observable(false)
        };
    };
})();

$(document).ready(function () {
    var form = document.getElementsByTagName('form')[0];
    var viewModelParameters = {
        form: form,
        userId: form.getAttribute('data-token'),
        userName: form.getAttribute('data-userName'),
        userNameInputSelector: $('#Input_UserName')
    };
    var viewModel = new indice.PasswordRulesViewModelFactory(viewModelParameters);
    viewModel.init();
    ko.bindingProvider.instance = new ko.secureBindingsProvider();
    ko.applyBindings(viewModel);
});
