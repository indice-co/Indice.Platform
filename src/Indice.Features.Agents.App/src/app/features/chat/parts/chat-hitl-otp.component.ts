import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { HitlRequest } from './part-contracts';

/** How many digits a one-time code has. */
const OTP_LENGTH = 6;

/**
 * Renders a `application/vnd.indice.hitl-request+json` part that asks for a one-time code: six boxes, one per digit,
 * sent as soon as the last one is filled. The code is emitted as plain text, which the chat page sends as the next
 * user message — the same `pick` path `ChatOptionsComponent` and `ChatConfirmComponent` already use.
 *
 * The boxes are only a picture. A single transparent `<input>` stretched over them does the actual editing, which is
 * what keeps paste, SMS autofill (`autocomplete="one-time-code"`), backspace and screen readers working the way they
 * do in any text field — none of which survive six separate inputs without a lot of key handling.
 *
 * This form is an affordance, not the mechanism: the chat page attaches the structured answer to *whatever* the next
 * user turn is, so answering in the composer instead works identically. That is why nothing here knows about
 * `requestId` or the response media type.
 *
 * Locks itself on the first submit so nothing can fire two turns before the user message lands; `disabled` is the
 * outer rule (only the last message in the thread stays actionable).
 */
@Component({
  selector: 'app-chat-hitl-otp',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (request(); as ask) {
      <div class="flex flex-col items-start gap-2">
        <!-- Usually absent: the server sends the question as prose parts beside this one, so the boxes stand alone. -->
        @if (ask.text) {
          <p class="text-sm text-base-content/70">{{ ask.text }}</p>
        }
        <div class="relative inline-flex items-center gap-1.5 sm:gap-2" [class.opacity-60]="locked()">
          @for (slot of slots(); track $index) {
            <!-- A wider gap splits the code into two groups of three, the way it is usually read out. -->
            <div
              aria-hidden="true"
              class="flex h-12 w-10 items-center justify-center rounded-field border bg-base-100 font-mono text-xl
                     font-semibold tabular-nums text-base-content shadow-sm transition-[border-color,box-shadow]
                     duration-150 sm:w-11"
              [class.ms-2]="$index === groupBreak"
              [class.border-base-300]="!slot.active && !slot.digit"
              [class.border-base-content/40]="!slot.active && slot.digit"
              [class.border-primary]="slot.active"
              [class.ring-2]="slot.active"
              [class.ring-primary/25]="slot.active"
            >
              @if (slot.digit) {
                {{ slot.digit }}
              } @else if (slot.active) {
                <span class="h-5 w-px animate-pulse bg-primary"></span>
              } @else {
                <span class="size-1.5 rounded-full bg-base-content/20"></span>
              }
            </div>
          }
          <input
            #field
            type="text"
            inputmode="numeric"
            autocomplete="one-time-code"
            pattern="[0-9]*"
            class="absolute inset-0 h-full w-full cursor-text opacity-0 disabled:cursor-not-allowed"
            [attr.maxlength]="length"
            [value]="code()"
            [disabled]="locked()"
            [attr.aria-label]="ask.text || 'Enter the ' + length + '-digit code'"
            (input)="onInput($event)"
            (focus)="onFocus()"
            (blur)="focused.set(false)"
            (click)="caretToEnd()"
            (keyup)="caretToEnd()"
            (keydown.enter)="submit()"
          />
        </div>
        @if (sending()) {
          <p class="flex items-center gap-1.5 text-xs text-base-content/60" role="status">
            <span class="loading loading-spinner loading-xs"></span>
            Checking code…
          </p>
        } @else if (!disabled()) {
          <p class="text-xs text-base-content/60">Enter the {{ length }}-digit code — it is sent automatically.</p>
        }
      </div>
    }
  `,
})
export class ChatHitlOtpComponent {
  /** The request to render, or null when the part carried nothing parseable. */
  readonly request = input<HitlRequest | null>(null);
  /** Whether the request is spent — set by the thread for anything that is no longer the latest message. */
  readonly disabled = input(false);

  /** Emits the typed code, to be sent as the next user message. */
  readonly pick = output<string>();

  protected readonly length = OTP_LENGTH;
  protected readonly groupBreak = OTP_LENGTH / 2;

  protected readonly code = signal('');
  protected readonly focused = signal(false);

  /** Guards against a second submit landing before the sent message pushes this form out of last position. */
  private readonly picked = signal(false);

  private readonly field = viewChild<ElementRef<HTMLInputElement>>('field');

  protected readonly locked = computed(() => this.disabled() || this.picked());
  /** Sent, and still the latest message: the answer is on its way but nothing has come back yet. */
  protected readonly sending = computed(() => this.picked() && !this.disabled());

  /** One entry per box: its digit, if typed, and whether it is where the next digit will land. */
  protected readonly slots = computed(() => {
    const code = this.code();
    const activeIndex = this.focused() && !this.locked() ? Math.min(code.length, OTP_LENGTH - 1) : -1;
    return Array.from({ length: OTP_LENGTH }, (_, index) => ({ digit: code[index] ?? '', active: index === activeIndex }));
  });

  constructor() {
    // The code is the only thing the user can do next, so take the keyboard — but never for a spent request.
    afterNextRender(() => {
      if (!this.locked()) {
        this.field()?.nativeElement.focus({ preventScroll: true });
      }
    });
  }

  protected onInput(event: Event): void {
    const field = event.target as HTMLInputElement;
    const code = field.value.replace(/\D/gu, '').slice(0, OTP_LENGTH);
    // Written back by hand: when a rejected keystroke leaves the signal unchanged, the `[value]` binding has nothing
    // to update and the stray character would stay in the (invisible) field.
    field.value = code;
    this.code.set(code);
    if (code.length === OTP_LENGTH) {
      this.submit();
    }
  }

  protected onFocus(): void {
    this.focused.set(true);
    this.caretToEnd();
  }

  /** Editing only ever happens at the end — the boxes have no way to show a caret in the middle. */
  protected caretToEnd(): void {
    const field = this.field()?.nativeElement;
    field?.setSelectionRange(field.value.length, field.value.length);
  }

  protected submit(): void {
    const code = this.code();
    if (this.locked() || code.length !== OTP_LENGTH) {
      return;
    }
    this.picked.set(true);
    this.pick.emit(code);
  }
}
