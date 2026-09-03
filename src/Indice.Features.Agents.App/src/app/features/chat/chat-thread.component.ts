import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterRenderEffect,
  computed,
  input,
  output,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';

import { ChatCitationsComponent } from './chat-citations.component';
import { ChatMessagePartComponent } from './chat-message-part.component';
import { EXAMPLE_PROMPTS, ThreadMessage } from './chat.models';

/** The scrolling conversation: message bubbles, live streaming answer, citations and empty state. */
@Component({
  selector: 'app-chat-thread',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ChatCitationsComponent, ChatMessagePartComponent],
  template: `
    <div #scroller class="dex-scroll dex-canvas h-full overflow-y-auto">
      <div class="mx-auto w-full max-w-3xl px-4 py-8 sm:px-6">
        @if (busy()) {
          <div class="flex h-64 items-center justify-center">
            <span class="loading loading-ring loading-lg text-primary/60"></span>
          </div>
        } @else if (messages().length === 0 && !streaming()) {
          <!-- Empty canvas -->
          <div class="flex flex-col items-center px-4 pt-12 text-center sm:pt-20">
            <img src="dex-logo.png" alt="Dex" class="size-16 drop-shadow-sm" />
            <h1 class="mt-6 text-4xl font-semibold tracking-tight text-base-content">
              Ask Dex
            </h1>
            <p class="mt-3 max-w-md text-base-content/55">
              Grounded answers from your knowledge base — with citations you can trace.
            </p>
            <div class="mt-8 flex flex-wrap justify-center gap-2">
              @for (prompt of examplePrompts; track prompt) {
                <button
                  type="button"
                  class="rounded-selector border border-base-300 bg-base-100 px-4 py-2 text-sm
                         text-base-content/75 shadow-sm transition hover:border-primary/40
                         hover:text-base-content"
                  (click)="examplePick.emit(prompt)"
                >
                  {{ prompt }}
                </button>
              }
            </div>
          </div>
        } @else {
          <div class="flex flex-col gap-6">
            @for (turn of turns(); track $index) {
              @if (turn.message.role === 'User') {
                <div class="dex-rise flex justify-end">
                  <div
                    class="max-w-[85%] whitespace-pre-wrap break-words rounded-box rounded-br-sm
                           bg-primary px-4 py-2.5 text-[0.95rem] leading-relaxed text-primary-content
                           shadow-sm"
                  >
                    @for (contentPart of turn.message.content.parts; track $index) {
                    <div class="{{contentPart.contentType}}">
                    {{ contentPart.value }}
                    </div>
                    }
                  </div>
                </div>
              } @else {
                <div class="group dex-rise flex gap-3">
                  <img
                    src="dex-logo.png"
                    alt="Dex"
                    class="mt-0.5 size-8 shrink-0 rounded-full ring-1 ring-base-300"
                  />
                  <div class="min-w-0 flex-1">
                    <!-- One block per part, spaced: still one answer, but an image or an options row is not welded into the prose card. -->
                    <div class="flex flex-col gap-3">
                      @for (contentPart of turn.message.content.parts; track $index) {
                        <app-chat-message-part
                          [part]="contentPart"
                          [first]="$first"
                          [interactive]="turn.isLatest && !streaming()"
                          (pick)="pick.emit($event)"
                        />
                      }
                    </div>

                            @if ((turn.message.citations ?? []).length > 0 || turn.message.messageId) {
                              <app-chat-citations [citations]="turn.message.citations ?? []">
                                @if (turn.message.messageId) {
                                  <!-- -mr compensates the circle buttons' padding so the icons end flush with the counter. -->
                                  <div
                                    class="-mr-[5px] flex items-center gap-0.5 transition"
                                    [ngClass]="turn.message.liked != null ? 'opacity-100' : 'opacity-0 focus-within:opacity-100 group-hover:opacity-100'"
                                  >
                                    <button
                                      type="button"
                                      class="btn btn-ghost btn-xs btn-circle"
                                      [class]="turn.message.liked === true ? 'text-primary' : 'text-base-content/40 hover:text-primary'"
                                      (click)="toggleLike(turn.message, true)"
                                      [attr.aria-pressed]="turn.message.liked === true"
                                      aria-label="Good answer"
                                      title="Good answer"
                                    >
                                      <svg viewBox="0 0 24 24" fill="none" class="size-3.5" aria-hidden="true">
                                        <path
                                          d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"
                                          stroke="currentColor"
                                          stroke-width="2"
                                          stroke-linecap="round"
                                          stroke-linejoin="round"
                                        />
                                      </svg>
                                    </button>
                                    <button
                                      type="button"
                                      class="btn btn-ghost btn-xs btn-circle"
                                      [class]="turn.message.liked === false ? 'text-error' : 'text-base-content/40 hover:text-error'"
                                      (click)="toggleLike(turn.message, false)"
                                      [attr.aria-pressed]="turn.message.liked === false"
                                      aria-label="Bad answer"
                                      title="Bad answer"
                                    >
                                      <svg viewBox="0 0 24 24" fill="none" class="size-3.5" aria-hidden="true">
                                        <path
                                          d="M10 15v4a3 3 0 0 0 3 3l4-9V2H5.72a2 2 0 0 0-2 1.7l-1.38 9a2 2 0 0 0 2 2.3zM17 2h3a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2h-3"
                                          stroke="currentColor"
                                          stroke-width="2"
                                          stroke-linecap="round"
                                          stroke-linejoin="round"
                                        />
                                      </svg>
                                    </button>
                                  </div>
                                }
                              </app-chat-citations>
                            }
                            @if (turn.questionNumber !== null && questionsTotal() !== null) {
                              <div
                                class="mt-1 flex items-center justify-end gap-1.5 font-mono text-sm
                                       tabular-nums text-base-content/45"
                                title="Questions used in this conversation"
                              >
                                <span
                                  class="inline-block size-2.5 rounded-full"
                                  [style.background-color]="turn.dotColor"
                                ></span>
                                {{ turn.questionNumber }}/{{ questionsTotal() }}
                              </div>
                            }
                </div>
                </div>
              }
            }

            @if (streaming()) {
              <div class="dex-rise flex gap-3">
                <img
                  src="dex-logo.png"
                  alt="Dex"
                  class="mt-0.5 size-8 shrink-0 rounded-full ring-1 ring-base-300"
                />
                <div class="min-w-0 flex-1">
                  @if (step() && !hasStreamContent()) {
                    <div
                      class="inline-flex items-center gap-2 rounded-selector bg-base-100 px-3 py-1.5
                             text-sm text-base-content/60 ring-1 ring-base-300"
                    >
                      <span class="loading loading-dots loading-xs text-primary"></span>
                      {{ step() }}
                    </div>
                  } @else if (streamingMessage(); as live) {
                    <div class="flex flex-col gap-3">
                      <!-- Nothing on a still-streaming answer is actionable yet — interactive defaults to false. -->
                      @for (contentPart of live.content.parts; track $index) {
                        <app-chat-message-part [part]="contentPart" [first]="$first" [caret]="$last" />
                      }
                    </div>
                    @if ((live.citations ?? []).length > 0) {
                      <app-chat-citations [citations]="live.citations ?? []" />
                    }
                  }
                </div>
              </div>
            }

            @if (error()) {
              <div role="alert" class="alert alert-error border-0 text-sm shadow-sm">
                <svg viewBox="0 0 24 24" fill="none" class="size-5 shrink-0" aria-hidden="true">
                  <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="2" />
                  <path d="M12 8v5M12 16h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                </svg>
                <span>{{ error() }}</span>
              </div>
            }
          </div>
        }

        <div #anchor></div>
      </div>
    </div>
  `,
})
export class ChatThreadComponent {
  readonly messages = input<ThreadMessage[]>([]);
  readonly streaming = input(false);
  readonly streamingMessage = input<ThreadMessage | null>(null);
  readonly step = input<string | null>(null);
  readonly error = input<string | null>(null);
  readonly busy = input(false);
  readonly questionsTotal = input<number | null>(null);

  readonly examplePick = output<string>();
  /** Emits text the user picked from an interactive part (an option, a confirmation button), to be sent as the next user message. */
  readonly pick = output<string>();
  /** Emits when the user rates an assistant answer: `like` true/false, or null to clear the rating. */
  readonly likeChanged = output<{ messageId: string; like: boolean | null }>();

  protected readonly examplePrompts = EXAMPLE_PROMPTS;

  /** Like/dislike toggle — clicking the active thumb again clears the rating. */
  protected toggleLike(message: ThreadMessage, like: boolean): void {
    if (!message.messageId) {
      return;
    }
    this.likeChanged.emit({ messageId: message.messageId, like: message.liked === like ? null : like });
  }

  /** Whether the streaming answer has any visible content yet — until then the step chip shows. */
  protected readonly hasStreamContent = computed(
    () => this.streamingMessage()?.content.parts?.some((part) => part.value) ?? false,
  );

  /**
   * Messages annotated with usage: only the latest assistant answer carries the counter — the
   * questions used so far, clamped to the total so the non-persisted limit-reached reply reads as
   * the cap (5/5, not 6/5). The dot hue runs green (fresh) → red (at the cap): 120 → 0 by used/total.
   *
   * `isLatest` marks the last message in the thread, which is what keeps an interactive part (a
   * multiple-choice list) actionable: picking an option appends a user message, so the list stops
   * being last and disables itself — no per-part "spent" flag to track or persist.
   */
  protected readonly turns = computed(() => {
    const total = this.questionsTotal();
    const messages = this.messages();
    let lastAssistantIndex = -1;
    let answered = 0;
    messages.forEach((message, index) => {
      if (message.role === 'Assistant') {
        lastAssistantIndex = index;
        answered++;
      }
    });
    return messages.map((message, index) => {
      const isLatest = index === messages.length - 1;
      if (index !== lastAssistantIndex || total === null || total === 0) {
        return { message, isLatest, questionNumber: null, dotColor: null };
      }
      const questionNumber = Math.min(answered, total);
      const hue = Math.round(120 * (1 - questionNumber / total));
      return { message, isLatest, questionNumber, dotColor: `hsl(${hue} 70% 45%)` };
    });
  });

  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');

  constructor() {
    // Keep the view pinned to the latest content as messages stream in.
    afterRenderEffect(() => {
      // Track the signals that grow the thread.
      this.messages().length;
      this.streamingMessage();
      this.step();
      this.streaming();
      const element = this.scroller()?.nativeElement;
      if (element) {
        element.scrollTop = element.scrollHeight;
      }
    });
  }
}
