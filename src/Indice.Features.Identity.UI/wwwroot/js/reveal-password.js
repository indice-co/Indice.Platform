(function ($) {
    function togglePassword(e) {
        e.preventDefault();

        var $btn = $(this);
        var $root = $btn.closest('.field__group, .password-control');
        var $input = $root.find('input.password');

        if (!$input.length) return;

        var isVisible = $input.attr('type') === 'text';

        $input.attr('type', isVisible ? 'password' : 'text');
        $btn.attr('aria-pressed', String(!isVisible));

        var label = isVisible
            ? ($btn.data('label-show') || 'Show password')
            : ($btn.data('label-hide') || 'Hide password');
        $btn.attr('aria-label', label);
        $btn.attr('title', label);

        $btn.find('.fa-eye, .fa-eye-slash')
            .toggleClass('fa-eye', isVisible)
            .toggleClass('fa-eye-slash', !isVisible);
    }

    $(document).on('click', '.field__reveal, .password-control .reveal-icon', togglePassword);
})(jQuery);
