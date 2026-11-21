import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';

import { ChatFilesService } from '../../../core/services/chat-files.service';
import { FileReference, formatFileSize } from './part-contracts';

/**
 * Renders a file part as a card with a download button: the file name, its size, and the start of its SHA-256 so the reader
 * can check a downloaded copy against what the assistant produced.
 *
 * The bytes are fetched through the authenticated HTTP client and saved from a temporary object URL; the link is never a
 * plain `href` to the API, because that request would carry no token. The button stays disabled until the conversation has an
 * id (a streaming first turn only learns it from its `start` frame), and a failed download says so instead of failing silently.
 */
@Component({
  selector: 'app-chat-file',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (file(); as f) {
      <div class="flex w-full items-center gap-3 rounded-box border border-base-300 bg-base-100 px-4 py-3 shadow-sm">
        <svg viewBox="0 0 24 24" fill="none" class="size-8 shrink-0 text-primary" aria-hidden="true">
          <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round" />
          <path d="M14 3v5h5M9 13h6M9 17h6" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        <div class="min-w-0 flex-1">
          <div class="truncate font-medium" [title]="f.fileName">{{ f.fileName }}</div>
          @if (details(); as info) {
            <div class="truncate text-xs text-base-content/60">{{ info }}</div>
          }
          @if (error(); as message) {
            <div role="alert" class="mt-1 text-xs text-error">{{ message }}</div>
          }
        </div>
        <button
          type="button"
          class="btn btn-primary btn-sm shrink-0"
          [disabled]="busy() || !conversationId()"
          [attr.aria-label]="'Download ' + f.fileName"
          (click)="download()"
        >
          @if (busy()) {
            <span class="loading loading-spinner loading-xs"></span>
          }
          Download
        </button>
      </div>
    }
  `,
})
export class ChatFileComponent {
  private readonly files = inject(ChatFilesService);

  /** The file to offer, or null when the part carried nothing renderable. */
  readonly file = input<FileReference | null>(null);
  /** The conversation the file belongs to; the download URL is built from it. */
  readonly conversationId = input<string | null>(null);

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly details = computed(() => {
    const f = this.file();
    if (!f) {
      return '';
    }
    const parts = [formatFileSize(f.length)];
    if (f.sha256) {
      parts.push(`SHA-256 ${f.sha256.slice(0, 12)}…`);
    }
    return parts.filter(Boolean).join(' · ');
  });

  protected download(): void {
    const file = this.file();
    const conversationId = this.conversationId();
    if (!file || !conversationId || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.files.download(conversationId, file.id).subscribe({
      next: (blob) => {
        saveBlob(blob, file.fileName, file.contentType);
        this.busy.set(false);
      },
      error: () => {
        this.error.set('The file could not be downloaded. Try again in a moment.');
        this.busy.set(false);
      },
    });
  }
}

/**
 * Saves a blob under a file name through a temporary object URL. The URL is released later, not straight after the click: some
 * browsers start reading it after the click handler returns, and revoking it first cancels the download.
 */
function saveBlob(blob: Blob, fileName: string, contentType: string): void {
  const url = URL.createObjectURL(blob.type ? blob : new Blob([blob], { type: contentType }));
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.rel = 'noopener';
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 30_000);
}
