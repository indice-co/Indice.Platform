const refreshMeta = document.querySelector("meta[http-equiv=refresh]");
const rawUrl = refreshMeta ? refreshMeta.getAttribute("data-url") : null;
if (rawUrl) {
    try {
        const parsedUrl = new URL(rawUrl, window.location.origin);
        window.location.href = parsedUrl.href;
    } catch (e) {
        // Ignore invalid redirect URL.
    }
}
