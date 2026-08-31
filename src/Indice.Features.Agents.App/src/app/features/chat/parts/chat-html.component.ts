import { ChangeDetectionStrategy, Component, ViewEncapsulation, computed, input } from '@angular/core';

/**
 * Renders a `text/html` part dex-html fragment in the chat thread.
 *
 * The fragment may arrive in three shapes: a plain HTML string, or a `data:text/html` URI in either its base64 or its
 * percent-encoded form. All three decode to the same markup before rendering; a data URI that fails to decode renders
 * nothing rather than spilling an opaque base64 blob into the thread.
 *
 * The fragment is bound through `[innerHTML]`, which routes it through Angular's built-in sanitizer: scripts, event
 * handlers, and other active content are stripped before the markup reaches the DOM. That is a deliberate choice over
 * `bypassSecurityTrustHtml` — the fragment usually originates from a model or an external document, so it is never
 * trusted wholesale.
 *
 * An empty or whitespace-only fragment renders nothing at all, costing no gap in the part stack — the same discipline
 * the other part renderers apply to payloads with nothing to show.
 */
@Component({
  selector: 'app-chat-html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  styles: `
    .dex-html { line-height: 1.55; }
    .dex-html section, .dex-html article { padding: 0.25rem 0; }
    .dex-html h1, .dex-html h2, .dex-html h3, .dex-html h4 {
      margin: 0.75rem 0 0.25rem; font-weight: 600; line-height: 1.3;
    }
    .dex-html h3 { font-size: 1.05rem; }
    .dex-html p { margin: 0.375rem 0; }
    .dex-html ul { margin: 0.5rem 0; padding-left: 0; list-style: none; }
    .dex-html li { margin: 0.375rem 0; }
    .dex-html a { color: var(--color-primary); text-decoration: none; }
    .dex-html a:hover { text-decoration: underline; }
    .dex-html img { border-radius: 0.75rem; }
    .dex-html figure { margin: 0; }
    .dex-html .dex-card {
      display: flex; gap: 1rem; align-items: flex-start;
      padding: 1rem; margin: 0.25rem 0;
      border: 1px solid var(--color-base-300); border-radius: 1rem;
      background: var(--color-base-100);
    }
    .dex-html .dex-card img {
      width: 5rem; height: 5rem; border-radius: 9999px; object-fit: cover; flex: none;
      outline: 2px solid var(--color-primary); outline-offset: 2px;
    }
    .dex-html .dex-muted { color: color-mix(in oklch, var(--color-base-content) 60%, transparent); font-size: 0.85em; }
    .dex-html .dex-badge {
      display: inline-block; padding: 0.125rem 0.625rem; border-radius: 9999px;
      font-size: 0.75rem; font-weight: 500;
      background: color-mix(in oklch, var(--color-primary) 12%, transparent);
      color: var(--color-primary);
    }
  `,
  template: `
    @if (content(); as fragment) {
      <div
        class="dex-html text-[0.95rem]"
        [class.rounded-tl-sm]="first()"
        [class.dex-caret]="caret()"
        [innerHTML]="fragment"
      ></div>
    }
  `,
})
export class ChatHtmlComponent {
  /** The HTML fragment to render, or null when the part carried nothing renderable. */
  readonly html = input<string | null | undefined>(null);
  /** Whether this is the message's first part; only that one gets the bubble tail pointing at the avatar. */
  readonly first = input(false);
  /** Whether to show the blinking streaming caret. */
  readonly caret = input(false);

  protected readonly content = computed(() => decodeHtmlFragment(this.html()));
}

/** Matches a `data:text/html[;charset=...][;base64],payload` URI, capturing the parameter list and the payload. */
const DATA_URI_PATTERN = /^data:text\/html([^,]*),([\s\S]*)$/i;

/**
 * Normalizes a `text/html` part value to the markup it carries. A plain string passes through trimmed; a
 * `data:text/html` URI is decoded from base64 (assumed UTF-8) or percent-encoding as its parameters dictate. Anything
 * empty or undecodable yields `null`, which the template treats as "nothing to show".
 */
function decodeHtmlFragment(value: string | null | undefined): string | null {
  const raw = value?.trim();
  if (!raw) {
    return null;
  }
  const match = DATA_URI_PATTERN.exec(raw);
  if (!match) {
    return raw;
  }
  const [, params, payload] = match;
  try {
    const decoded = /;base64/i.test(params)
      ? new TextDecoder().decode(Uint8Array.from(atob(payload), (char) => char.charCodeAt(0)))
      : decodeURIComponent(payload);
    const fragment = decoded.trim();
    return fragment ? fragment : null;
  } catch {
    return null;
  }
}
