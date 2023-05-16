var indice = indice || {};

(function () {
    indice.MfaOnboardingViewModelFactory = function (viewModelParams) {
        return {
            self: undefined,
            init: function () {
                self = this;
                self.selectedMethodType(null);
                self.authenticationMethods(viewModelParams.authenticationMethods.map(function (method, index) {
                    return new self.authenticationMethod(method.displayName, method.description, method.type, index === 0);
                }));
            },
            authenticationMethod: function (displayName, description, type, selected) {
                return {
                    displayName: displayName,
                    description: description,
                    type: type,
                    selected: ko.observable(selected)
                };
            },
            methodSelected: function (method) {
                self.authenticationMethods().forEach(function (x) {
                    if (x.selected()) {
                        x.selected(false);
                    }
                });
                method.selected(true);
                self.selectedMethodType(method.type);
            },
            authenticationMethods: ko.observableArray([]),
            selectedMethodType: ko.observable()
        }
    }
})();

