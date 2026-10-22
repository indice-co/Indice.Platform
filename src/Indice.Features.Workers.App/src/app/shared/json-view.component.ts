import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';

/** How long the "Copied" confirmation stays on the button. */
const COPIED_MS = 1500;

/** Read-only JSON block for payloads and task state: pretty-printed when it parses, raw otherwise. */
@Component({
  selector: 'app-json-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative">
      @if (text()) {
        <button
          type="button"
          class="btn btn-ghost btn-xs absolute right-2 top-2"
          (click)="copy()"
          [attr.aria-label]="copied() ? 'Copied' : 'Copy JSON'"
        >
          {{ copied() ? 'Copied' : 'Copy' }}
        </button>
      }
      <pre
        class="app-scroll max-h-[28rem] overflow-auto rounded-box bg-base-200 p-4 font-mono text-xs"
      >{{ text() || '—' }}</pre>
    </div>
  `,
})
export class JsonViewComponent {
  readonly json = input<string | null | undefined>();

  protected readonly copied = signal(false);
  protected readonly text = computed(() => prettyPrint(this.json()));

  private timer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.timer));
  }

  protected copy(): void {
    navigator.clipboard?.writeText(this.text()).then(() => {
      this.copied.set(true);
      clearTimeout(this.timer);
      this.timer = setTimeout(() => this.copied.set(false), COPIED_MS);
    });
  }
}

function prettyPrint(value: string | null | undefined): string {
  if (!value) {
    return '';
  }
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}
