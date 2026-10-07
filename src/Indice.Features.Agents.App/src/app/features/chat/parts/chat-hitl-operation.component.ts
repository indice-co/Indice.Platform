import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import { HitlRequest } from './part-contracts';

/**
 * Renders an `OperationRequest` HITL part as a list of checkboxes of which only one may be ticked. The chosen operation
 * is emitted as plain text, which the chat page sends as the next user message; `hitlResponseParts` attaches the
 * structured `{ selectedOperation }` answer.
 *
 * Locks itself on submit so nothing can fire two turns before the user message lands; `disabled` is the outer rule
 * (only the last message in the thread stays actionable).
 */
@Component({
  selector: 'app-chat-hitl-operation',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (request(); as ask) {
      <div class="flex flex-col items-start gap-2" [class.opacity-60]="locked()">
        @if (ask.text) {
          <p class="text-sm text-base-content/70">{{ ask.text }}</p>
        }
        <div class="flex flex-wrap gap-2" role="group" [attr.aria-label]="ask.text || 'Choose an operation'">
          @for (operation of operations(); track operation) {
            <button
              type="button"
              class="btn btn-sm"
              [class.btn-primary]="selected() === operation"
              [class.btn-outline]="selected() !== operation"
              [disabled]="locked()"
              (click)="submit(operation)"
            >
              {{ operation }}
            </button>
          }
        </div>
        @if (sending()) {
          <p class="flex items-center gap-1.5 text-xs text-base-content/60" role="status">
            <span class="loading loading-spinner loading-xs"></span>
            Sending…
          </p>
        }
      </div>
    }
  `,
})
export class ChatHitlOperationComponent {
  /** The request to render, or null when the part carried nothing parseable. */
  readonly request = input<HitlRequest | null>(null);
  /** Whether the request is spent — set by the thread for anything that is no longer the latest message. */
  readonly disabled = input(false);

  /** Emits the selected operation, to be sent as the next user message. */
  readonly pick = output<string>();

  protected readonly selected = signal<string | null>(null);

  /** Guards against a second submit landing before the sent message pushes this form out of last position. */
  private readonly picked = signal(false);

  protected readonly operations = computed(() => this.request()?.operation?.supportedOperations ?? []);
  protected readonly locked = computed(() => this.disabled() || this.picked());
  /** Sent, and still the latest message: the answer is on its way but nothing has come back yet. */
  protected readonly sending = computed(() => this.picked() && !this.disabled());

  protected submit(operation: string): void {
    if (this.locked() || !operation) {
      return;
    }
    this.selected.set(operation);
    this.picked.set(true);
    this.pick.emit(operation);
  }
}
