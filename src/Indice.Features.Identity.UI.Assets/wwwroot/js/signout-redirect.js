window.addEventListener("load", function () {
    var a = document.getElementById('PostLogoutRedirectUri');
    if (a) {
        window.location = a.href;
    }
});
