import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import { DexApiService, IChatStreamEvent, SessionListItem } from '../../core/services/dex-api.service';
import { ChatStreamService } from '../../core/services/chat-stream.service';
import { ChatComposerComponent } from './chat-composer.component';
import { ChatSidebarComponent } from './chat-sidebar.component';
import { ChatThreadComponent } from './chat-thread.component';
import { ThreadMessage, toThreadMessage } from './chat.models';

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

  protected readonly sessions = signal<SessionListItem[]>([]);
  protected readonly sessionsLoading = signal(false);
  protected readonly activeSessionId = signal<string | null>(null);
  protected readonly messages = signal<ThreadMessage[]>([]);
  protected readonly threadLoading = signal(false);

  protected readonly isStreaming = signal(false);
  protected readonly streamingText = signal('');
  protected readonly currentStep = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly questionsTotal = signal<number | null>(null);

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
    this.streamingText.set('');
    this.currentStep.set(null);
    this.dex
      .getChatSession(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          this.activeSessionId.set(id);
          this.messages.set((session.messages ?? []).map(toThreadMessage));
          this.questionsTotal.set(session.questionsTotal ?? null);
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
    this.streamingText.set('');
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

  protected send(text: string): void {
    const value = text.trim();
    if (!value || this.isStreaming()) {
      return;
    }
    this.cancelStream();
    this.error.set(null);
    this.messages.update((list) => [...list, { role: 'User', content: value, createdAt: new Date() }]);
    this.isStreaming.set(true);
    this.streamingText.set('');
    this.currentStep.set('Working…');

    const sessionId = this.activeSessionId();
    const stream$ = sessionId
      ? this.streamSvc.streamMessage(sessionId, value)
      : this.streamSvc.streamCreate(value);

    this.streamSub = stream$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (event) => this.onEvent(event),
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

  private onEvent(event: IChatStreamEvent): void {
    switch (event.type) {
      case 'step':
        this.currentStep.set(event.step ?? null);
        break;
      case 'delta':
        this.streamingText.update((text) => text + (event.text ?? ''));
        break;
      case 'complete':
        this.finalize(event);
        break;
    }
  }

  private finalize(event: IChatStreamEvent): void {
    const answer = event.answer ?? this.streamingText();
    if (event.failed) {
      this.error.set(event.failureReason ?? 'The assistant could not complete the answer.');
    }
    if (answer) {
      this.messages.update((list) => [
        ...list,
        { role: 'Assistant', content: answer, createdAt: new Date(), citations: event.citations ?? [] },
      ]);
    }
    if (!this.activeSessionId() && event.sessionId) {
      this.activeSessionId.set(event.sessionId);
    }
    this.questionsTotal.set(event.questionsTotal ?? null);
    this.isStreaming.set(false);
    this.streamingText.set('');
    this.currentStep.set(null);
    // Refresh the rail so the new/updated session and its title appear in order.
    this.loadSessions();
  }

  private finalizeIfStreaming(): void {
    if (!this.isStreaming()) {
      return;
    }
    const answer = this.streamingText();
    if (answer) {
      this.messages.update((list) => [...list, { role: 'Assistant', content: answer, createdAt: new Date() }]);
    }
    this.isStreaming.set(false);
    this.streamingText.set('');
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
