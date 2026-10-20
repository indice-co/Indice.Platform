import { Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FilterClause, OperatorOptions, Operators, SearchOption } from './filtering';

/**
 * Builds filter clauses from the list's search options and shows the active ones as removable chips.
 * Single-term fields replace their previous value; multi-term fields (case types, checkpoints, …) accumulate and are
 * OR-ed by the API. A date range adds a `≥ from` and a `≤ to` clause. Read-only clauses cannot be removed.
 */
@Component({
  selector: 'app-filter-bar',
  imports: [FormsModule, NgIcon, TranslatePipe],
  template: `
    @if (open()) {
      <form class="flex flex-wrap items-end gap-2 rounded-box border border-base-300 bg-base-200/60 p-3" (ngSubmit)="add()">
        <label class="w-full sm:w-52">
          <span class="mb-1 block text-xs text-quiet">{{ 'list.filterField' | translate }}</span>
          <select class="select select-sm w-full" name="field" [ngModel]="field()?.field" (ngModelChange)="selectField($event)">
            <option [ngValue]="undefined" disabled>{{ 'list.chooseField' | translate }}</option>
            @for (option of searchOptions(); track option.field) {
              @if (!option.readonly) {
                <option [ngValue]="option.field">{{ option.name }}</option>
              }
            }
          </select>
        </label>
        @if (field(); as current) {
          @if (current.dataType === 'daterange') {
            <label>
              <span class="mb-1 block text-xs text-quiet">{{ 'list.from' | translate }}</span>
              <input type="date" class="input input-sm w-40" name="from" [max]="dateTo() ?? ''" [ngModel]="dateFrom()" (ngModelChange)="dateFrom.set($event || undefined)" />
            </label>
            <label>
              <span class="mb-1 block text-xs text-quiet">{{ 'list.to' | translate }}</span>
              <input type="date" class="input input-sm w-40" name="to" [min]="dateFrom() ?? ''" [ngModel]="dateTo()" (ngModelChange)="dateTo.set($event || undefined)" />
            </label>
          } @else {
            @if (operators().length > 1) {
              <label class="w-36">
                <span class="mb-1 block text-xs text-quiet">{{ 'list.operator' | translate }}</span>
                <select class="select select-sm w-full" name="operator" [ngModel]="operator()" (ngModelChange)="operator.set($event)">
                  @for (op of operators(); track op.value) {
                    <option [ngValue]="op.value">{{ ('list.op.' + op.value) | translate }}</option>
                  }
                </select>
              </label>
            }
            <label class="min-w-48 flex-1 sm:max-w-72">
              <span class="mb-1 block text-xs text-quiet">{{ 'list.value' | translate }}</span>
              @if (current.dataType === 'array') {
                <select class="select select-sm w-full" name="value" [ngModel]="value()" (ngModelChange)="value.set($event)">
                  <option [ngValue]="undefined" disabled>{{ 'list.chooseValue' | translate }}</option>
                  @for (option of current.options ?? []; track option.value) {
                    <option [ngValue]="option.value">{{ option.label }}</option>
                  }
                </select>
              } @else {
                <input type="text" class="input input-sm w-full" name="value" maxlength="300" autocomplete="off"
                       [placeholder]="current.placeholder ?? ''" [ngModel]="value()" (ngModelChange)="value.set($event || undefined)" />
              }
            </label>
          }
          <button type="submit" class="btn btn-primary btn-sm" [disabled]="!canAdd()">
            <ng-icon name="lucidePlus" size="1rem" /> {{ 'list.addFilter' | translate }}
          </button>
        }
        <button type="button" class="btn btn-ghost btn-sm ml-auto" (click)="close()">{{ 'common.close' | translate }}</button>
      </form>
    }

    @if (chips().length) {
      <div class="flex flex-wrap items-center gap-1.5" [class.mt-3]="open()">
        @for (chip of chips(); track chip.key) {
          <span class="badge badge-lg h-auto gap-1.5 border-base-300 bg-base-100 py-1 pr-1 pl-2.5 text-[0.8125rem] font-normal"
                [class.pr-2.5]="chip.readonly">
            <span class="text-quiet">{{ chip.name }}</span>
            <span class="text-quiet">{{ chip.operator }}</span>
            <span class="font-semibold">{{ chip.value }}</span>
            @if (!chip.readonly) {
              <button type="button" class="btn btn-ghost btn-xs btn-circle size-5 min-h-0" (click)="remove(chip.index)"
                      [attr.aria-label]="('list.removeFilter' | translate) + ' ' + chip.name">
                <ng-icon name="lucideX" size="0.8rem" />
              </button>
            }
          </span>
        }
        @if (removableCount() > 1) {
          <button type="button" class="btn btn-ghost btn-xs text-quiet" (click)="clear()">{{ 'list.clearFilters' | translate }}</button>
        }
      </div>
    }
  `,
  host: { class: 'block empty:hidden' }
})
export class FilterBarComponent {
  public readonly searchOptions = input<SearchOption[]>([]);
  public readonly filters = input<FilterClause[]>([]);
  public readonly open = input(false);
  public readonly filtersChange = output<FilterClause[]>();
  public readonly openChange = output<boolean>();

  protected readonly field = signal<SearchOption | undefined>(undefined);
  protected readonly operator = signal<string>(Operators.EQUALS.value);
  protected readonly value = signal<string | undefined>(undefined);
  protected readonly dateFrom = signal<string | undefined>(undefined);
  protected readonly dateTo = signal<string | undefined>(undefined);

  protected readonly operators = computed(() => {
    const dataType = this.field()?.dataType;
    return dataType && dataType !== 'daterange' ? (OperatorOptions[dataType] ?? OperatorOptions['string']) : [];
  });

  protected readonly canAdd = computed(() => this.field()?.dataType === 'daterange'
    ? !!(this.dateFrom() || this.dateTo())
    : this.value() !== undefined && this.value() !== '');

  /** Chips in search-option order, so names follow the current language. */
  protected readonly chips = computed(() => {
    const filters = this.filters();
    return this.searchOptions().flatMap(option => filters
      .map((filter, index) => ({ filter, index }))
      .filter(({ filter }) => filter.member === option.field)
      .map(({ filter, index }) => ({
        key: `${index}:${filter.toString()}`,
        index,
        name: option.name,
        operator: filter.uiOperator ?? '',
        value: option.options?.find(o => o.value === filter.value)?.label ?? filter.uiValue ?? filter.value,
        readonly: !!option.readonly
      })));
  });

  protected readonly removableCount = computed(() => this.chips().filter(c => !c.readonly).length);

  protected selectField(fieldName: string): void {
    const option = this.searchOptions().find(o => o.field === fieldName);
    this.field.set(option);
    this.operator.set((option && option.dataType !== 'daterange' ? (OperatorOptions[option.dataType ?? 'string'] ?? OperatorOptions['string'])[0].value : Operators.EQUALS.value));
    this.value.set(undefined);
    this.dateFrom.set(undefined);
    this.dateTo.set(undefined);
  }

  protected add(): void {
    const option = this.field();
    if (!option || !this.canAdd()) {
      return;
    }
    let filters = [...this.filters()];
    if (option.dataType === 'daterange') {
      if (!option.multiTerm) {
        filters = filters.filter(f => f.dataType !== 'datetime');
      }
      if (this.dateFrom()) {
        filters.push(new FilterClause(option.field, this.toLocalDate(this.dateFrom()!), Operators.GREATER_THAN_EQUAL.value as FilterClause.Op, 'datetime', this.searchOptions()));
      }
      if (this.dateTo()) {
        filters.push(new FilterClause(option.field, this.toLocalDate(this.dateTo()!), Operators.LESS_THAN_EQUAL.value as FilterClause.Op, 'datetime', this.searchOptions()));
      }
      this.dateFrom.set(undefined);
      this.dateTo.set(undefined);
    } else {
      const clause = new FilterClause(option.field, this.value(), this.operator() as FilterClause.Op, option.dataType, this.searchOptions());
      if (!option.multiTerm) {
        filters = filters.filter(f => f.member !== option.field);
      } else if (filters.some(f => f.toString() === clause.toString())) {
        return;
      }
      filters.push(clause);
      this.value.set(undefined);
    }
    this.filtersChange.emit(filters);
  }

  protected remove(index: number): void {
    this.filtersChange.emit(this.filters().filter((_, i) => i !== index));
  }

  /** Removes every filter the user can remove; read-only ones (e.g. the case type of a case-type page) stay. */
  protected clear(): void {
    const readonlyFields = new Set(this.searchOptions().filter(o => o.readonly).map(o => o.field));
    this.filtersChange.emit(this.filters().filter(f => readonlyFields.has(f.member)));
  }

  protected close(): void {
    this.field.set(undefined);
    this.openChange.emit(false);
  }

  /** A date input value (yyyy-MM-dd) at local midnight, as the previous date picker produced. */
  private toLocalDate(value: string): Date {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day);
  }
}
