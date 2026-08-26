/**
 * The rendering contracts between the pipeline and the chat UI. Every media type here mirrors a constant in
 * `AgentsConstants.MediaTypes` on the server: a part carrying one holds a JSON payload that a dedicated component
 * renders instead of markdown.
 *
 * Every parser below is called from a template, so none of them may throw. An unexpected payload degrades to `null`
 * (or an empty list), which the renderers treat as "nothing to show" — a newer server can send a shape this client has
 * never seen without taking the thread down.
 */

/** A list of options the user can pick from; picking one posts it verbatim as the next user message. */
export const MULTIPLE_CHOICE_MEDIA_TYPE = 'application/vnd.indice.multiple-choice+json';

/** A single image rendered as a figure, with optional alt text and caption. */
export const IMAGE_MEDIA_TYPE = 'application/vnd.indice.image+json';

/** A short highlighted notice rendered as an alert. */
export const CALLOUT_MEDIA_TYPE = 'application/vnd.indice.callout+json';

/** A two-way confirmation; picking a button posts its label verbatim as the next user message. */
export const CONFIRM_MEDIA_TYPE = 'application/vnd.indice.confirm+json';

/** What `ChatMessagePartComponent` renders a part as. */
export type PartKind = 'markdown' | 'image' | 'multiple-choice' | 'callout' | 'confirm' | 'unknown';

/**
 * Classifies a part by its `contentType`. This exists as a function rather than a plain `@switch` on the raw media type
 * because images need *prefix* matching: an image attached as `DataContent`/`UriContent` arrives as `image/png`,
 * `image/svg+xml`, and so on. Anything unrecognised is `'unknown'`, which renders nothing — the same forward-compat
 * discipline `chat-stream.service.ts` applies to unknown SSE frame types.
 */
export function partKind(contentType: string | undefined): PartKind {
  switch (contentType) {
    case 'text/markdown':
    case 'text':
      return 'markdown';
    case MULTIPLE_CHOICE_MEDIA_TYPE:
      return 'multiple-choice';
    case IMAGE_MEDIA_TYPE:
      return 'image';
    case CALLOUT_MEDIA_TYPE:
      return 'callout';
    case CONFIRM_MEDIA_TYPE:
      return 'confirm';
    default:
      return contentType?.startsWith('image/') ? 'image' : 'unknown';
  }
}

/** An image to render as a figure. Mirrors the server's `ImageReference`. */
export interface ImageReference {
  url: string;
  alt?: string;
  caption?: string;
}

/** How prominently a callout is rendered. Mirrors the server's `Callout.Severities`. */
export type CalloutSeverity = 'info' | 'success' | 'warning' | 'error';

/** A short highlighted notice. Mirrors the server's `Callout`. */
export interface Callout {
  severity: CalloutSeverity;
  title?: string;
  text: string;
}

/** A two-way choice whose button labels are the messages posted. Mirrors the server's `Confirmation`. */
export interface Confirmation {
  prompt?: string;
  confirmText: string;
  cancelText: string;
}

/** Reads the options out of a multiple-choice part value; anything unexpected yields an empty list. */
export function parseMultipleChoice(value: string | undefined): string[] {
  const parsed = parseObject<{ options?: unknown }>(value);
  return Array.isArray(parsed?.options)
    ? parsed.options.filter((option): option is string => typeof option === 'string' && option.trim().length > 0)
    : [];
}

/**
 * Reads an image out of a part. Handles both shapes: the `image+json` envelope, and a raw `image/*` part whose value is
 * already the URL — a hosted `https:` one from a `UriContent`, or the `data:` URI of an embedded `DataContent`.
 */
export function parseImage(value: string | undefined, contentType: string | undefined): ImageReference | null {
  if (contentType !== IMAGE_MEDIA_TYPE && contentType?.startsWith('image/')) {
    const url = value?.trim() ?? '';
    return isRenderableImageUrl(url) ? { url } : null;
  }
  const parsed = parseObject<{ url?: unknown; alt?: unknown; caption?: unknown }>(value);
  const url = typeof parsed?.url === 'string' ? parsed.url.trim() : '';
  if (!isRenderableImageUrl(url)) {
    return null;
  }
  return {
    url,
    alt: typeof parsed?.alt === 'string' ? parsed.alt : undefined,
    caption: typeof parsed?.caption === 'string' ? parsed.caption : undefined,
  };
}

/** Reads a callout out of a part value. An unknown severity falls back to `info`; a bodiless callout renders nothing. */
export function parseCallout(value: string | undefined): Callout | null {
  const parsed = parseObject<{ severity?: unknown; title?: unknown; text?: unknown }>(value);
  const text = typeof parsed?.text === 'string' ? parsed.text : '';
  if (!text.trim()) {
    return null;
  }
  return {
    severity: CALLOUT_SEVERITIES.find((severity) => severity === parsed?.severity) ?? 'info',
    title: typeof parsed?.title === 'string' && parsed.title.trim() ? parsed.title : undefined,
    text,
  };
}

/** Reads a confirmation out of a part value. Missing labels fall back to Yes/No, matching the server's defaults. */
export function parseConfirmation(value: string | undefined): Confirmation | null {
  const parsed = parseObject<{ prompt?: unknown; confirmText?: unknown; cancelText?: unknown }>(value);
  if (!parsed) {
    return null;
  }
  return {
    prompt: typeof parsed.prompt === 'string' && parsed.prompt.trim() ? parsed.prompt : undefined,
    confirmText: label(parsed.confirmText, 'Yes'),
    cancelText: label(parsed.cancelText, 'No'),
  };
}

const CALLOUT_SEVERITIES: readonly CalloutSeverity[] = ['info', 'success', 'warning', 'error'];

/**
 * Only `http`, `https`, `data:image/` and same-origin root-relative URLs reach the DOM. `//host/x` is deliberately
 * excluded: despite the leading slash it is protocol-relative and points off-origin. Angular's `[src]` sanitizer
 * already neutralises `javascript:`, but the payload is ultimately model-influenced, so it is rejected here rather
 * than relying on a downstream escape hatch.
 */
function isRenderableImageUrl(url: string): boolean {
  const value = url.toLowerCase();
  return (
    value.startsWith('https://') ||
    value.startsWith('http://') ||
    value.startsWith('data:image/') ||
    (value.startsWith('/') && !value.startsWith('//'))
  );
}

/** Parses a part value into a plain JSON object, or `null` for anything else (malformed, empty, array, scalar). */
function parseObject<T>(value: string | undefined): T | null {
  if (!value) {
    return null;
  }
  try {
    const parsed: unknown = JSON.parse(value);
    return typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed) ? (parsed as T) : null;
  } catch {
    return null;
  }
}

function label(value: unknown, fallback: string): string {
  return typeof value === 'string' && value.trim() ? value : fallback;
}
