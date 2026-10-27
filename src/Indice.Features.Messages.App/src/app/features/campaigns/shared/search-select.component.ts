import { NgTemplateOutlet } from '@angular/common';
import { Component, DestroyRef, TemplateRef, contentChild, inject, input, model, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, debounceTime } from 'rxjs';
import { LucideChevronsUpDown, LucideX } from '@lucide/angular';

export interface SearchSelectItem<T = unknown> {
  id: string;
  label: string;
  data?: T;
}

/**
 * Single select combobox with server side search and "show more" paging. The host loads `items` in response to
 * `search` (emitted with `''` when the list opens) and `showMore`. Project an `<ng-template let-item>` to customize
 * how options render.
 */
@Component({
  selector: 'app-search-select',
  imports: [NgTemplateOutlet, TranslatePipe, LucideChevronsUpDown, LucideX],
  host: { class: 'relative block' },
  template: `
    <label class="input w-full" [class.input-error]="invalid()">
      <input class="grow" role="combobox" autocomplete="off" [attr.id]="inputId()" [attr.aria-expanded]="open()"
        [placeholder]="placeholder()" [value]="open() ? query() : (selected()?.label ?? '')"
        (focus)="openList()" (blur)="open.set(false)" (input)="onInput($any($event.target).value)" (keydown.escape)="open.set(false)" />
      @if (loading()) { <span class="loading loading-spinner loading-xs opacity-60"></span> }
      @if (selected()) {
        <button type="button" class="btn btn-ghost btn-xs btn-circle" (mousedown)="$event.preventDefault()" (click)="clear()"
          [attr.aria-label]="'Common.Close' | translate">
          <svg lucideX class="size-3.5"></svg>
        </button>
      }
      <svg lucideChevronsUpDown class="size-4 opacity-50"></svg>
    </label>
    @if (open()) {
      <ul role="listbox" (mousedown)="$event.preventDefault()"
        class="menu absolute inset-x-0 top-full z-30 mt-1 max-h-72 flex-nowrap overflow-y-auto rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
        @for (item of items(); track item.id) {
          <li>
            <button type="button" role="option" [attr.aria-selected]="item.id === selected()?.id"
              [class.menu-active]="item.id === selected()?.id" (click)="select(item)">
              @if (itemTemplate(); as template) {
                <ng-container *ngTemplateOutlet="template; context: { $implicit: item }" />
              } @else {
                <span class="truncate">{{ item.label }}</span>
              }
            </button>
          </li>
        } @empty {
          <li class="menu-disabled"><span>{{ (loading() ? 'Home.PleaseWait' : 'List.NoResults') | translate }}</span></li>
        }
        @if (hasMore()) {
          <li>
            <button type="button" class="justify-center text-primary" [disabled]="loading()" (click)="showMore.emit()">
              {{ 'Campaigns.ShowMore' | translate }}
            </button>
          </li>
        }
      </ul>
    }
  `
})
export class SearchSelectComponent<T = unknown> {
  public readonly items = input<SearchSelectItem<T>[]>([]);
  public readonly selected = model<SearchSelectItem<T> | null>(null);
  public readonly loading = input(false);
  public readonly hasMore = input(false);
  public readonly invalid = input(false);
  public readonly placeholder = input('');
  public readonly inputId = input<string>();
  public readonly search = output<string>();
  public readonly showMore = output<void>();

  protected readonly itemTemplate = contentChild(TemplateRef);
  protected readonly open = signal(false);
  protected readonly query = signal('');
  private readonly _query$ = new Subject<string>();

  constructor() {
    this._query$
      .pipe(debounceTime(300), takeUntilDestroyed(inject(DestroyRef)))
      .subscribe(query => this.search.emit(query));
  }

  protected openList(): void {
    if (this.open()) {
      return;
    }
    this.open.set(true);
    this.query.set('');
    this.search.emit('');
  }

  protected onInput(value: string): void {
    this.open.set(true);
    this.query.set(value);
    this._query$.next(value.trim());
  }

  protected select(item: SearchSelectItem<T>): void {
    this.selected.set(item);
    this.open.set(false);
  }

  protected clear(): void {
    this.selected.set(null);
    this.query.set('');
  }
}
