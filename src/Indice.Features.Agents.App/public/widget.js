// @ts-check
/**
 * Dex chat widget loader.
 *
 *   <script src="https://<dex host>/widget.js" data-theme="acme" async></script>
 *
 * Draws a launcher in a corner of the page; the launcher opens a panel that frames the chat UI's compact `/embed`
 * route. Visitors chat as guests. Optional attributes on the script tag:
 *
 *   data-theme     a theme the Dex host serves (colours + `{theme}-logo.png` on the launcher; Dex's logo without one)
 *   data-position  `right` (default) or `left`
 *   data-label     accessible name and tooltip of the launcher (default `Chat`)
 *   data-open      open the panel straight away
 *   data-refid     the record this page is about; grounds a new chat on it (with the optional data-reftype)
 *
 * Page API, on `window.DexWidget` once the `dexwidget:ready` event has fired:
 *   open(), close(), toggle(), isOpen(), identify({ name })
 * and the `dexwidget:open` / `dexwidget:close` events. `identify` is an unverified display name, used when the visitor's
 * first chat is created; later calls do not rename an existing chat.
 *
 * Hand-written and dependency free (ES2019), so it runs on any site as is.
 */
(function () {
  'use strict';

  // Inside a frame there is nothing to do: the chat UI injects this script into its own pages, `/embed` included.
  try {
    if (window.top !== window.self) {
      return;
    }
  } catch (e) {
    return;
  }
  const win = /** @type {any} */ (window);
  if (win.DexWidget) {
    return;
  }
  const script = document.currentScript;
  if (!(script instanceof HTMLScriptElement) || !script.src) {
    return;
  }

  const OPEN_STORAGE_KEY = 'dexwidget.open';
  const ICON_BUBBLE =
    '<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 11.5a8.4 8.4 0 0 1-12.2 7.5L3 21l2-5.4A8.5 8.5 0 1 1 21 11.5Z"/></svg>';
  const ICON_CLOSE =
    '<svg class="icon close" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m6 9 6 6 6-6"/></svg>';

  // Where the chat UI lives: the origin and base path this script was served from.
  const scriptUrl = new URL(script.src, window.location.href);
  const dexOrigin = scriptUrl.origin;
  const basePath = scriptUrl.pathname.replace(/\/widget\.js$/i, '');

  const data = script.dataset;
  const theme = (data.theme || '').trim().toLowerCase().replace(/[^a-z0-9_-]/g, '');
  const side = data.position === 'left' ? 'left' : 'right';
  const label = (data.label || '').trim() || 'Chat';
  const startOpen = data.open !== undefined && data.open !== 'false';
  const refId = (data.refid || '').trim();
  const refType = (data.reftype || '').trim();

  /** @returns {string} the address of the frame: the `/embed` route with the theme and the page's topic. */
  function embedUrl() {
    const params = new URLSearchParams();
    if (theme) {
      params.set('uitheme', theme);
    }
    if (refId) {
      params.set('refid', refId);
      if (refType) {
        params.set('reftype', refType);
      }
    }
    const query = params.toString();
    return dexOrigin + basePath + '/embed' + (query ? '?' + query : '');
  }

  // ---- DOM: everything in a shadow root, so neither side's CSS reaches the other --------------------------------
  const host = document.createElement('div');
  host.setAttribute('data-dex-widget', '');
  const root = host.attachShadow({ mode: 'closed' });
  root.innerHTML =
    '<style>' +
    ':host{all:initial}' +
    '.launcher,.panel{position:fixed;z-index:2147483000;' + side + ':20px}' +
    '.launcher{bottom:20px;width:56px;height:56px;padding:0;border:0;border-radius:50%;cursor:pointer;' +
    'display:grid;place-items:center;overflow:hidden;background:#fff;color:#111827;' +
    'box-shadow:0 6px 20px rgba(15,23,42,.2),0 1px 3px rgba(15,23,42,.14);transition:transform .2s ease}' +
    '.launcher:hover{transform:scale(1.06)}' +
    '.launcher:focus-visible{outline:3px solid #2563eb;outline-offset:3px}' +
    '.launcher img{width:100%;height:100%;object-fit:cover}' +
    '.launcher.fallback,.launcher[aria-expanded="true"]{background:#111827;color:#fff}' +
    '.face{width:100%;height:100%;display:grid;place-items:center}' +
    '.icon{width:26px;height:26px}' +
    '.launcher .close,.launcher[aria-expanded="true"] .face{display:none}' +
    '.launcher[aria-expanded="true"] .close{display:block}' +
    '.panel{bottom:88px;width:min(400px,calc(100vw - 40px));height:min(640px,calc(100vh - 108px));' +
    'border-radius:16px;overflow:hidden;background:#fff;box-shadow:0 16px 48px rgba(15,23,42,.24);' +
    'transform-origin:bottom ' + side + ';opacity:0;transform:translateY(12px) scale(.98);visibility:hidden;' +
    'transition:opacity .18s ease,transform .18s ease,visibility 0s linear .18s}' +
    '.panel.open{opacity:1;transform:none;visibility:visible;transition:opacity .18s ease,transform .18s ease}' +
    '.panel iframe{display:block;width:100%;height:100%;border:0}' +
    '@media (max-width:639px){.panel{inset:0;width:auto;height:auto;border-radius:0}' +
    ':host([data-open]) .launcher{display:none}}' +
    '@media (prefers-reduced-motion:reduce){.launcher,.panel,.panel.open{transition:none}.launcher:hover{transform:none}}' +
    '</style>' +
    '<div class="panel" role="region"></div>' +
    '<button class="launcher" type="button" aria-expanded="false"><span class="face"></span>' + ICON_CLOSE + '</button>';

  const panel = /** @type {HTMLDivElement} */ (root.querySelector('.panel'));
  const launcher = /** @type {HTMLButtonElement} */ (root.querySelector('.launcher'));
  const face = /** @type {HTMLSpanElement} */ (root.querySelector('.face'));
  panel.setAttribute('aria-label', label);
  launcher.setAttribute('aria-label', label);
  launcher.title = label;

  // The theme's logo, or Dex's own mark (a full-bleed circle, so it fills the launcher); the bubble if it fails to load.
  function showBubble() {
    launcher.classList.add('fallback');
    face.innerHTML = ICON_BUBBLE;
  }
  const logo = document.createElement('img');
  logo.alt = '';
  logo.src = dexOrigin + basePath + '/' + (theme ? theme + '-logo.png' : 'dex-logo.svg');
  logo.addEventListener('error', showBubble, { once: true });
  face.appendChild(logo);

  // ---- State ----------------------------------------------------------------------------------------------------
  /** @type {HTMLIFrameElement | null} */
  let frame = null;
  let frameReady = false;
  let open = false;
  /** @type {{ name: string } | null} */
  let identity = null;

  /** Created on first open, then kept (hidden) while closed, so the conversation and its stream survive. */
  function ensureFrame() {
    if (frame) {
      return frame;
    }
    frame = document.createElement('iframe');
    frame.title = label;
    frame.allow = 'clipboard-write';
    frame.src = embedUrl();
    panel.appendChild(frame);
    return frame;
  }

  /**
   * @param {string} type
   * @param {unknown} [payload]
   */
  function post(type, payload) {
    if (frame && frame.contentWindow) {
      frame.contentWindow.postMessage({ dexWidget: 1, type: type, payload: payload }, dexOrigin);
    }
  }

  /** Sent even when there is no name: a null payload clears a name the frame was given earlier. */
  function sendIdentity() {
    if (frameReady) {
      post('identify', identity);
    }
  }

  /** @param {boolean} value */
  function remember(value) {
    try {
      if (value) {
        sessionStorage.setItem(OPEN_STORAGE_KEY, '1');
      } else {
        sessionStorage.removeItem(OPEN_STORAGE_KEY);
      }
    } catch (e) {
      // Storage unavailable: the panel just won't reopen on the next page.
    }
  }

  function remembered() {
    try {
      return sessionStorage.getItem(OPEN_STORAGE_KEY) === '1';
    } catch (e) {
      return false;
    }
  }

  /**
   * @param {boolean} value
   * @param {boolean} [moveFocus] move focus into the panel on open, back to the launcher on close
   */
  function setOpen(value, moveFocus) {
    if (value === open) {
      return;
    }
    open = value;
    const iframe = value ? ensureFrame() : frame;
    const focusInside = document.activeElement === host;
    panel.classList.toggle('open', value);
    host.toggleAttribute('data-open', value);
    launcher.setAttribute('aria-expanded', String(value));
    remember(value);
    if (moveFocus !== false) {
      if (value && iframe) {
        iframe.focus();
      } else if (!value && focusInside) {
        launcher.focus();
      }
    }
    window.dispatchEvent(new CustomEvent(value ? 'dexwidget:open' : 'dexwidget:close'));
  }

  launcher.addEventListener('click', function () {
    setOpen(!open);
  });
  root.addEventListener('keydown', function (event) {
    if (/** @type {KeyboardEvent} */ (event).key === 'Escape' && open) {
      setOpen(false);
    }
  });

  // Only the frame this loader created, served from the Dex origin, is listened to.
  window.addEventListener('message', function (event) {
    if (!frame || event.source !== frame.contentWindow || event.origin !== dexOrigin) {
      return;
    }
    const message = event.data;
    if (!message || message.dexWidget !== 1 || typeof message.type !== 'string') {
      return;
    }
    switch (message.type) {
      case 'ready':
        frameReady = true;
        sendIdentity();
        break;
      case 'close':
        setOpen(false);
        break;
    }
  });

  // ---- Page API -------------------------------------------------------------------------------------------------
  win.DexWidget = Object.freeze({
    open: function () {
      setOpen(true);
    },
    close: function () {
      setOpen(false);
    },
    toggle: function () {
      setOpen(!open);
    },
    isOpen: function () {
      return open;
    },
    /**
     * The visitor's display name, for the chat the visitor has not started yet. Unverified: personalization only.
     * @param {{ name?: string } | null | undefined} value
     */
    identify: function (value) {
      const name = value && typeof value.name === 'string' ? value.name.trim().slice(0, 100) : '';
      identity = name ? { name: name } : null;
      sendIdentity();
    },
  });

  function mount() {
    document.body.appendChild(host);
    // Reopen on the next page of the site if the visitor left it open, and honour data-open.
    if (startOpen || remembered()) {
      setOpen(true, false);
    }
  }
  if (document.body) {
    mount();
  } else {
    document.addEventListener('DOMContentLoaded', mount, { once: true });
  }
  window.dispatchEvent(new CustomEvent('dexwidget:ready'));
})();
