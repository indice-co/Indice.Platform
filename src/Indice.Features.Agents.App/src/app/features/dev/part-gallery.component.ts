import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { ChatMessageContent, ChatMessagePart } from '../../core/services/dex-api.service';
import { ChatThreadComponent } from '../chat/chat-thread.component';
import { ThreadMessage } from '../chat/chat.models';
import {
  CALLOUT_MEDIA_TYPE,
  CONFIRM_MEDIA_TYPE,
  IMAGE_MEDIA_TYPE,
  MULTIPLE_CHOICE_MEDIA_TYPE,
} from '../chat/parts/part-contracts';

/**
 * A fixture thread exercising every renderable content part, rendered through the real `ChatThreadComponent` so what
 * you see is what an answer looks like. Unlisted and unguarded: it calls no API and holds no data, which is what makes
 * it usable without signing in and harmless if it ships.
 *
 * This is the styling harness the automated specs cannot be — the specs prove the parsers and the wiring, this proves
 * the layout. Reach for it when adding a media type, changing part chrome, or checking a narrow viewport. Its images
 * are inline data URIs so it renders identically offline.
 */
@Component({
  selector: 'app-part-gallery',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChatThreadComponent],
  template: `
    <div class="flex h-dvh flex-col bg-base-200">
      <header class="border-b border-base-300 bg-base-100 px-4 py-2">
        <h1 class="text-sm font-semibold">Content part gallery</h1>
        <p class="text-xs text-base-content/55">Every renderable part type. Not part of the app — no API, no auth.</p>
      </header>
      <div class="min-h-0 flex-1">
        <app-chat-thread [messages]="messages" [questionsTotal]="5" (pick)="picked.set($event)" />
      </div>
      <footer class="border-t border-base-300 bg-base-100 px-4 py-2 font-mono text-xs text-base-content/60">
        last pick: {{ picked() || '—' }}
      </footer>
    </div>
  `,
})
export class PartGalleryComponent {
  /** Echoes what an interactive part emitted, standing in for the send the real chat page would do. */
  protected readonly picked = signal('');

  protected readonly messages: ThreadMessage[] = [
    user('Show me everything you can render.'),
    assistant([
      part('text/markdown', 'Prose renders as **markdown** in its own bubble — the tail corner marks the first part.'),
      part(IMAGE_MEDIA_TYPE, json({
        url: 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIzMjAiIGhlaWdodD0iMTUwIj48cmVjdCB3aWR0aD0iMzIwIiBoZWlnaHQ9IjE1MCIgZmlsbD0iI2UxMWQyZiIvPjx0ZXh0IHg9IjE2MCIgeT0iODIiIGZvbnQtZmFtaWx5PSJzYW5zLXNlcmlmIiBmb250LXNpemU9IjE5IiBmaWxsPSIjZmZmIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj5lbnJvbG1lbnQgZmxvdzwvdGV4dD48L3N2Zz4=',
        alt: 'A red panel labelled enrolment flow',
        caption: 'Figure 1 — an image+json part, with a caption',
      })),
      part('text/markdown', 'A part after an image opens a *new* bubble rather than merging back into the first.'),
      part('image/svg+xml', 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyMDAiIGhlaWdodD0iOTAiPjxyZWN0IHdpZHRoPSIyMDAiIGhlaWdodD0iOTAiIGZpbGw9IiMxZjI5MzciLz48dGV4dCB4PSIxMDAiIHk9IjUyIiBmb250LWZhbWlseT0ibW9ub3NwYWNlIiBmb250LXNpemU9IjE0IiBmaWxsPSIjOWNhM2FmIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj5yYXcgaW1hZ2Uvc3ZnK3htbDwvdGV4dD48L3N2Zz4='),
    ]),
    user('And the notices?'),
    assistant([
      part(CALLOUT_MEDIA_TYPE, json({ severity: 'info', title: 'Heads up', text: 'An info callout, with a title.' })),
      part(CALLOUT_MEDIA_TYPE, json({ severity: 'success', text: 'A success callout with no title.' })),
      part(CALLOUT_MEDIA_TYPE, json({ severity: 'warning', text: 'A warning callout.\nLine breaks are preserved.' })),
      part(CALLOUT_MEDIA_TYPE, json({ severity: 'error', title: 'Blocked', text: 'An error callout.' })),
      part(CALLOUT_MEDIA_TYPE, json({ severity: 'nonsense', text: 'An unknown severity falls back to info.' })),
    ]),
    user('What about the ones I can click?'),
    assistant([
      part('text/markdown', 'The parts below are interactive because this is the last message in the thread.'),
      part(MULTIPLE_CHOICE_MEDIA_TYPE, json({
        options: ['What can you tell me about policy?', 'What can you tell me about identity?', 'Something else'],
      })),
      part(CONFIRM_MEDIA_TYPE, json({ prompt: 'Should I look that up?', confirmText: 'Yes, go ahead', cancelText: 'No thanks' })),
      part('text/markdown', 'Everything below renders nothing, and must leave **no** empty box and no double gap:'),
      part('application/vnd.indice.not-invented-yet+json', '{"whatever":1}'),
      part(MULTIPLE_CHOICE_MEDIA_TYPE, 'not json'),
      part(IMAGE_MEDIA_TYPE, json({ url: 'javascript:alert(1)' })),
      part(IMAGE_MEDIA_TYPE, json({ url: 'https://example.invalid/gone.png', caption: 'A dead URL' })),
      part(CALLOUT_MEDIA_TYPE, json({ severity: 'info' })),
    ]),
  ];
}

function part(contentType: string, value: string): ChatMessagePart {
  return new ChatMessagePart({ contentType, value });
}

function json(payload: unknown): string {
  return JSON.stringify(payload);
}

function user(text: string): ThreadMessage {
  return { role: 'User', content: new ChatMessageContent({ parts: [part('text/markdown', text)] }) };
}

function assistant(parts: ChatMessagePart[]): ThreadMessage {
  return { role: 'Assistant', content: new ChatMessageContent({ parts }) };
}
