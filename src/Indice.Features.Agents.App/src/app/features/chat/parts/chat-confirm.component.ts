import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import { Confirmation } from './part-contracts';

/**
 * Renders a confirmation part: an optional question and two buttons. Picking one emits its label, which the chat page
 * sends as a normal user message — the label is the message, verbatim, exactly as in `ChatOptionsComponent`.
 *
 * Locks itself on the first pick so a double-click cannot fire two turns before the user message lands; `disabled` is
 * the outer rule (only the last message in the thread stays actionable).
 */
@Component({
  selector: 'app-chat-confirm',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (confirmation(); as prompt) {
      <div class="flex flex-col gap-2">
        @if (prompt.prompt) {
          <p class="text-sm text-base-content/70">{{ prompt.prompt }}</p>
        }
        <div class="flex flex-wrap gap-2">
          <button type="button" class="btn btn-primary btn-sm" [disabled]="locked()" (click)="choose(prompt.confirmText)">
            {{ prompt.confirmText }}
          </button>
          <button
            type="button"
            class="btn btn-ghost btn-sm border border-base-300 bg-base-100"
            [disabled]="locked()"
            (click)="choose(prompt.cancelText)"
          >
            {{ prompt.cancelText }}
          </button>
        </div>
      </div>
    }
  `,
})
export class ChatConfirmComponent {
  /** The confirmation to render, or null when the part carried nothing renderable. */
  readonly confirmation = input<Confirmation | null>(null);
  /** Whether the choice is spent — set by the thread for anything that is no longer the latest message. */
  readonly disabled = input(false);

  /** Emits the picked button's label, to be sent as the next user message. */
  readonly pick = output<string>();

  /** Guards against a second click landing before the sent message pushes this choice out of last position. */
  private readonly picked = signal(false);

  protected readonly locked = computed(() => this.disabled() || this.picked());

  protected choose(label: string): void {
    if (this.locked()) {
      return;
    }
    this.picked.set(true);
    this.pick.emit(label);
  }
}
