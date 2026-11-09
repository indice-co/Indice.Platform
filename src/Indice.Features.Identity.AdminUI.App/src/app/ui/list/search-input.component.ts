import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounce, distinctUntilChanged, filter, map, timer } from 'rxjs';
import { LucideSearch, LucideX } from '@lucide/angular';
import { TranslatePipe } from '@ngx-translate/core';

/** Debounced free-text search box. Emits once the text is empty or at least `minLength` characters long. */
@Component({
  selector: 'app-search-input',
  imports: [LucideSearch, LucideX, TranslatePipe],
  template: `
    <label class="input input-sm w-full">
      <svg lucideSearch class="size-4 opacity-60"></svg>
      <!-- The native 'search' event (Enter / native clear) bubbles to the host and would be taken for our 'search' output, so it is stopped here. -->
      <input type="search" class="grow [&::-webkit-search-cancel-button]:hidden" [placeholder]="placeholder()" [value]="text()"
        (input)="onInput($any($event.target).value)" (search)="$event.stopPropagation()" (keydown.enter)="$event.preventDefault(); onInput($any($event.target).value, true)"
        (keydown.escape)="onInput('')" />
      @if (text()) {
        <button type="button" class="btn btn-ghost btn-xs btn-circle" (click)="onInput('')" [attr.aria-label]="'Common.Clear' | translate">
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

  constructor() {
    effect(() => this.text.set(this.value() ?? ''));
    this._input$
      .pipe(
        // Debounce first so that every keystroke, including too-short ones, cancels a pending obsolete search. Enter skips the wait.
        debounce(({ immediate }) => immediate ? timer(0) : timer(this.debounce())),
        map(({ value }) => value),
        filter(value => value.length === 0 || value.length >= this.minLength()),
        distinctUntilChanged((previous, current) => previous === current && this.value().trim() === current),
        takeUntilDestroyed(inject(DestroyRef))
      )
      .subscribe(value => this.search.emit(value));
  }

  protected onInput(value: string, immediate = false): void {
    this.text.set(value);
    this._input$.next({ value: value.trim(), immediate });
  }
}
