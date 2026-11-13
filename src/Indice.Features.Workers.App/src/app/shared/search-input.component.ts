import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LucideSearch, LucideX } from '@lucide/angular';
import { Subject, debounce, filter, map, timer } from 'rxjs';

/**
 * Debounced free-text search box, the same control as the Cases, Messaging and Risk UIs. Emits once the text is empty or
 * at least `minLength` characters long; Enter, Escape and the clear button search right away (Enter regardless of
 * `minLength`).
 */
@Component({
  selector: 'app-search-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideSearch, LucideX],
  host: { class: 'block' },
  template: `
    <label class="input input-sm w-full">
      <svg lucideSearch class="size-4 opacity-60"></svg>
      <!-- not type="search": its native clear button duplicates ours and its native, bubbling "search" event (Enter)
           would reach the host's (search) listener as an Event instead of our string output -->
      <input
        type="text"
        role="searchbox"
        enterkeyhint="search"
        autocomplete="off"
        class="grow"
        [placeholder]="placeholder()"
        [attr.aria-label]="placeholder() || null"
        [value]="text()"
        (input)="onInput($any($event.target).value)"
        (keydown.enter)="onInput(text(), true)"
        (keydown.escape)="onInput('', true)"
      />
      @if (text()) {
        <button type="button" class="btn btn-ghost btn-xs btn-circle" (click)="onInput('', true)" aria-label="Clear">
          <svg lucideX class="size-3.5"></svg>
        </button>
      }
    </label>
  `,
})
export class SearchInputComponent {
  readonly value = input<string>('');
  readonly placeholder = input<string>('');
  readonly minLength = input<number>(1);
  readonly debounce = input<number>(300);
  readonly search = output<string>();

  protected readonly text = signal('');
  private readonly input$ = new Subject<{ value: string; immediate: boolean }>();
  /** The last emitted term, until `value` reflects it. */
  private emitted?: string;

  constructor() {
    effect(() => {
      const value = this.value() ?? '';
      // The echo of our own search must not overwrite what was typed meanwhile (more characters, a trailing space);
      // any other change (cleared filters, a new page) does.
      const echo = value === this.emitted;
      this.emitted = undefined;
      if (!echo) {
        this.text.set(value);
      }
    });
    this.input$
      .pipe(
        // Debounce first so that every keystroke, including too-short ones, cancels a pending obsolete search.
        debounce(({ immediate }) => timer(immediate ? 0 : this.debounce())),
        filter(({ value, immediate }) => immediate || value.length === 0 || value.length >= this.minLength()),
        map(({ value }) => value),
        // Compare with the applied term rather than the last emitted one, which may since have changed from outside.
        filter((value) => value !== (untracked(this.value) ?? '')),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((value) => {
        this.emitted = value;
        this.search.emit(value);
      });
  }

  protected onInput(value: string, immediate = false): void {
    this.text.set(value);
    this.input$.next({ value: value.trim(), immediate });
  }
}
