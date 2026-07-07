import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterRenderEffect,
  input,
  output,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MarkdownModule } from 'ngx-markdown';

import { EXAMPLE_PROMPTS, ThreadMessage } from './chat.models';

/** The scrolling conversation: message bubbles, live streaming answer, citations and empty state. */
@Component({
  selector: 'app-chat-thread',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MarkdownModule],
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
            @for (message of messages(); track $index) {
              @if (message.role === 'User') {
                <div class="dex-rise flex justify-end">
                  <div
                    class="max-w-[85%] whitespace-pre-wrap break-words rounded-box rounded-br-sm
                           bg-primary px-4 py-2.5 text-[0.95rem] leading-relaxed text-primary-content
                           shadow-sm"
                  >
                    {{ message.content }}
                  </div>
                </div>
              } @else {
                <div class="dex-rise flex gap-3">
                  <img
                    src="dex-logo.png"
                    alt="Dex"
                    class="mt-0.5 size-8 shrink-0 rounded-full ring-1 ring-base-300"
                  />
                  <div class="min-w-0 flex-1">
                    <div
                      class="markdown whitespace-pre-wrap break-words rounded-box rounded-tl-sm border
                             border-base-300 bg-base-100 px-4 py-3 text-[0.95rem] leading-relaxed
                             text-base-content shadow-sm"
                      markdown
                      [data]="message.content"
                    ></div>
                    @if (message.citations && message.citations.length > 0) {
                      <div class="mt-2 flex flex-wrap gap-1.5">
                        @for (citation of message.citations; track citation.chunkId) {
                          <span
                            class="inline-flex max-w-full items-center gap-1.5 rounded-selector
                                   border border-base-300 bg-base-100 py-1 pl-2 pr-2.5 font-mono
                                   text-[0.7rem] text-base-content/70"
                            [title]="citation.title || citation.headingPath || ''"
                          >
                            <span class="text-accent">#</span>
                            <span class="truncate">
                              {{ citation.headingPath || citation.title || 'Source' }}
                            </span>
                          </span>
                        }
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
                  @if (step() && !streamingText()) {
                    <div
                      class="inline-flex items-center gap-2 rounded-selector bg-base-100 px-3 py-1.5
                             text-sm text-base-content/60 ring-1 ring-base-300"
                    >
                      <span class="loading loading-dots loading-xs text-primary"></span>
                      {{ step() }}
                    </div>
                  } @else {
                    <div
                      class="markdown dex-caret whitespace-pre-wrap break-words rounded-box rounded-tl-sm border
                             border-base-300 bg-base-100 px-4 py-3 text-[0.95rem] leading-relaxed
                             text-base-content shadow-sm"
                      markdown
                      [data]="streamingText()"
                    ></div>
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
  readonly streamingText = input('');
  readonly step = input<string | null>(null);
  readonly error = input<string | null>(null);
  readonly busy = input(false);

  readonly examplePick = output<string>();

  protected readonly examplePrompts = EXAMPLE_PROMPTS;

  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');

  constructor() {
    // Keep the view pinned to the latest content as messages stream in.
    afterRenderEffect(() => {
      // Track the signals that grow the thread.
      this.messages().length;
      this.streamingText();
      this.step();
      this.streaming();
      const element = this.scroller()?.nativeElement;
      if (element) {
        element.scrollTop = element.scrollHeight;
      }
    });
  }
}
