(function ($) {
    function togglePassword(e) {
        e.preventDefault();

        const $btn = $(this);

        const $root = $btn.closest('.password-control');
        const $input = $root.find('.form-control');

        if (!$input.length) return;

        const isVisible = $input.attr('type') === 'text';

        $input.attr('type', isVisible ? 'password' : 'text');
        $btn.toggleClass('active', !isVisible);
        $btn.attr('aria-pressed', String(!isVisible));
        const label = isVisible ? 'Show password' : 'Hide password';
        $btn.attr('aria-label', label);
        $btn.attr('title', label);
    }
    $(document).on('click', '.password-control .btn-password-show', togglePassword);
})(jQuery);