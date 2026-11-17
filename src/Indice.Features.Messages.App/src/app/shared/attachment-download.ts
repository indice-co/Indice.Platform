/**
 * Saving of attachments (inbox messages, campaigns) that are downloaded through the authenticated http client.
 * Every attachment is saved as an opaque file download and is never rendered by the browser: a blob URL inherits the origin
 * of the page that creates it, so rendering one would let its content run as a page of the application.
 */

const DEFAULT_FILE_NAME = 'attachment';
/** Generous enough for the browser to start the download before the object URL is released. */
export const ATTACHMENT_URL_LIFETIME_MS = 60_000;

/**
 * Resolves a (host relative, e.g. `/api/campaigns/attachments/{id}.pdf`) file link of the api against the api address, which
 * itself may be absolute (`https://host/api`) or relative to the app (`/api`). Returns `undefined` for links of other hosts,
 * so that the access token is never sent to them.
 */
export function resolveApiFileUrl(link: string, apiUrl: string | null | undefined, origin: string = window.location.origin): string | undefined {
  try {
    const api = new URL(apiUrl || origin, origin);
    const url = new URL(link, api);
    return url.origin === api.origin ? url.href : undefined;
  } catch {
    return undefined;
  }
}

/** The file name of an attachment url, i.e. the last path segment (`{fileGuid}.{format}`). */
export function attachmentFileName(url: string): string {
  try {
    const name = new URL(url, window.location.origin).pathname.split('/').pop();
    return name ? decodeURIComponent(name) : DEFAULT_FILE_NAME;
  } catch {
    return DEFAULT_FILE_NAME;
  }
}

/** Saves a downloaded attachment as a file, whatever its type. */
export function saveAttachment(blob: Blob, fileName: string, doc: Document = document): void {
  const url = URL.createObjectURL(new Blob([blob], { type: 'application/octet-stream' }));
  const link = doc.createElement('a');
  link.style.display = 'none';
  link.href = url;
  link.download = fileName || DEFAULT_FILE_NAME;
  link.rel = 'noopener';
  doc.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), ATTACHMENT_URL_LIFETIME_MS);
}
