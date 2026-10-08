import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

/**
 * Renders a `application/vnd.indice.multiple-choice+json` part: the options as buttons. Picking one emits its text,
 * which the chat page sends as a normal user message — the option string is the message, verbatim.
 *
 * The list locks itself on the first pick so a double-click cannot fire two turns in the window before the user
 * message lands; `disabled` is the outer rule (only the last message in the thread stays actionable).
 */
@Component({
  selector: 'app-chat-options',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (options().length > 0) {
      <div class="mt-2 flex flex-wrap gap-2" role="group" aria-label="Suggested questions">
        @for (option of options(); track option) {
          <button
            type="button"
            class="rounded-selector border border-base-300 bg-base-100 px-4 py-2 text-left text-sm
                   text-base-content/75 shadow-sm transition enabled:hover:border-primary/40
                   enabled:hover:text-base-content disabled:cursor-not-allowed disabled:opacity-50"
            [disabled]="locked()"
            (click)="pickOption(option)"
          >
            {{ option }}
          </button>
        }
      </div>
    }
  `,
})
export class ChatOptionsComponent {
  /** The options to offer, in display order. */
  readonly options = input<string[]>([]);
  /** Whether the list is spent — set by the thread for anything that is no longer the latest message. */
  readonly disabled = input(false);

  /** Emits the picked option's text, to be sent as the next user message. */
  readonly pick = output<string>();

  /** Guards against a second click landing before the sent message pushes this list out of last position. */
  private readonly picked = signal(false);

  protected readonly locked = computed(() => this.disabled() || this.picked());

  protected pickOption(option: string): void {
    if (this.locked()) {
      return;
    }
    this.picked.set(true);
    this.pick.emit(option);
  }
}
