import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged, filter } from 'rxjs';
import { LucideSearch, LucideX } from '@lucide/angular';

/** Debounced free-text search box. Emits once the text is empty or at least `minLength` characters long. */
@Component({
  selector: 'app-search-input',
  imports: [LucideSearch, LucideX],
  template: `
    <label class="input input-sm w-full">
      <svg lucideSearch class="size-4 opacity-60"></svg>
      <input type="search" class="grow" [placeholder]="placeholder()" [value]="text()"
        (input)="onInput($any($event.target).value)" (keydown.escape)="onInput('')" />
      @if (text()) {
        <button type="button" class="btn btn-ghost btn-xs btn-circle" (click)="onInput('')" aria-label="Clear">
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
  private readonly _input$ = new Subject<string>();

  constructor() {
    effect(() => this.text.set(this.value() ?? ''));
    this._input$
      .pipe(
        // Debounce first so that every keystroke, including too-short ones, cancels a pending obsolete search
        debounceTime(this.debounce()),
        filter(value => value.length === 0 || value.length >= this.minLength()),
        distinctUntilChanged((previous, current) => previous === current && this.value().trim() === current),
        takeUntilDestroyed(inject(DestroyRef))
      )
      .subscribe(value => this.search.emit(value));
  }

  protected onInput(value: string): void {
    this.text.set(value);
    this._input$.next(value.trim());
  }
}
