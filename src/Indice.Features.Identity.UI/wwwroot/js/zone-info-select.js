!function () {
    const id = document.currentScript.getAttribute('data-id');
    const input = document.getElementById(id);

    if (input == undefined) return;

    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    const first = [].find.call(input.options, (option => 
        timezone === option.getAttribute('data-iana-id') || timezone === option.value
    ));

    if (first === undefined) return;

    first.selected = true;
}();