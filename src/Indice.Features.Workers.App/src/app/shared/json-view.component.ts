import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { LucideCheck, LucideCopy } from '@lucide/angular';

/** How long the "Copied" confirmation stays on the button. */
const COPIED_MS = 1500;

/** Read-only JSON block for payloads and task state: pretty-printed when it parses, raw otherwise. */
@Component({
  selector: 'app-json-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCheck, LucideCopy],
  template: `
    <div class="relative">
      @if (text()) {
        <button
          type="button"
          class="btn btn-xs absolute right-2 top-2 gap-1 border-base-300 bg-base-100 shadow-sm"
          (click)="copy()"
          [attr.aria-label]="copied() ? 'Copied' : 'Copy JSON'"
        >
          @if (copied()) {
            <svg lucideCheck class="size-3.5 text-success"></svg>Copied
          } @else {
            <svg lucideCopy class="size-3.5"></svg>Copy
          }
        </button>
      }
      <pre
        class="app-scroll max-h-[28rem] overflow-auto rounded-box border border-base-300 bg-base-200/70 p-4 font-mono
               text-xs leading-relaxed"
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
