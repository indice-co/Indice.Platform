/*!
 * Dex chat widget — drop one tag on any page:
 *
 *   <script src="https://<dex host>/widget.js" async></script>
 *
 * It draws a launcher bubble in the bottom corner; a click opens the Dex chat (the app's `/embed` route) in an iframe
 * panel above it. The embedding page's origin must be allowed by the Dex host (AgentsUIOptions.FrameAncestors),
 * otherwise the browser refuses to render the panel.
 *
 * Optional attributes on the script tag:
 *   data-theme     the Dex theme of the panel (e.g. "papadakis"); defaults to the host's configured theme
 *   data-refid     the record the visit is about — grounds the first conversation (with data-reftype)
 *   data-reftype   the kind of record data-refid names
 *   data-position  "right" (default) or "left"
 *   data-color     launcher colour, any CSS colour; defaults to the dex brand red
 *   data-label     accessible name of the launcher; defaults to "Open chat"
 *
 * Page API: window.DexWidget.open() / .close() / .toggle()
 */
(function () {
  'use strict';

  var script = document.currentScript;
  if (!script || window.DexWidget) {
    return;
  }
  var framed;
  try {
    framed = window.self !== window.top;
  } catch (e) {
    framed = true;
  }
  // Never inside a frame: the panel itself (and Dex's hidden renew iframe) load this script too when the host
  // injects it into every Dex page.
  if (framed) {
    return;
  }

  /** Message tag shared with the app's core/embed/embed.ts. */
  var MESSAGE_SOURCE = 'dex-widget';
  /** Where the open/closed state is remembered, so the panel stays open as the visitor moves between pages. */
  var OPEN_KEY = 'dex.widget.open';
  /** Dex's own routes that must not show a launcher — the panel, and the OIDC pages a sign-in popup lands on. */
  var OWN_ROUTES = ['embed', 'auth-callback', 'auth-renew', 'logged-out'];

  var base = new URL('.', script.src);
  if (location.origin === base.origin && location.pathname.indexOf(base.pathname) === 0) {
    var route = location.pathname.slice(base.pathname.length).split('/')[0];
    if (OWN_ROUTES.indexOf(route) >= 0) {
      return;
    }
  }

  var data = script.dataset;
  var label = data.label || 'Open chat';
  var color = data.color && window.CSS && CSS.supports('color', data.color) ? data.color : 'oklch(58.5% 0.214 27)';

  var host = document.createElement('div');
  host.id = 'dex-widget';
  host.style.setProperty('--dex-color', color);
  var root = host.attachShadow({ mode: 'open' });
  root.innerHTML =
    '<style>' +
    ':host{all:initial}' +
    '.launcher,.panel{position:fixed;z-index:2147483000;right:20px}' +
    '.left .launcher,.left .panel{right:auto;left:20px}' +
    '.launcher{bottom:20px;width:56px;height:56px;padding:0;border:0;border-radius:50%;display:grid;place-items:center;' +
    'background:var(--dex-color);color:#fff;cursor:pointer;box-shadow:0 6px 20px rgba(0,0,0,.25);transition:transform .15s ease}' +
    '.launcher:hover{transform:scale(1.06)}' +
    '.launcher:focus-visible{outline:3px solid var(--dex-color);outline-offset:3px}' +
    '.launcher svg{width:26px;height:26px}' +
    '.launcher .close,.open .launcher .chat{display:none}' +
    '.open .launcher .close{display:block}' +
    '.panel{bottom:88px;width:380px;height:min(640px,calc(100vh - 108px));border-radius:16px;overflow:hidden;background:#fff;' +
    'box-shadow:0 12px 40px rgba(0,0,0,.28);opacity:0;transform:translateY(12px);visibility:hidden;' +
    'transition:opacity .18s ease,transform .18s ease,visibility 0s linear .18s}' +
    '.open .panel{opacity:1;transform:none;visibility:visible;transition:opacity .18s ease,transform .18s ease}' +
    'iframe{display:block;width:100%;height:100%;border:0}' +
    '@media (max-width:480px){.panel,.left .panel{inset:0;width:auto;height:auto;border-radius:0}.open .launcher{display:none}}' +
    '@media (prefers-reduced-motion:reduce){.launcher,.panel,.open .panel{transition:none}}' +
    '</style>' +
    '<div class="dex">' +
    '<div class="panel"></div>' +
    '<button class="launcher" type="button" aria-expanded="false">' +
    '<svg class="chat" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M21 12a8 8 0 0 1-11.6 7.1L4 20l1-4.6A8 8 0 1 1 21 12z" ' +
    'stroke="currentColor" stroke-width="2" stroke-linejoin="round"/></svg>' +
    '<svg class="close" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18" ' +
    'stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>' +
    '</button>' +
    '</div>';

  var wrapper = root.querySelector('.dex');
  var panel = root.querySelector('.panel');
  var launcher = root.querySelector('.launcher');
  var frame = null;
  var isOpen = false;

  if (data.position === 'left') {
    wrapper.classList.add('left');
  }
  launcher.setAttribute('aria-label', label);

  /** The panel's address: the app's `/embed` route, told who hosts it and what the visit is about. */
  function panelUrl() {
    var url = new URL('embed', base);
    url.searchParams.set('origin', location.origin);
    if (data.theme) {
      url.searchParams.set('uitheme', data.theme);
    }
    if (data.refid) {
      url.searchParams.set('refid', data.refid);
      if (data.reftype) {
        url.searchParams.set('reftype', data.reftype);
      }
    }
    return url.href;
  }

  function setOpen(next, moveFocus) {
    if (next === isOpen) {
      return;
    }
    isOpen = next;
    if (next && !frame) {
      // Created on first open, so a page that never opens the chat never loads it.
      frame = document.createElement('iframe');
      frame.title = label;
      frame.allow = 'clipboard-write';
      frame.src = panelUrl();
      panel.appendChild(frame);
    }
    wrapper.classList.toggle('open', next);
    launcher.setAttribute('aria-expanded', String(next));
    launcher.setAttribute('aria-label', next ? 'Close chat' : label);
    remember(next);
    if (moveFocus !== false) {
      if (next) {
        frame.focus();
      } else if (document.activeElement === host) {
        // Focus was in the panel, which is now hidden — hand it back to the launcher.
        launcher.focus();
      }
    }
  }

  function remember(open) {
    try {
      if (open) {
        sessionStorage.setItem(OPEN_KEY, '1');
      } else {
        sessionStorage.removeItem(OPEN_KEY);
      }
    } catch (e) {
      // Storage refused — the panel just starts closed on the next page.
    }
  }

  function recall() {
    try {
      return sessionStorage.getItem(OPEN_KEY) === '1';
    } catch (e) {
      return false;
    }
  }

  launcher.addEventListener('click', function () {
    setOpen(!isOpen);
  });

  // Escape while focus is on the widget (inside the panel, the app handles it and posts `close`).
  root.addEventListener('keydown', function (event) {
    if (event.key === 'Escape' && isOpen) {
      setOpen(false);
    }
  });

  // Only the panel's own frame, served from the Dex origin, may drive the widget.
  window.addEventListener('message', function (event) {
    if (!frame || event.source !== frame.contentWindow || event.origin !== base.origin) {
      return;
    }
    var message = event.data;
    if (!message || message.source !== MESSAGE_SOURCE) {
      return;
    }
    if (message.type === 'close') {
      setOpen(false);
    }
  });

  window.DexWidget = {
    open: function () {
      setOpen(true);
    },
    close: function () {
      setOpen(false);
    },
    toggle: function () {
      setOpen(!isOpen);
    },
  };

  function mount() {
    document.body.appendChild(host);
    if (recall()) {
      // Reopened after a navigation — don't steal focus from the page that just loaded.
      setOpen(true, false);
    }
  }

  if (document.body) {
    mount();
  } else {
    document.addEventListener('DOMContentLoaded', mount);
  }
})();
