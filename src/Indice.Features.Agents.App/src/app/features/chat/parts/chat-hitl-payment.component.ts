import { ChangeDetectionStrategy, Component, computed, ElementRef, input, output, signal, viewChild } from '@angular/core';

import { HitlRequest } from './part-contracts';

/** Distinguishes the radio groups of two payment forms in the same thread. */
let nextGroupId = 0;

/**
 * Renders a `application/vnd.indice.hitl-request+json` part that asks for a payment method: the methods as a radio
 * list and a button that opens the payment sheet. The sheet is a picture — nothing is charged — and dismissing it, by
 * clicking outside it or pressing Escape, emits the chosen method as plain text, which the chat page sends as the next
 * user message — the same `pick` path `ChatOptionsComponent` and `ChatConfirmComponent` already use.
 *
 * This form is an affordance, not the mechanism: the chat page attaches the structured answer to *whatever* the next
 * user turn is, so answering in the composer instead works identically. That is why nothing here knows about
 * `requestId` or the response media type.
 *
 * Locks itself once the sheet is dismissed so nothing can fire two turns before the user message lands; `disabled` is
 * the outer rule (only the last message in the thread stays actionable).
 */
@Component({
  selector: 'app-chat-hitl-payment',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (methods().length > 0) {
      <div class="flex flex-col items-start gap-3">
        <fieldset class="flex w-full max-w-xs flex-col gap-2" [disabled]="locked()" [class.opacity-60]="locked()">
          <legend class="mb-2 text-sm text-base-content/70">{{ request()?.text || 'Επιλέξτε τρόπο πληρωμής' }}</legend>
          @for (method of methods(); track method) {
            <label
              class="flex cursor-pointer items-center gap-3 rounded-field border bg-base-100 px-4 py-2.5 text-sm
                     text-base-content shadow-sm transition-[border-color] duration-150"
              [class.border-base-300]="selected() !== method"
              [class.border-primary]="selected() === method"
              [class.cursor-not-allowed]="locked()"
            >
              <input
                type="radio"
                class="radio radio-primary radio-sm"
                [name]="groupName"
                [value]="method"
                [checked]="selected() === method"
                (change)="selected.set(method)"
              />
              {{ method }}
              <span class="ms-auto ps-4" [attr.data-icon]="iconFor(method)">
                @switch (iconFor(method)) {
                  @case ('apple') {
                    <img src="apple-pay-mark.svg" alt="" class="h-7 w-auto" />
                  }
                  @case ('google') {
                    <img src="google-pay-mark.svg" alt="" class="h-7 w-auto" />
                  }
                  @default {
                    <svg viewBox="0 0 24 24" fill="none" class="h-7 w-9 text-base-content/70" aria-hidden="true">
                      <path
                        d="M3 7.5A1.5 1.5 0 0 1 4.5 6h15A1.5 1.5 0 0 1 21 7.5v9a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 16.5v-9zM3 10h18M7 14.5h3"
                        stroke="currentColor"
                        stroke-width="1.8"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      />
                    </svg>
                  }
                }
              </span>
            </label>
          }
        </fieldset>
        <button type="button" class="btn btn-primary btn-sm" [disabled]="locked() || !selected()" (click)="open()">
          Πληρωμή
        </button>
      </div>
      <dialog #sheet class="modal" (close)="onClosed()">
        <div class="modal-box w-auto max-w-sm bg-transparent p-0 shadow-none">
          <!-- The radius matches the popup's own corners in the picture, so the page behind it is clipped away. -->
          <img
            src="apple-pay-scan.png"
            alt="Scan the code with your iPhone to pay with {{ selected() }}"
            class="block w-full rounded-[7%]"
          />
        </div>
        <form method="dialog" class="modal-backdrop">
          <button type="submit" aria-label="Close">close</button>
        </form>
      </dialog>
    }
  `,
})
export class ChatHitlPaymentComponent {
  /** The request to render, or null when the part carried nothing parseable. */
  readonly request = input<HitlRequest | null>(null);
  /** Whether the request is spent — set by the thread for anything that is no longer the latest message. */
  readonly disabled = input(false);

  /** Emits the chosen payment method, to be sent as the next user message. */
  readonly pick = output<string>();

  protected readonly groupName = `payment-method-${nextGroupId++}`;

  protected readonly methods = computed(() => this.request()?.payment?.methods ?? []);
  protected readonly selected = signal<string | null>(null);

  /** Guards against a second submit landing before the sent message pushes this form out of last position. */
  private readonly picked = signal(false);

  private readonly sheet = viewChild<ElementRef<HTMLDialogElement>>('sheet');

  protected readonly locked = computed(() => this.disabled() || this.picked());

  /**
   * Which glyph a method gets. The methods arrive as display names, so the brand is read off the name; anything
   * unrecognised is drawn as a card.
   */
  protected iconFor(method: string): 'apple' | 'google' | 'card' {
    const name = method.toLowerCase();
    if (name.includes('apple')) {
      return 'apple';
    }
    return name.includes('google') ? 'google' : 'card';
  }

  protected open(): void {
    if (this.locked() || !this.selected()) {
      return;
    }
    this.sheet()?.nativeElement.showModal();
  }

  /** The sheet was dismissed, which is what stands in for a completed payment. */
  protected onClosed(): void {
    const method = this.selected();
    if (this.locked() || !method) {
      return;
    }
    this.picked.set(true);
    this.pick.emit(method);
  }
}
