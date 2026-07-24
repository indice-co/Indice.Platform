import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import { ChatMessagePart, ConversationListItem, DexApiService, DexChatResponse, LikeRequest } from '../../core/services/dex-api.service';
import { ChatStreamFrame, ChatStreamService } from '../../core/services/chat-stream.service';
import { JsonPointerPatch } from '../../core/services/json-pointer-patch';
import { ChatComposerComponent } from './chat-composer.component';
import { ChatSidebarComponent } from './chat-sidebar.component';
import { ChatThreadComponent } from './chat-thread.component';
import { ThreadMessage, responseToThreadMessage, toThreadMessage } from './chat.models';

/** The Dex chat surface: session rail + conversation thread + composer, wired to the streaming API. */
@Component({
  selector: 'app-chat-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChatSidebarComponent, ChatThreadComponent, ChatComposerComponent],
  templateUrl: './chat-page.component.html',
})
export class ChatPageComponent {
  private readonly dex = inject(DexApiService);
  private readonly streamSvc = inject(ChatStreamService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly sessions = signal<ConversationListItem[]>([]);
  protected readonly sessionsLoading = signal(false);
  protected readonly activeSessionId = signal<string | null>(null);
  protected readonly messages = signal<ThreadMessage[]>([]);
  protected readonly threadLoading = signal(false);

  protected readonly isStreaming = signal(false);
  /** The DexChatResponse the stream is assembling — the invariant says the patched document IS one. */
  protected readonly streamResponse = signal<DexChatResponse | null>(null);
  protected readonly streamingMessage = computed(() => responseToThreadMessage(this.streamResponse()));
  protected readonly currentStep = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly questionsTotal = signal<number | null>(null);

  /** The raw patch target for the turn's `delta` frames — plain JSON; `streamResponse` is its typed projection. */
  private streamDocument: Record<string, any> = {};
  private patcher = new JsonPointerPatch();
  private streamSub?: Subscription;

  constructor() {
    this.loadSessions();
  }

  protected selectSession(id: string): void {
    if (id === this.activeSessionId() || this.threadLoading()) {
      return;
    }
    this.cancelStream();
    this.threadLoading.set(true);
    this.error.set(null);
    this.streamResponse.set(null);
    this.currentStep.set(null);
    this.dex
      .getChatSession(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          this.activeSessionId.set(id);
          this.messages.set((session.messages ?? []).map(toThreadMessage));
          this.questionsTotal.set(session.usage?.questionsLimitCount ?? null);
          this.threadLoading.set(false);
        },
        error: () => {
          this.threadLoading.set(false);
          this.error.set('Could not load this conversation.');
        },
      });
  }

  protected newChat(): void {
    this.cancelStream();
    this.activeSessionId.set(null);
    this.messages.set([]);
    this.streamResponse.set(null);
    this.currentStep.set(null);
    this.error.set(null);
    this.isStreaming.set(false);
    this.questionsTotal.set(null);
  }

  protected deleteSession(id: string): void {
    this.dex
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.sessions.update((list) => list.filter((s) => s.id !== id));
          if (this.activeSessionId() === id) {
            this.newChat();
          }
        },
        error: () => this.error.set('Could not delete the conversation.'),
      });
  }

  protected setLike(change: { messageId: string; like: boolean | null }): void {
    const sessionId = this.activeSessionId();
    if (!sessionId) {
      return;
    }
    const previous = this.messages();
    // Optimistic: reflect the rating immediately, roll back if the server rejects it.
    this.messages.update((list) =>
      list.map((message) => (message.messageId === change.messageId ? { ...message, liked: change.like } : message)),
    );
    this.dex
      .like(sessionId, change.messageId, new LikeRequest({ like: change.like ?? undefined }))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          this.messages.set(previous);
          this.error.set('Could not save your feedback.');
        },
      });
  }

  protected send(text: string): void {
    const value = text.trim();
    if (!value || this.isStreaming()) {
      return;
    }
    this.cancelStream();
    this.error.set(null);
    this.messages.update((list) => [...list, { role: 'User', content: { parts: [new ChatMessagePart({ value: value, contentType: 'text/markdown' })] }, createdAt: new Date() }]);
    this.isStreaming.set(true);
    this.streamResponse.set(null);
    this.currentStep.set('Working…');
    this.streamDocument = {};
    this.patcher = new JsonPointerPatch();

    const sessionId = this.activeSessionId();
    const stream$ = sessionId
      ? this.streamSvc.streamMessage(sessionId, value)
      : this.streamSvc.streamCreate(value);

    this.streamSub = stream$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (frame) => this.onFrame(frame),
      error: (err: unknown) => {
        this.isStreaming.set(false);
        this.currentStep.set(null);
        this.error.set(err instanceof Error ? err.message : 'The request failed.');
      },
      complete: () => this.finalizeIfStreaming(),
    });
  }

  protected stop(): void {
    this.cancelStream();
    this.finalizeIfStreaming();
  }

  private onFrame(frame: ChatStreamFrame): void {
    switch (frame.type) {
      case 'start':
        if (!this.activeSessionId() && frame.conversationId) {
          this.activeSessionId.set(frame.conversationId);
        }
        break;
      case 'status':
        this.currentStep.set(frame.value ?? null);
        break;
      case 'delta':
        this.patcher.apply(this.streamDocument, frame);
        this.streamResponse.set(DexChatResponse.fromJS(this.streamDocument));
        break;
      case 'error':
        // The document is abandoned per protocol — the server persisted no answer.
        this.error.set(frame.reason ?? 'The assistant could not complete the answer.');
        this.resetStreamingState();
        break;
      case 'done':
        this.finalize();
        break;
    }
  }

  /** Terminal success: the assembled document is complete — settle it into the thread. */
  private finalize(): void {
    const answer = this.streamingMessage();
    if (answer) {
      this.messages.update((list) => [...list, answer]);
    }
    this.questionsTotal.set(this.streamResponse()?.usage?.questionsLimitCount ?? null);
    this.resetStreamingState();
    // Refresh the rail so the new/updated session and its title appear in order.
    this.loadSessions();
  }

  /** Stream ended without `done` (stop pressed / connection closed): keep the partial answer visible. */
  private finalizeIfStreaming(): void {
    if (!this.isStreaming()) {
      return;
    }
    const partial = this.streamingMessage();
    if (partial?.content.parts?.some((part) => part.value)) {
      this.messages.update((list) => [...list, partial]);
    }
    this.resetStreamingState();
  }

  private resetStreamingState(): void {
    this.isStreaming.set(false);
    this.streamResponse.set(null);
    this.currentStep.set(null);
  }

  private loadSessions(): void {
    this.sessionsLoading.set(true);
    this.dex
      .list(1, 100, null, null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.sessions.set(result.items ?? []);
          this.sessionsLoading.set(false);
        },
        error: () => this.sessionsLoading.set(false),
      });
  }

  private cancelStream(): void {
    this.streamSub?.unsubscribe();
    this.streamSub = undefined;
  }
}
