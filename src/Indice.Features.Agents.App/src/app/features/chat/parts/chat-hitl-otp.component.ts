import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import { HitlRequest } from './part-contracts';

/**
 * Renders a `application/vnd.indice.hitl-request+json` part: the question the workflow is blocked on, and a field to
 * answer it in. Submitting emits the typed text, which the chat page sends as the next user message — the same
 * `pick` path `ChatOptionsComponent` and `ChatConfirmComponent` already use.
 *
 * This form is an affordance, not the mechanism: the chat page attaches the structured answer to *whatever* the next
 * user turn is, so answering in the composer instead works identically. That is why nothing here knows about
 * `requestId` or the response media type.
 *
 * Locks itself on the first submit so a double-click cannot fire two turns before the user message lands;
 * `disabled` is the outer rule (only the last message in the thread stays actionable).
 */
@Component({
  selector: 'app-chat-hitl-otp',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (request(); as ask) {
      <div class="flex flex-col gap-2">
        <!-- Usually absent: the server sends the question as prose parts beside this one, so the field stands alone. -->
        @if (ask.text) {
          <p class="text-sm text-base-content/70">{{ ask.text }}</p>
        }
        <div class="flex flex-wrap items-center gap-2">
          <input
            type="text"
            class="input input-sm w-full max-w-xs border-base-300 bg-base-100 text-[0.95rem]"
            [value]="answer()"
            [disabled]="locked()"
            [attr.aria-label]="ask.text || 'Your answer'"
            (input)="onInput($event)"
            (keydown.enter)="submit()"
          />
          <button
            type="button"
            class="btn btn-primary btn-sm"
            [disabled]="locked() || !canSubmit()"
            (click)="submit()"
          >
            Send
          </button>
        </div>
      </div>
    }
  `,
})
export class ChatHitlOtpComponent {
  /** The request to render, or null when the part carried nothing parseable. */
  readonly request = input<HitlRequest | null>(null);
  /** Whether the request is spent — set by the thread for anything that is no longer the latest message. */
  readonly disabled = input(false);

  /** Emits the typed answer, to be sent as the next user message. */
  readonly pick = output<string>();

  protected readonly answer = signal('');

  /** Guards against a second submit landing before the sent message pushes this form out of last position. */
  private readonly picked = signal(false);

  protected readonly locked = computed(() => this.disabled() || this.picked());
  protected readonly canSubmit = computed(() => this.answer().trim().length > 0);

  protected onInput(event: Event): void {
    this.answer.set((event.target as HTMLInputElement).value);
  }

  protected submit(): void {
    const value = this.answer().trim();
    if (this.locked() || !value) {
      return;
    }
    this.picked.set(true);
    this.pick.emit(value);
  }
}
