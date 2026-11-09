var indice = indice || {};

(function () {
    indice.MfaOnboardingViewModelFactory = function (viewModelParams) {
        return {
            self: undefined,
            init: function () {
                self = this;
                self.authenticationMethods(viewModelParams.authenticationMethods.map(function (method, index) {
                    var selected = index === 0;
                    if (selected) {
                        self.selectedMethodType(method.type);
                    }
                    return new self.authenticationMethod(method.displayName, method.description, method.type, selected, viewModelParams.icons[method.code]);
                }));
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
                self.selectedMethodType(method.type);
            },
            authenticationMethods: ko.observableArray([]),
            selectedMethodType: ko.observable()
        }
    }
})();

