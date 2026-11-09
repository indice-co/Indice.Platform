import { Component, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounce, filter, map, timer } from 'rxjs';
import { LucideSearch, LucideX } from '@lucide/angular';

/**
 * Debounced free-text search box. Emits once the text is empty or at least `minLength` characters long;
 * Enter, Escape and the clear button search right away (Enter regardless of `minLength`).
 */
@Component({
  selector: 'app-search-input',
  imports: [LucideSearch, LucideX],
  template: `
    <label class="input input-sm w-full">
      <svg lucideSearch class="size-4 opacity-60"></svg>
      <!-- not type="search": its native clear button duplicates ours and its native, bubbling "search" event (Enter)
           would reach the host's (search) listener as an Event instead of our string output -->
      <input type="text" role="searchbox" enterkeyhint="search" autocomplete="off" class="grow" [placeholder]="placeholder()"
        [value]="text()" (input)="onInput($any($event.target).value)" (keydown.enter)="onInput(text(), true)"
        (keydown.escape)="onInput('', true)" />
      @if (text()) {
        <button type="button" class="btn btn-ghost btn-xs btn-circle" (click)="onInput('', true)" aria-label="Clear">
          <svg lucideX class="size-3.5"></svg>
        </button>
      }
    </label>
  `
})
export class SearchInputComponent {
  public readonly value = input<string>('');
  public readonly placeholder = input<string>('');
  public readonly minLength = input<number>(3);
  public readonly debounce = input<number>(300);
  public readonly search = output<string>();

  protected readonly text = signal('');
  private readonly _input$ = new Subject<{ value: string; immediate: boolean }>();
  /** The last emitted term, until `value` reflects it. */
  private _emitted?: string;

  constructor() {
    effect(() => {
      const value = this.value() ?? '';
      // the echo of our own search must not overwrite what was typed meanwhile (more characters, a trailing space);
      // any other change (back / forward, cleared filters) does
      const echo = value === this._emitted;
      this._emitted = undefined;
      if (!echo) {
        this.text.set(value);
      }
    });
    this._input$
      .pipe(
        // Debounce first so that every keystroke, including too-short ones, cancels a pending obsolete search
        debounce(({ immediate }) => timer(immediate ? 0 : this.debounce())),
        filter(({ value, immediate }) => immediate || value.length === 0 || value.length >= this.minLength()),
        map(({ value }) => value),
        // compare with the applied term rather than the last emitted one, which may since have changed from outside
        filter(value => value !== (untracked(this.value) ?? '')),
        takeUntilDestroyed(inject(DestroyRef))
      )
      .subscribe(value => {
        this._emitted = value;
        this.search.emit(value);
      });
  }

  protected onInput(value: string, immediate = false): void {
    this.text.set(value);
    this._input$.next({ value: value.trim(), immediate });
  }
}
