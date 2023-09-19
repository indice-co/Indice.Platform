!function () {
    const id = document.currentScript.getAttribute('data-id');
    const callingCodes = document.getElementById(id);

    if (callingCodes == undefined) return;

    function focus() {
        [].forEach.call(this.options, (option) => {
            option.textContent = option.getAttribute('data-text');
        });
    }
    function blur() {
        [].forEach.call(this.options, (option) => {
            if (option.selected) {
                option.textContent = '+' + option.getAttribute('value');
            }
            else {
                option.textContent = option.getAttribute('data-text');
            }
        });
    }
    callingCodes.addEventListener('focus', focus);
    callingCodes.addEventListener('blur', blur);
    callingCodes.addEventListener('change', blur);

    blur.call(callingCodes);
}();