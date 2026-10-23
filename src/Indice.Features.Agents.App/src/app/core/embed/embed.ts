/**
 * The app as the chat panel of the embeddable widget: `public/widget.js` draws a launcher on the host page and
 * frames the `/embed` route, handing over the host page's origin as `?origin=`. The two sides talk over
 * `postMessage`, always addressed to the other side's exact origin and tagged with `MESSAGE_SOURCE`.
 */

/** Message tag shared with `public/widget.js`, so either side can ignore unrelated `postMessage` traffic. */
const MESSAGE_SOURCE = 'dex-widget';

/** What the panel tells the host page: it has rendered (`ready`), or the visitor asked to close it (`close`). */
export type HostMessageType = 'ready' | 'close';

/** Whether the app runs inside a frame — as the widget's panel. A frame we may not even inspect counts too. */
export const isEmbedded = (() => {
  try {
    return window.self !== window.top;
  } catch {
    return true;
  }
})();

/** The origin of the page hosting the widget (`?origin=`, read once at startup); `null` when absent or malformed. */
export const hostOrigin = readHostOrigin();

/** Posts a message to the host page — only when framed and only to the origin widget.js handed over, never `*`. */
export function postToHost(type: HostMessageType): void {
  if (!isEmbedded || !hostOrigin) {
    return;
  }
  window.parent.postMessage({ source: MESSAGE_SOURCE, type }, hostOrigin);
}

function readHostOrigin(): string | null {
  const value = new URLSearchParams(window.location.search).get('origin');
  if (!value) {
    return null;
  }
  try {
    const origin = new URL(value).origin;
    // Opaque origins (file:, data:) serialize as "null" — nothing we could address a message to.
    return origin === 'null' ? null : origin;
  } catch {
    return null;
  }
}
