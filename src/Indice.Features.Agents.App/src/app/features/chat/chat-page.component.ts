import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import { AgentInfo, ChatMessagePart, DexApiService, DexChatResponse, LikeRequest } from '../../core/services/dex-api.service';
import { ChatStreamFrame, ChatStreamService } from '../../core/services/chat-stream.service';
import { ConversationsStore } from '../../core/services/conversations.store';
import { JsonPointerPatch } from '../../core/services/json-pointer-patch';
import { ChatComposerComponent } from './chat-composer.component';
import { ChatThreadComponent } from './chat-thread.component';
import { EXAMPLE_PROMPTS, ThreadMessage, responseToThreadMessage, toThreadMessage } from './chat.models';

/**
 * The Dex chat surface: conversation thread + composer, wired to the streaming API.
 *
 * Which conversation is open is owned by `ConversationsStore` — the rail (in the shell) sets it,
 * this page renders it. `loadedId` records the thread actually fetched so a session the stream
 * just created is adopted without a redundant round-trip.
 */
@Component({
  selector: 'app-chat-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChatThreadComponent, ChatComposerComponent],
  templateUrl: './chat-page.component.html',
})
export class ChatPageComponent {
  private readonly dex = inject(DexApiService);
  private readonly streamSvc = inject(ChatStreamService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly store = inject(ConversationsStore);

  protected readonly messages = signal<ThreadMessage[]>([]);
  protected readonly threadLoading = signal(false);

  protected readonly examplePrompts = EXAMPLE_PROMPTS;
  /** A brand-new, unsent chat — the hero and composer render centered, ChatGPT-style. */
  protected readonly isEmptySession = computed(
    () =>
      this.messages().length === 0 &&
      !this.isStreaming() &&
      !this.threadLoading() &&
      !this.store.activeId(),
  );

  protected readonly isStreaming = signal(false);
  /** The DexChatResponse the stream is assembling — the invariant says the patched document IS one. */
  protected readonly streamResponse = signal<DexChatResponse | null>(null);
  protected readonly streamingMessage = computed(() => responseToThreadMessage(this.streamResponse()));
  protected readonly currentStep = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly questionsTotal = signal<number | null>(null);

  /** The modes discovered from GET /agents; empty (picker hidden) when discovery fails. */
  protected readonly agents = signal<AgentInfo[]>([]);
  /** The composer's picked mode; `null` falls back to the first discovered agent. */
  protected readonly selectedAgentName = signal<string | null>(null);

  /** The raw patch target for the turn's `delta` frames — plain JSON; `streamResponse` is its typed projection. */
  private streamDocument: Record<string, any> = {};
  private patcher = new JsonPointerPatch();
  private streamSub?: Subscription;
  private threadSub?: Subscription;
  /** The conversation whose thread is on screen — `null` for an unsent new chat. */
  private loadedId: string | null = null;

  constructor() {
    effect(() => {
      const id = this.store.activeId();
      if (id === this.loadedId) {
        // Already on screen, or a session this page just streamed into existence.
        return;
      }
      this.loadedId = id;
      if (id) {
        this.loadThread(id);
      } else {
        this.resetThread();
      }
    });
    this.loadAgents();
  }

  protected setLike(change: { messageId: string; like: boolean | null }): void {
    const sessionId = this.store.activeId();
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

    const sessionId = this.store.activeId();
    const agentName = this.selectedAgentName() ?? this.agents()[0]?.name ?? null;
    const stream$ = sessionId
      ? this.streamSvc.streamMessage(sessionId, value, agentName)
      : this.streamSvc.streamCreate(value, agentName);

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
        if (!this.store.activeId() && frame.conversationId) {
          // Claim the id before publishing it, so the effect sees no change and skips the fetch.
          this.loadedId = frame.conversationId;
          this.store.adopt(frame.conversationId);
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
    this.store.refresh();
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

  /** Fetch and show an existing conversation, superseding any load still in flight. */
  private loadThread(id: string): void {
    this.cancelStream();
    this.threadSub?.unsubscribe();
    this.threadLoading.set(true);
    this.error.set(null);
    this.streamResponse.set(null);
    this.currentStep.set(null);
    this.isStreaming.set(false);
    this.threadSub = this.dex
      .getChatSession(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
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

  /** Clear the surface for an unsent new chat. */
  private resetThread(): void {
    this.cancelStream();
    this.threadSub?.unsubscribe();
    this.threadSub = undefined;
    this.messages.set([]);
    this.streamResponse.set(null);
    this.currentStep.set(null);
    this.error.set(null);
    this.isStreaming.set(false);
    this.threadLoading.set(false);
    this.questionsTotal.set(null);
  }

  private loadAgents(): void {
    this.dex
      .discovery()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (agents) => this.agents.set(agents ?? []),
        // Non-fatal: without a modes list the picker stays hidden and requests carry no agentName.
        error: () => this.agents.set([]),
      });
  }

  private cancelStream(): void {
    this.streamSub?.unsubscribe();
    this.streamSub = undefined;
  }
}
