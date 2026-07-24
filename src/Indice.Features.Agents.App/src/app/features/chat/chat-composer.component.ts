import { ChangeDetectionStrategy, Component, computed, input, model, output, signal } from '@angular/core';

import { AgentInfo } from '../../core/services/dex-api.service';

const MAX_LENGTH = 2000;

/** The message input: mode picker, auto-growing textarea, character counter, send / stop control. */
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
          @if (agents().length > 0) {
            <!-- Mode picker: which agent answers. Opens upward — the composer hugs the viewport bottom. -->
            <div class="dropdown dropdown-top self-end">
              <button
                type="button"
                class="btn btn-ghost btn-sm mb-1 gap-1 px-2 font-normal text-base-content/60
                       hover:text-base-content"
                [disabled]="streaming()"
                aria-label="Select mode"
                title="Mode"
              >
                <svg viewBox="0 0 24 24" fill="none" class="size-3.5" aria-hidden="true">
                  <path
                    d="M12 3c.5 3.8 2.7 6 6.5 6.5-3.8.5-6 2.7-6.5 6.5-.5-3.8-2.7-6-6.5-6.5C9.3 9 11.5 6.8 12 3z"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linejoin="round"
                  />
                </svg>
                <span class="capitalize">{{ activeAgent()?.name }}</span>
                <svg viewBox="0 0 24 24" fill="none" class="size-3" aria-hidden="true">
                  <path d="M6 15l6-6 6 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </button>
              <ul
                tabindex="0"
                class="dropdown-content menu z-10 mb-2 w-64 rounded-box border border-base-300
                       bg-base-100 p-2 shadow-lg"
              >
                @for (agent of agents(); track agent.name) {
                  <li>
                    <button type="button" class="flex-col items-start gap-0.5" (click)="pickAgent(agent.name)">
                      <span class="flex w-full items-center gap-1.5">
                        <span class="capitalize font-medium">{{ agent.name }}</span>
                        @if (agent.name === activeAgent()?.name) {
                          <svg viewBox="0 0 24 24" fill="none" class="ml-auto size-3.5 text-primary" aria-hidden="true">
                            <path d="M5 13l4 4L19 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
                          </svg>
                        }
                      </span>
                      <span class="text-xs text-base-content/55">{{ agent.description }}</span>
                    </button>
                  </li>
                }
              </ul>
            </div>
          }

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

          <span
            class="self-end pb-3 font-mono text-[0.7rem] tabular-nums text-base-content/40"
            [class.text-warning]="text().length > maxLength - 100"
          >
            {{ text().length }}/{{ maxLength }}
          </span>

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

        <div class="mt-1.5 px-1 text-[0.7rem] text-base-content/45">
          <span>Enter to send · Shift + Enter for a new line</span>
        </div>
      </div>
    </div>
  `,
})
export class ChatComposerComponent {
  readonly streaming = input(false);
  readonly placeholder = input('Ask Dex anything…');
  /** The modes (agents) the user can pick from; the picker hides when empty. */
  readonly agents = input<AgentInfo[]>([]);
  /** Two-way: the picked agent name — `null` until the user picks, meaning the first discovered agent. */
  readonly selectedAgent = model<string | null>(null);

  readonly send = output<string>();
  readonly stop = output<void>();

  protected readonly maxLength = MAX_LENGTH;
  protected readonly text = signal('');
  protected readonly canSend = computed(() => this.text().trim().length > 0 && !this.streaming());

  /** The agent shown on the picker trigger — the explicit pick, else the first discovered one. */
  protected readonly activeAgent = computed<AgentInfo | null>(() => {
    const agents = this.agents();
    return agents.find((agent) => agent.name === this.selectedAgent()) ?? agents[0] ?? null;
  });

  protected pickAgent(name: string): void {
    this.selectedAgent.set(name);
    // The daisyUI dropdown is focus-driven — blur closes it after the pick.
    (document.activeElement as HTMLElement | null)?.blur();
  }

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
