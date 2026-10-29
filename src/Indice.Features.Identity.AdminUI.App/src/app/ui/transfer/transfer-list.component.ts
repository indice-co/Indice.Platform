import { Component, computed, input, output, signal } from '@angular/core';
import { LucideMinus, LucidePlus, LucideSearch } from '@lucide/angular';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Two column picker: candidates on the left, assigned items on the right. The component never mutates its inputs;
 * the host reacts to `added` / `removed` (usually by calling the API) and then updates `selected`.
 * Candidates already present in `selected` (matched by `key`) are hidden from the left column.
 */
@Component({
  selector: 'app-transfer-list',
  imports: [LucideMinus, LucidePlus, LucideSearch, TranslatePipe],
  host: { class: 'grid gap-4 md:grid-cols-2' },
  template: `
    <section class="panel flex min-h-0 flex-col">
      <header class="flex items-center justify-between gap-3 border-b border-base-300 px-4 py-3">
        <h3 class="text-sm font-semibold">{{ availableTitle() || ('Ui.TransferList.Available' | translate) }}</h3>
        <span class="badge badge-sm badge-ghost tabular">{{ candidates().length }}</span>
      </header>
      <div class="border-b border-base-300 p-2">
        <label class="input input-sm w-full">
          <svg lucideSearch class="size-4 opacity-60"></svg>
          <input type="search" class="grow" [placeholder]="'Ui.TransferList.FilterPlaceholder' | translate" [value]="filter()" (input)="filter.set($any($event.target).value)" />
        </label>
      </div>
      <ul class="max-h-80 flex-1 divide-y divide-base-200 overflow-y-auto">
        @for (item of filteredCandidates(); track key()(item)) {
          <li class="flex items-center gap-3 px-4 py-2">
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium">{{ label()(item) }}</p>
              @if (sublabel()?.(item); as sub) { <p class="truncate text-xs text-base-content/60">{{ sub }}</p> }
            </div>
            @if (!readonly()) {
              <button type="button" class="btn btn-ghost btn-xs btn-square text-primary" [attr.aria-label]="'Ui.TransferList.AddItem' | translate: { label: label()(item) }"
                (click)="added.emit(item)">
                <svg lucidePlus class="size-4"></svg>
              </button>
            }
          </li>
        } @empty {
          <li class="px-4 py-8 text-center text-sm text-base-content/60">{{ filter() ? ('Ui.TransferList.NoMatches' | translate) : (emptyAvailable() || ('Ui.TransferList.EmptyAvailable' | translate)) }}</li>
        }
      </ul>
    </section>

    <section class="panel flex min-h-0 flex-col">
      <header class="flex items-center justify-between gap-3 border-b border-base-300 px-4 py-3">
        <h3 class="text-sm font-semibold">{{ selectedTitle() || ('Ui.TransferList.Assigned' | translate) }}</h3>
        <span class="badge badge-sm badge-primary badge-soft tabular">{{ selected().length }}</span>
      </header>
      <ul class="max-h-[23.25rem] flex-1 divide-y divide-base-200 overflow-y-auto">
        @for (item of selected(); track key()(item)) {
          <li class="flex items-center gap-3 px-4 py-2">
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium">{{ label()(item) }}</p>
              @if (sublabel()?.(item); as sub) { <p class="truncate text-xs text-base-content/60">{{ sub }}</p> }
            </div>
            @if (!readonly()) {
              <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Ui.TransferList.RemoveItem' | translate: { label: label()(item) }"
                (click)="removed.emit(item)">
                <svg lucideMinus class="size-4"></svg>
              </button>
            }
          </li>
        } @empty {
          <li class="px-4 py-8 text-center text-sm text-base-content/60">{{ emptySelected() || ('Ui.TransferList.EmptySelected' | translate) }}</li>
        }
      </ul>
    </section>
  `
})
export class TransferListComponent<T> {
  public readonly available = input<T[]>([]);
  public readonly selected = input<T[]>([]);
  public readonly key = input.required<(item: T) => string>();
  public readonly label = input.required<(item: T) => string>();
  public readonly sublabel = input<(item: T) => string | null | undefined>();
  /** Defaults to the translated `Available`. */
  public readonly availableTitle = input('');
  /** Defaults to the translated `Assigned`. */
  public readonly selectedTitle = input('');
  /** Defaults to the translated `Nothing left to add.`. */
  public readonly emptyAvailable = input('');
  /** Defaults to the translated `Nothing assigned yet.`. */
  public readonly emptySelected = input('');
  public readonly readonly = input(false);
  public readonly added = output<T>();
  public readonly removed = output<T>();

  protected readonly filter = signal('');
  protected readonly candidates = computed(() => {
    const key = this.key();
    const taken = new Set(this.selected().map(key));
    return this.available().filter(item => !taken.has(key(item)));
  });
  protected readonly filteredCandidates = computed(() => {
    const term = this.filter().trim().toLowerCase();
    if (!term) {
      return this.candidates();
    }
    const label = this.label();
    const sublabel = this.sublabel();
    return this.candidates().filter(item => label(item)?.toLowerCase().includes(term) || sublabel?.(item)?.toLowerCase().includes(term));
  });
}
