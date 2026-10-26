/**
 * Types a browser renders without running script. A blob URL inherits the app's origin, so anything else
 * (SVG, HTML, XML, ...) could run uploaded script with access to the app's storage and is downloaded instead.
 */
const PREVIEWABLE_TYPES = new Set(['application/pdf', 'image/png', 'image/jpeg', 'image/gif', 'image/webp', 'image/bmp']);

/** How long a preview's object URL stays valid; the new tab has read the blob long before this. */
const PREVIEW_URL_LIFETIME_MS = 60_000;

function mediaType(contentType: string | undefined): string {
  return (contentType ?? '').split(';')[0].trim().toLowerCase();
}

/** Whether an attachment of this content type is opened in a new tab rather than downloaded. */
export function canPreview(contentType: string | undefined): boolean {
  return PREVIEWABLE_TYPES.has(mediaType(contentType));
}

/** Saves the blob to disk under the given file name. */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}

/**
 * Opens the blob in a new tab when its type is in the inert allowlist, otherwise saves it. The object URL is
 * released once the tab has had time to load it, or right away (falling back to a download) when the popup is blocked.
 */
export function previewBlob(blob: Blob, fileName: string): void {
  if (!canPreview(blob.type)) {
    saveBlob(blob, fileName);
    return;
  }
  const url = URL.createObjectURL(blob);
  if (!window.open(url, '_blank')) {
    URL.revokeObjectURL(url);
    saveBlob(blob, fileName);
    return;
  }
  setTimeout(() => URL.revokeObjectURL(url), PREVIEW_URL_LIFETIME_MS);
}
