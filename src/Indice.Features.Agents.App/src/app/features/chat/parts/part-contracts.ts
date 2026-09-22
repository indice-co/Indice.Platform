/**
 * The rendering contracts between the pipeline and the chat UI. Every media type here mirrors a constant in
 * `AgentsConstants.MediaTypes` on the server: a part carrying one holds a JSON payload that a dedicated component
 * renders instead of markdown.
 *
 * Every parser below is called from a template, so none of them may throw. An unexpected payload degrades to `null`
 * (or an empty list), which the renderers treat as "nothing to show" — a newer server can send a shape this client has
 * never seen without taking the thread down.
 */

// Type-only: the generated shape of a part, so the builders below cannot drift from the wire contract. It adds no
// runtime dependency, which is what keeps this file importable from a spec without a TestBed.
import type { IChatMessagePart } from '../../../core/services/dex-api.service';

/** A list of options the user can pick from; picking one posts it verbatim as the next user message. */
export const MULTIPLE_CHOICE_MEDIA_TYPE = 'application/vnd.indice.multiple-choice+json';

/** A single image rendered as a figure, with an optional caption. */
export const IMAGE_MEDIA_TYPE = 'application/vnd.indice.image+json';

/** A short highlighted notice rendered as an alert. */
export const CALLOUT_MEDIA_TYPE = 'application/vnd.indice.callout+json';

/** A two-way confirmation; picking a button posts its label verbatim as the next user message. */
export const CONFIRM_MEDIA_TYPE = 'application/vnd.indice.confirm+json';

/**
 * A question the workflow is blocked on until a human answers it — the assistant half of the human-in-the-loop
 * round-trip. The answer goes back on the next user turn as a {@link HITL_RESPONSE_MEDIA_TYPE} part.
 */
export const HITL_REQUEST_MEDIA_TYPE = 'application/vnd.indice.function-call.request+json';

/**
 * The user half of that round-trip. It only ever appears on a *user* turn, which is why it has no {@link PartKind}:
 * user turns render their text directly and never go through `ChatMessagePartComponent`.
 *
 * The structured payload rides as raw JSON text. `ChatMessagePart.ToAIContent()` handles
 * `application/vnd.indice.function-call.response+json` explicitly and correlates it through the part's `requestId`.
 */
export const HITL_RESPONSE_MEDIA_TYPE = 'application/vnd.indice.function-call.response+json';

/** What `ChatMessagePartComponent` renders a part as. */
export type PartKind =
  | 'markdown'
  | 'html'
  | 'image'
  | 'multiple-choice'
  | 'callout'
  | 'confirm'
  | 'hitl-request'
  | 'unknown';

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
    case 'text/html':
      return 'html';
    case MULTIPLE_CHOICE_MEDIA_TYPE:
      return 'multiple-choice';
    case IMAGE_MEDIA_TYPE:
      return 'image';
    case CALLOUT_MEDIA_TYPE:
      return 'callout';
    case CONFIRM_MEDIA_TYPE:
      return 'confirm';
    case HITL_REQUEST_MEDIA_TYPE:
      console.log('partKind: hitl-request');
      return 'hitl-request';
    default:
      return contentType?.startsWith('image/') ? 'image' : 'unknown';
  }
}
/**
 * Classifies a part by its `contentType`. This exists as a function rather than a plain `@switch` on the raw media type
 * because images need *prefix* matching: an image attached as `DataContent`/`UriContent` arrives as `image/png`,
 * `image/svg+xml`, and so on. Anything unrecognised is `'unknown'`, which renders nothing — the same forward-compat
 * discipline `chat-stream.service.ts` applies to unknown SSE frame types.
 */
export function HitlControlResolver(name: string | undefined): PartKind {
  switch (name) {
    case 'OwnershipVerificationRequestPort':
      console.log('HitlControlResolver: hitl-request');
      return 'hitl-request';
    default:
      return 'markdown';
  }
}

/**
 * An image to render as a figure. Mirrors the server's `ImageReference`, except that `caption` may also come from the
 * part's `name`, which is how a bare `image/*` part carries one.
 */
export interface ImageReference {
  uri: string;
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

/**
 * A question the workflow is waiting on a human to answer. Most payloads mirror the server's `HumanRequest`:
 * `requestId` correlates the answer back to the port that asked, and `text` is the prompt.
 *
 * Newer payloads can arrive wrapped in a chat-message `data` envelope instead, carrying `contents`, `messageId`
 * and `additionalProperties`. Those fields are surfaced too, and the first textual content becomes `text` so the
 * existing renderer keeps working.
 */
export interface HitlRequest {
  requestId?: string;
  text?: string;
  properties?: Record<string, string>;
  contents?: Record<string, unknown>[];
  messageId?: string;
  additionalProperties?: Record<string, unknown>;
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
 *
 * `name` is the part's own name, which the server lifts from `DataContent.Name`. It is the only way a bare `image/*`
 * part can be captioned, and it fills in for an envelope that carries no caption of its own.
 */
export function parseImage(
  value: string | undefined,
  contentType: string | undefined,
  name?: string,
): ImageReference | null {
  const partName = name?.trim() || undefined;
  if (contentType !== IMAGE_MEDIA_TYPE && contentType?.startsWith('image/')) {
    const uri = value?.trim() ?? '';
    return isRenderableImageUrl(uri) ? { uri, caption: partName } : null;
  }
  const parsed = parseObject<{ uri?: unknown; url?: unknown; alt?: unknown; caption?: unknown }>(value);
  // The field was spelled `url` until it was renamed to `uri`. Message contents are persisted verbatim, so every image
  // part stored before the rename still carries the old spelling — reading both is what keeps those threads rendering.
  const raw = parsed?.uri ?? parsed?.url;
  const uri = typeof raw === 'string' ? raw.trim() : '';
  if (!isRenderableImageUrl(uri)) {
    return null;
  }
  // `alt` and `caption` were two fields until they were collapsed into one, and persisted parts still carry the old
  // pair. Envelope text wins over the part name; within the envelope, the surviving spelling wins over the old one.
  return {
    uri,
    caption: text(parsed?.caption) ?? text(parsed?.alt) ?? partName,
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

/**
 * Reads a human-in-the-loop request out of a part value. Unlike the other parsers this one returns a request for any
 * well-formed JSON object, even one with no prompt: "is an answer owed?" and "does the form render?" have to be the
 * same predicate, or the composer would silently stop attaching answers for a payload the thread still shows.
 */
export function parseHitlRequest(value: string | undefined, fallbackRequestId?: string): HitlRequest | null {
  const parsed = parseObject<Record<string, unknown>>(value);
  if (!parsed) {
    return null;
  }
  // Both casings. `HumanRequest`/`HumanResponse` are the only payloads here with no `[JsonPropertyName]` attributes,
  // and `DataContentExtensions.JsonPart` serialises with the default options rather than the web ones — so they
  // arrive PascalCase while every other payload arrives camelCase. Some newer HITL payloads also arrive wrapped in a
  // chat-message `data` envelope; reading both shapes keeps persisted history and newer messages compatible.
  const payload = plainObject(parsed['Data'] ?? parsed['data']) ?? parsed;
  const contents = objectArray(payload['Contents'] ?? payload['contents']);
  return {
    requestId: text(payload['RequestId']) ?? text(payload['requestId']) ?? text(parsed['RequestId']) ?? text(parsed['requestId']) ?? fallbackRequestId,
    text: text(payload['Text']) ?? text(payload['text']) ?? firstContentText(contents),
    properties: stringMap(payload['Properties'] ?? payload['properties']),
    contents,
    messageId: text(payload['MessageId']) ?? text(payload['messageId']),
    additionalProperties: plainObject(payload['AdditionalProperties'] ?? payload['additionalProperties']),
  };
}

/**
 * Builds the parts of a user turn answering `request`. The readable text leads: it is what the thread and the
 * persisted history show, and it is the part the server's `ChatRequest.Text` facade resolves to — which is what
 * `ChatRequestValidator` validates, so a turn without it is a 400.
 *
 * It must be `text/plain` exactly. `ChatMessagePart.ToAIContent()` promotes only that one media type to
 * `TextContent`. The structured companion is a function-call result whose JSON body also carries `requestId`, while
 * the part-level `requestId` is what the server's `ToAIContent()` uses for correlation.
 */
export function hitlResponseParts(request: HitlRequest, answer: string): IChatMessagePart[] {
  const requestId = request.requestId ?? '';
  const payload = { text: answer, requestId, properties: {} };
  return [
    { value: answer, contentType: 'text/plain', requestId },
    { value: JSON.stringify(payload), contentType: HITL_RESPONSE_MEDIA_TYPE, requestId },
  ];
}

/** The parts of an ordinary, unstructured user turn. */
export function textParts(value: string): IChatMessagePart[] {
  return [{ value, contentType: 'text/plain' }];
}

/**
 * Whether a part carries prose the user's own bubble should print. Everything else on a user turn is structured
 * payload — a HITL answer, an attachment — which would otherwise spill its raw value into the thread.
 *
 * The `text` prefix (not `text/`) mirrors the server's `ChatRequest.Text` facade, so both ends agree on which part
 * of a turn is its text.
 */
export function isTextPart(contentType: string | undefined): boolean {
  return !contentType || contentType.toLowerCase().startsWith('text');
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

/** Narrows a payload member to usable text — anything else, including a blank string, is "not supplied". */
function text(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value : undefined;
}

/** Narrows a payload member to a flat string map, dropping entries whose value is not text. */
function stringMap(value: unknown): Record<string, string> | undefined {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    return undefined;
  }
  const entries = Object.entries(value).filter((entry): entry is [string, string] => typeof entry[1] === 'string');
  return entries.length > 0 ? Object.fromEntries(entries) : undefined;
}

/** Narrows a payload member to a plain JSON object. */
function plainObject(value: unknown): Record<string, unknown> | undefined {
  return typeof value === 'object' && value !== null && !Array.isArray(value) ? (value as Record<string, unknown>) : undefined;
}

/** Narrows a payload member to a list of plain JSON objects, dropping anything else. */
function objectArray(value: unknown): Record<string, unknown>[] | undefined {
  if (!Array.isArray(value)) {
    return undefined;
  }
  const items = value.filter((entry): entry is Record<string, unknown> => typeof entry === 'object' && entry !== null && !Array.isArray(entry));
  return items.length > 0 ? items : undefined;
}

/** Reads the first usable text-bearing content item, which is what the HITL input can label itself with. */
function firstContentText(contents: Record<string, unknown>[] | undefined): string | undefined {
  if (!contents) {
    return undefined;
  }
  for (const content of contents) {
    const value = text(content['Text']) ?? text(content['text']);
    if (value) {
      return value;
    }
  }
  return undefined;
}
