import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

const MAX_LENGTH = 2000;

/** The message input: auto-growing textarea, character counter, send / stop control. */
@Component({
  selector: 'app-chat-composer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="border-t border-base-300 bg-base-100/80 px-4 py-4 backdrop-blur-sm sm:px-6">
      <div class="mx-auto w-full max-w-3xl">
        <div
          class="flex items-end gap-2 rounded-box border border-base-300 bg-base-100 p-2 shadow-sm
                 transition-colors focus-within:border-primary/60 focus-within:shadow-md"
        >
          <textarea
            class="dex-scroll max-h-44 min-h-11 w-full resize-none bg-transparent px-3 py-2.5 text-[0.95rem]
                   leading-relaxed text-base-content outline-none placeholder:text-base-content/40
                   [field-sizing:content]"
            rows="1"
            [attr.maxlength]="maxLength"
            [value]="text()"
            [disabled]="streaming()"
            [placeholder]="placeholder()"
            (input)="onInput($event)"
            (keydown)="onKeydown($event)"
            aria-label="Message Dex"
          ></textarea>

          @if (streaming()) {
            <button
              type="button"
              class="btn btn-circle btn-ghost text-error hover:bg-error/10"
              (click)="stop.emit()"
              aria-label="Stop generating"
              title="Stop"
            >
              <span class="inline-block size-3.5 rounded-[3px] bg-current"></span>
            </button>
          } @else {
            <button
              type="button"
              class="btn btn-circle btn-primary shadow-sm disabled:opacity-40"
              [disabled]="!canSend()"
              (click)="emitSend()"
              aria-label="Send message"
              title="Send"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
                <path
                  d="M5 12h13M13 6l6 6-6 6"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
            </button>
          }
        </div>

        <div class="mt-1.5 flex items-center justify-between px-1 text-[0.7rem] text-base-content/45">
          <span>Enter to send · Shift + Enter for a new line</span>
          <span class="font-mono tabular-nums" [class.text-warning]="text().length > maxLength - 100">
            {{ text().length }}/{{ maxLength }}
          </span>
        </div>
      </div>
    </div>
  `,
})
export class ChatComposerComponent {
  readonly streaming = input(false);
  readonly placeholder = input('Ask Dex anything…');

  readonly send = output<string>();
  readonly stop = output<void>();

  protected readonly maxLength = MAX_LENGTH;
  protected readonly text = signal('');
  protected readonly canSend = computed(() => this.text().trim().length > 0 && !this.streaming());

  protected onInput(event: Event): void {
    this.text.set((event.target as HTMLTextAreaElement).value);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.emitSend();
    }
  }

  protected emitSend(): void {
    const value = this.text().trim();
    if (!value || this.streaming()) {
      return;
    }
    this.send.emit(value);
    this.text.set('');
  }
}
