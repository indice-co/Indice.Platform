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
        // No matching option found, create a localized display name for the browser's timezone
        const locale = document.documentElement.lang || undefined;
        const offset = new Intl.DateTimeFormat(locale, {
            timeZone: timezone,
            timeZoneName: 'longOffset'
        }).formatToParts().find(p => p.type === 'timeZoneName').value.replace(/^GMT/, 'UTC');
        const name = new Intl.DateTimeFormat(locale, {
            timeZone: timezone,
            timeZoneName: 'long'
        }).formatToParts().find(p => p.type === 'timeZoneName').value;
        const displayName = `(${offset}) ${name}`;

        input.options[0].value = timezone;
        input.options[0].text = displayName;
        input.options[0].selected = true;
    }
}();