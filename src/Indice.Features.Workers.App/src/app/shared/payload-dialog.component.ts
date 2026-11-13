import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { LucideCalendarClock, LucideInbox, LucidePlus, LucideRotateCcw } from '@lucide/angular';

export interface PayloadDialogResult {
  /** Only when `askQueue` is set. */
  queueName: string | null;
  /** The JSON as typed. */
  payload: string;
  /** UTC ISO date; `null` for "now". Only when `askVisibleAt` is set. */
  visibleAt: string | null;
}

/**
 * JSON editor in a native `<dialog>`, for adding a message and for editing a payload before requeueing.
 * The page calls `open()`, makes the request on `submitted`, and calls `close()` when it succeeds, so a
 * server error keeps the text for another try.
 */
@Component({
  selector: 'app-payload-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCalendarClock, LucideInbox, LucidePlus, LucideRotateCcw],
  template: `
    <dialog #dialog class="modal">
      <form class="modal-box flex max-w-2xl flex-col gap-4" (submit)="submit($event)">
        <div>
          <h3 class="text-lg font-semibold text-base-content">{{ title() }}</h3>
          @if (description()) {
            <p class="mt-1 text-sm text-base-content/70">{{ description() }}</p>
          }
        </div>

        @if (askQueue()) {
          <fieldset class="fieldset p-0">
            <legend class="fieldset-legend pt-0 text-sm font-medium">Queue <span class="text-error">*</span></legend>
            <label class="input w-full">
              <svg lucideInbox class="size-4 opacity-60"></svg>
              <input
                class="grow font-mono"
                list="payload-dialog-queues"
                required
                maxlength="400"
                autocomplete="off"
                placeholder="e.g. send-email"
                [value]="queueName()"
                (input)="queueName.set($any($event.target).value)"
              />
            </label>
            <datalist id="payload-dialog-queues">
              @for (queue of queues(); track queue) {
                <option [value]="queue"></option>
              }
            </datalist>
          </fieldset>
        }

        <fieldset class="fieldset p-0">
          <legend class="fieldset-legend pt-0 text-sm font-medium">Payload <span class="text-error">*</span></legend>
          <textarea
            class="textarea h-72 w-full font-mono text-xs leading-relaxed"
            spellcheck="false"
            aria-label="Payload (JSON)"
            [class.textarea-error]="!!parseError()"
            [attr.aria-invalid]="!!parseError()"
            [value]="text()"
            (input)="text.set($any($event.target).value)"
          ></textarea>
          @if (parseError(); as message) {
            <p class="label text-xs text-error">{{ message }}</p>
          } @else {
            <p class="label text-xs">{{ hint() || 'JSON, sent to the queue as typed.' }}</p>
          }
        </fieldset>

        @if (askVisibleAt()) {
          <fieldset class="fieldset p-0">
            <legend class="fieldset-legend pt-0 text-sm font-medium">Visible at</legend>
            <label class="input w-full sm:w-72">
              <svg lucideCalendarClock class="size-4 opacity-60"></svg>
              <input
                type="datetime-local"
                class="grow tabular-nums"
                [value]="visibleAt()"
                (input)="visibleAt.set($any($event.target).value)"
              />
            </label>
            <p class="label text-xs">Optional, in your local time. Leave empty to make it visible now.</p>
          </fieldset>
        }

        <div class="modal-action mt-0">
          <button type="button" class="btn btn-ghost" (click)="close()">Cancel</button>
          <button type="submit" class="btn btn-primary gap-1.5" [disabled]="!canSubmit()">
            @if (busy()) {
              <span class="loading loading-spinner loading-xs"></span>
            } @else if (confirmIcon() === 'add') {
              <svg lucidePlus class="size-4"></svg>
            } @else {
              <svg lucideRotateCcw class="size-4"></svg>
            }
            {{ confirmLabel() }}
          </button>
        </div>
      </form>
      <form method="dialog" class="modal-backdrop">
        <button>close</button>
      </form>
    </dialog>
  `,
})
export class PayloadDialogComponent {
  readonly title = input('Edit payload');
  readonly description = input('');
  readonly hint = input('');
  readonly confirmLabel = input('Save');
  /** The submit button's icon: a plus for a new message, requeue for an edited one. */
  readonly confirmIcon = input<'add' | 'requeue'>('requeue');
  /** Shows a queue name field, with `queues` as suggestions. */
  readonly askQueue = input(false);
  readonly queues = input<readonly string[]>([]);
  /** Shows the optional "visible at" field. */
  readonly askVisibleAt = input(false);
  /** The page's request is in flight. */
  readonly busy = input(false);

  readonly submitted = output<PayloadDialogResult>();

  protected readonly text = signal('');
  protected readonly queueName = signal('');
  protected readonly visibleAt = signal('');

  protected readonly parseError = computed(() => {
    const text = this.text().trim();
    if (!text) {
      return 'The payload is required.';
    }
    try {
      JSON.parse(text);
      return null;
    } catch (e) {
      return e instanceof Error ? `Not valid JSON: ${e.message}` : 'Not valid JSON.';
    }
  });
  protected readonly canSubmit = computed(
    () => !this.busy() && !this.parseError() && (!this.askQueue() || !!this.queueName().trim()),
  );

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  /** Opens with the given payload as stored. */
  open(payload?: string | null, queueName = ''): void {
    this.text.set(initialPayload(payload));
    this.queueName.set(queueName);
    this.visibleAt.set('');
    this.dialog().nativeElement.showModal();
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  protected submit(event: Event): void {
    event.preventDefault();
    if (!this.canSubmit()) {
      return;
    }
    const visibleAt = this.askVisibleAt() && this.visibleAt() ? new Date(this.visibleAt()) : null;
    this.submitted.emit({
      queueName: this.askQueue() ? this.queueName().trim() : null,
      // Sent as typed: a round trip through JSON.parse would round integers above 2^53.
      payload: this.text().trim(),
      visibleAt: visibleAt && !Number.isNaN(visibleAt.getTime()) ? visibleAt.toISOString() : null,
    });
  }
}

function initialPayload(value: string | null | undefined): string {
  if (!value) {
    return '{\n  \n}';
  }
  return value;
}
