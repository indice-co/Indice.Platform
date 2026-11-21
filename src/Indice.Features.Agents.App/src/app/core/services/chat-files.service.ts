import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { DEX_API_BASE_URL } from './dex-api.service';

/**
 * Downloads the files an agent produced in a conversation, such as a generated report.
 *
 * It goes through `HttpClient` and not through a plain link on purpose: the download endpoint needs the bearer token, which
 * an `<a href>` cannot send and which must never be put in a URL. The interceptor attaches it, the response comes back as a
 * `Blob`, and the component saves that.
 */
@Injectable({ providedIn: 'root' })
export class ChatFilesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = (inject(DEX_API_BASE_URL, { optional: true }) ?? 'https://localhost:2001').replace(/\/$/, '');

  /** GET /my/chats/{chatId}/files/{fileId} — the file's bytes. A file the user does not own is a 404. */
  download(conversationId: string, fileId: string): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/my/chats/${encodeURIComponent(conversationId)}/files/${encodeURIComponent(fileId)}`,
      { responseType: 'blob' },
    );
  }
}
