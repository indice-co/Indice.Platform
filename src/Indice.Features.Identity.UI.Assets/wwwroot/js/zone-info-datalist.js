!function () {
    const id = document.currentScript.getAttribute('data-id');
    const input = document.getElementById(id);
    const list = document.getElementById(id + "_list");

    if (input == undefined || list == undefined) return;

    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    const first = [].find.call(list.options, (option => timezone === option.value));

    if (first === undefined) return;

    input.value = first.value;
}();