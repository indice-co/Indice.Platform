// Progressive enhancement for one-time-code inputs.
// Any <input class="otp-field__input"> is replaced visually by N single-digit cells that mirror
// their value into the original input, which stays in the form (and remains the posted field).
// Without JavaScript the original input is a plain text box.
(function () {
    'use strict';

    function enhance(input) {
        if (input.dataset.otpEnhanced === 'true' || input.disabled || input.readOnly) {
            return;
        }
        var length = parseInt(input.dataset.otpLength || input.getAttribute('maxlength') || '6', 10);
        if (!length || length < 2 || length > 12) {
            return;
        }
        input.dataset.otpEnhanced = 'true';

        var container = document.createElement('div');
        container.className = 'otp-field';
        container.setAttribute('role', 'group');
        if (input.getAttribute('aria-describedby')) {
            container.setAttribute('aria-describedby', input.getAttribute('aria-describedby'));
        }
        var label = input.id ? document.querySelector('label[for="' + input.id + '"]') : null;
        if (label) {
            label.id = label.id || input.id + '-label';
            container.setAttribute('aria-labelledby', label.id);
        }

        var cells = [];
        var initial = (input.value || '').split('');
        for (var i = 0; i < length; i++) {
            var cell = document.createElement('input');
            cell.type = 'text';
            cell.className = 'otp-field__cell';
            cell.inputMode = 'numeric';
            cell.autocomplete = i === 0 ? 'one-time-code' : 'off';
            cell.maxLength = 1;
            cell.setAttribute('aria-label', (i + 1) + ' / ' + length);
            cell.value = initial[i] || '';
            cells.push(cell);
            container.appendChild(cell);
        }

        function sync() {
            input.value = cells.map(function (c) { return c.value; }).join('');
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        function fill(text, startIndex) {
            var chars = (text || '').replace(/\s+/g, '').split('');
            var index = startIndex;
            chars.forEach(function (ch) {
                if (index < length) {
                    cells[index].value = ch;
                    index++;
                }
            });
            sync();
            cells[Math.min(index, length - 1)].focus();
        }

        cells.forEach(function (cell, index) {
            cell.addEventListener('input', function () {
                if (cell.value.length > 1) {
                    fill(cell.value, index);
                    return;
                }
                sync();
                if (cell.value && index < length - 1) {
                    cells[index + 1].focus();
                    cells[index + 1].select();
                }
            });
            cell.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && !cell.value && index > 0) {
                    cells[index - 1].value = '';
                    cells[index - 1].focus();
                    sync();
                    e.preventDefault();
                } else if (e.key === 'ArrowLeft' && index > 0) {
                    cells[index - 1].focus();
                    e.preventDefault();
                } else if (e.key === 'ArrowRight' && index < length - 1) {
                    cells[index + 1].focus();
                    e.preventDefault();
                }
            });
            cell.addEventListener('paste', function (e) {
                var text = (e.clipboardData || window.clipboardData).getData('text');
                if (text) {
                    e.preventDefault();
                    fill(text, index);
                }
            });
            cell.addEventListener('focus', function () { cell.select(); });
        });

        input.classList.add('otp-field__input--enhanced');
        input.setAttribute('tabindex', '-1');
        input.setAttribute('aria-hidden', 'true');
        input.insertAdjacentElement('afterend', container);

        if (input.hasAttribute('autofocus') || document.activeElement === input) {
            cells[0].focus();
        }
    }

    function init() {
        document.querySelectorAll('input.otp-field__input').forEach(enhance);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
