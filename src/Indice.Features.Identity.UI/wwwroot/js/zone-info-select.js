!function () {
    const id = document.currentScript.getAttribute('data-id');
    const input = document.getElementById(id);

    if (input == undefined) return;

    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    const matchingOption = [].find.call(input.options, (option => 
        timezone === option.value
    ));

    if (matchingOption !== undefined) {
        matchingOption.selected = true;
    } else if (input.options.length > 0) {
        // No matching option found, set the first option's value and text to the browser's timezone
        input.options[0].value = timezone;
        input.options[0].text = timezone;
        input.options[0].selected = true;
    }
}();