import { Component, computed, input, output, signal } from '@angular/core';
import { LucideCalendar, LucideListFilter, LucideX } from '@lucide/angular';
import { DATE_FROM, DATE_TO, FilterClause, FilterField, FilterOperator, OPERATOR_LABELS } from './filter-clause';

/**
 * Filter toolbar: a created-at date range, an "Add filter" popover for field filters and the active filters as removable chips.
 * Project extra toolbar content (e.g. a refresh button) with `[actions]`.
 */
@Component({
  selector: 'app-filter-bar',
  imports: [LucideCalendar, LucideListFilter, LucideX],
  host: { class: 'block space-y-3' },
  template: `
    <div class="flex flex-wrap items-center gap-2">
      <div class="join join-vertical w-full sm:join-horizontal sm:w-auto">
        <label class="join-item input input-sm w-full sm:w-auto">
          <svg lucideCalendar class="size-4 opacity-60"></svg>
          <span class="text-base-content/60">From</span>
          <input type="date" class="grow tabular" [value]="dateFrom()" [max]="dateTo() || null" (change)="setDate(fromMember, $any($event.target).value)" />
        </label>
        <label class="join-item input input-sm w-full sm:w-auto">
          <span class="text-base-content/60">To</span>
          <input type="date" class="grow tabular" [value]="dateTo()" [min]="dateFrom() || null" (change)="setDate(toMember, $any($event.target).value)" />
        </label>
      </div>

      <div class="dropdown">
        <button type="button" tabindex="0" class="btn btn-sm btn-ghost border-base-300 gap-1.5" (focus)="resetDraft()">
          <svg lucideListFilter class="size-4"></svg>Add filter
        </button>
        <form tabindex="0" class="dropdown-content z-30 mt-2 w-72 space-y-3 rounded-box border border-base-300 bg-base-100 p-4 shadow-lg"
          (submit)="$event.preventDefault(); apply()">
          <label class="block space-y-1">
            <span class="text-xs font-medium text-base-content/70">Field</span>
            <select class="select select-sm w-full" (change)="draftField.set($any($event.target).value); draftOperator.set('eq')">
              @for (field of fields(); track field.field) {
                <option [value]="field.field" [selected]="field.field === draftField()">{{ field.label }}</option>
              }
            </select>
          </label>
          <label class="block space-y-1">
            <span class="text-xs font-medium text-base-content/70">Condition</span>
            <select class="select select-sm w-full" (change)="draftOperator.set($any($event.target).value)">
              @for (operator of draftOperators(); track operator) {
                <option [value]="operator" [selected]="operator === draftOperator()">{{ operatorLabels[operator] }}</option>
              }
            </select>
          </label>
          <label class="block space-y-1">
            <span class="text-xs font-medium text-base-content/70">Value</span>
            <input type="text" class="input input-sm w-full" [placeholder]="draftPlaceholder()" [value]="draftValue()"
              (input)="draftValue.set($any($event.target).value)" />
          </label>
          <button type="submit" class="btn btn-primary btn-sm w-full" [disabled]="!draftValue().trim()">Apply filter</button>
        </form>
      </div>

      <div class="ml-auto flex items-center gap-2">
        <ng-content select="[actions]" />
      </div>
    </div>

    @if (fieldFilters().length > 0 || hasDates()) {
      <div class="flex flex-wrap items-center gap-2">
        @for (clause of fieldFilters(); track clause.toString()) {
          <span class="badge badge-soft badge-primary gap-1 py-3 pr-1 pl-2.5">
            <span class="font-semibold">{{ labelOf(clause.member) }}</span>
            <span class="opacity-75">{{ operatorLabels[clause.operator] }}</span>
            <span class="max-w-48 truncate font-mono text-xs">{{ clause.value }}</span>
            <button type="button" class="btn btn-ghost btn-xs btn-circle" [attr.aria-label]="'Remove ' + labelOf(clause.member) + ' filter'"
              (click)="remove(clause)">
              <svg lucideX class="size-3"></svg>
            </button>
          </span>
        }
        <button type="button" class="btn btn-link btn-xs text-base-content/60" (click)="filtersChange.emit([])">Clear all</button>
      </div>
    }
  `
})
export class FilterBarComponent {
  public readonly fields = input.required<FilterField[]>();
  public readonly filters = input.required<FilterClause[]>();
  public readonly filtersChange = output<FilterClause[]>();

  protected readonly operatorLabels = OPERATOR_LABELS;
  protected readonly fromMember = DATE_FROM;
  protected readonly toMember = DATE_TO;
  protected readonly dateFrom = computed(() => this.filters().find(f => f.member === DATE_FROM)?.value ?? '');
  protected readonly dateTo = computed(() => this.filters().find(f => f.member === DATE_TO)?.value ?? '');
  protected readonly hasDates = computed(() => !!this.dateFrom() || !!this.dateTo());
  protected readonly fieldFilters = computed(() => this.filters().filter(f => !f.isDate));

  protected readonly draftField = signal('');
  protected readonly draftOperator = signal<FilterOperator>('eq');
  protected readonly draftValue = signal('');
  private readonly _draftDefinition = computed(() => this.fields().find(f => f.field === this.draftField()));
  protected readonly draftOperators = computed(() => this._draftDefinition()?.operators ?? (['eq', 'neq', 'contains'] as FilterOperator[]));
  protected readonly draftPlaceholder = computed(() => this._draftDefinition()?.placeholder ?? '');

  protected labelOf(member: string): string {
    return this.fields().find(f => f.field.toLowerCase() === member.toLowerCase())?.label ?? member;
  }

  protected resetDraft(): void {
    if (!this._draftDefinition()) {
      this.draftField.set(this.fields()[0]?.field ?? '');
    }
  }

  protected apply(): void {
    // commas separate clauses in the URL, so they cannot be part of a value
    const value = this.draftValue().replaceAll(',', ' ').trim();
    if (!value || !this.draftField()) {
      return;
    }
    const clause = new FilterClause(this.draftField(), value, this.draftOperator());
    this.filtersChange.emit([...this.filters().filter(f => f.toString() !== clause.toString()), clause]);
    this.draftValue.set('');
    (document.activeElement as HTMLElement | null)?.blur();
  }

  protected remove(clause: FilterClause): void {
    this.filtersChange.emit(this.filters().filter(f => f !== clause));
  }

  protected setDate(member: string, value: string): void {
    const others = this.filters().filter(f => f.member !== member);
    this.filtersChange.emit(value ? [...others, new FilterClause(member, value)] : others);
  }
}
