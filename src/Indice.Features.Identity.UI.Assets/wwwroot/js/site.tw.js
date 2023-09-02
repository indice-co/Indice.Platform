(function () {
    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);
    const sidebar = urlParams.get('sidebar');
    if (sidebar == 'true') {
        const sidebar = document.getElementById('sidebar');
        const options = {
            backdropClasses: 'bg-transparent',
        };
        const modal = new Modal(sidebar, options);
        modal.toggle();
        sidebar.classList.remove('flex', 'justify-center', 'items-center');
    }
})();