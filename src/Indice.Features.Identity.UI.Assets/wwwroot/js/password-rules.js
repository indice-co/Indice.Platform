var indice = indice || {};

(function () {
    indice.PasswordRulesViewModelFactory = function (viewModelParams) {
        var self;
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
                    success: function (data, textStatus, jqXHR) {
                        self.passwordRules([]);
                        self.passwordRules(data.passwordRules);
                    },
                    error: function (jqXHR, textStatus, errorThrown) { }
                });
            }, 500),
            formChanged: function (viewModel, event) {
                self.isFormValid($(event.currentTarget).validate().checkForm() && self.passwordRules().every(function ruleIsValid(rule) { return rule.isValid; }));
            },
            passwordRules: ko.observableArray([]),
            isFormValid: ko.observable(false)
        };
    };
})();

$(document).ready(function () {
    var form = document.getElementsByTagName('form')[0];
    var viewModelParameters = {
        userId: form.getAttribute('data-token'),
        userName: form.getAttribute('data-userName'),
        userNameInputSelector: $('#Input_UserName')
    };
    debugger
    var viewModel = new indice.PasswordRulesViewModelFactory(viewModelParameters);
    viewModel.init();
    ko.bindingProvider.instance = new ko.secureBindingsProvider();
    ko.applyBindings(viewModel);
});
