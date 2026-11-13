var indice = indice || {};

(function () {
    indice.MfaOnboardingViewModelFactory = function (viewModelParams) {
        return {
            self: undefined,
            init: function () {
                self = this;
                self.authenticationMethods(viewModelParams.authenticationMethods.map(function (method, index) {
                    return new self.authenticationMethod(method.displayName, method.description, method.type, index === 0, viewModelParams.icons[method.code]);
                }));
                var first = self.authenticationMethods()[0];
                if (first) {
                    self.selectedMethod(first);
                    self.selectedMethodType(first.type);
                }
            },
            authenticationMethod: function (displayName, description, type, selected, iconClass) {
                return {
                    displayName: displayName,
                    description: description,
                    type: type,
                    iconClass: iconClass,
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
                self.selectedMethod(method);
                self.selectedMethodType(method.type);
                return true;
            },
            authenticationMethods: ko.observableArray([]),
            selectedMethod: ko.observable(),
            selectedMethodType: ko.observable()
        }
    }
})();

