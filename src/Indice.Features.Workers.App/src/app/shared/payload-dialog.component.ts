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
          <label class="flex flex-col gap-1">
            <span class="text-sm font-medium">Queue</span>
            <input
              class="input input-sm w-full font-mono"
              list="payload-dialog-queues"
              required
              maxlength="400"
              [value]="queueName()"
              (input)="queueName.set($any($event.target).value)"
            />
            <datalist id="payload-dialog-queues">
              @for (queue of queues(); track queue) {
                <option [value]="queue"></option>
              }
            </datalist>
          </label>
        }

        <label class="flex flex-col gap-1">
          <span class="text-sm font-medium">Payload (JSON)</span>
          <textarea
            class="textarea textarea-sm h-72 w-full font-mono text-xs"
            spellcheck="false"
            [class.textarea-error]="!!parseError()"
            [value]="text()"
            (input)="text.set($any($event.target).value)"
          ></textarea>
          @if (parseError(); as message) {
            <span class="text-xs text-error">{{ message }}</span>
          } @else if (hint()) {
            <span class="text-xs text-base-content/60">{{ hint() }}</span>
          }
        </label>

        @if (askVisibleAt()) {
          <label class="flex flex-col gap-1">
            <span class="text-sm font-medium">Visible at <span class="font-normal text-base-content/60">(optional, your local time)</span></span>
            <input
              type="datetime-local"
              class="input input-sm w-full sm:w-64"
              [value]="visibleAt()"
              (input)="visibleAt.set($any($event.target).value)"
            />
          </label>
        }

        <div class="modal-action mt-0">
          <button type="button" class="btn btn-ghost" (click)="close()">Cancel</button>
          <button type="submit" class="btn btn-primary" [disabled]="!canSubmit()">
            @if (busy()) {
              <span class="loading loading-spinner loading-xs"></span>
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

  /** Opens with the given payload, pretty-printed when it parses. */
  open(payload?: string | null, queueName = ''): void {
    this.text.set(prettyPrint(payload));
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

function prettyPrint(value: string | null | undefined): string {
  if (!value) {
    return '{\n  \n}';
  }
  try {
    let lossy = false;
    const parsed: unknown = JSON.parse(value, (_key, item: unknown) => {
      if (typeof item === 'number' && Number.isInteger(item) && !Number.isSafeInteger(item)) {
        lossy = true;
      }
      return item;
    });
    // Re-formatting would round a 64-bit id, so such a payload is edited as stored.
    return lossy ? value : JSON.stringify(parsed, null, 2);
  } catch {
    return value;
  }
}
