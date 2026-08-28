import { ChangeDetectionStrategy, Component, computed, input, model, output, signal } from '@angular/core';

import { AgentInfo } from '../../core/services/dex-api.service';

const MAX_LENGTH = 2000;

const FALLBACK_AGENT_ICON = 'M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z';

/**
 * 24×24 stroked path data keyed by the semantic icon token discovery advertises
 * (`AgentsConstants.AgentIcons` server-side). The server names what a flow *is*; which glyph that
 * becomes stays a client decision, so new agents reusing a known token need no frontend change and
 * an unrecognised one degrades to the generic glyph instead of rendering blank.
 */
const ICON_PATHS: Record<string, string> = {
  sparkles:
    'M12 3c.5 3.8 2.7 6 6.5 6.5-3.8.5-6 2.7-6.5 6.5-.5-3.8-2.7-6-6.5-6.5C9.3 9 11.5 6.8 12 3z',
  book: 'M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2zM22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z',
  chat: FALLBACK_AGENT_ICON,
};

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
                    [attr.d]="iconFor(activeAgent())"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
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
                    <!-- Two rows per flow: icon + name, then the description beneath it. -->
                    <button
                      type="button"
                      class="items-start gap-2.5 py-2"
                      (click)="pickAgent(agent.name)"
                      [attr.aria-current]="agent.name === activeAgent()?.name ? 'true' : null"
                    >
                      <svg
                        viewBox="0 0 24 24"
                        fill="none"
                        class="mt-0.5 size-4 shrink-0 text-primary"
                        aria-hidden="true"
                      >
                        <path
                          [attr.d]="iconFor(agent)"
                          stroke="currentColor"
                          stroke-width="1.8"
                          stroke-linecap="round"
                          stroke-linejoin="round"
                        />
                      </svg>
                      <span class="flex min-w-0 flex-1 flex-col gap-0.5">
                        <span class="flex w-full items-center gap-1.5">
                          <span class="font-medium capitalize text-base-content">
                            {{ agent.name }}
                          </span>
                          @if (agent.name === activeAgent()?.name) {
                            <svg
                              viewBox="0 0 24 24"
                              fill="none"
                              class="ml-auto size-3.5 shrink-0 text-primary"
                              aria-hidden="true"
                            >
                              <path
                                d="M5 13l4 4L19 7"
                                stroke="currentColor"
                                stroke-width="2"
                                stroke-linecap="round"
                                stroke-linejoin="round"
                              />
                            </svg>
                          }
                        </span>
                        <span
                          class="whitespace-normal text-xs leading-snug text-base-content/55"
                        >
                          {{ agent.description }}
                        </span>
                      </span>
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

  /** Resolve a flow's advertised icon token to a glyph, falling back when it is absent or unknown. */
  protected iconFor(agent: AgentInfo | null | undefined): string {
    const token = agent?.icon;
    return (token ? ICON_PATHS[token.toLowerCase()] : undefined) ?? FALLBACK_AGENT_ICON;
  }

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
