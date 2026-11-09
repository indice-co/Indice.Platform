import { Component, computed, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideListFilter, LucidePlus, LucideX } from '@lucide/angular';
import { FilterClause, FilterOperator, OperatorInfo, OperatorOptions, Operators, SearchOption } from './filter-clause';

/**
 * Filter chips plus an inline "add filter" builder.
 *
 * Supports `string` (operator + text), `array` (operator + option) and `daterange` (from / to) search options.
 * Emits the complete new list of clauses; the owner decides how to apply them (usually `ListState.setFilters`).
 */
@Component({
  selector: 'app-filter-bar',
  imports: [DatePipe, TranslatePipe, LucideListFilter, LucidePlus, LucideX],
  host: { class: 'block' },
  template: `
    <div class="flex flex-wrap items-center gap-2">
      <button type="button" class="btn btn-sm" [class.btn-active]="open()" (click)="toggle()" [disabled]="!searchOptions().length">
        <svg lucideListFilter class="size-4"></svg>
        {{ 'filters.add' | translate }}
      </button>

      @for (chip of chips(); track chip.key) {
        <span class="badge badge-lg gap-1.5 border-base-300 bg-base-100 font-normal" [class.opacity-70]="chip.readonly">
          <span class="text-base-content/60">{{ chip.name }}</span>
          <span class="text-base-content/50">{{ chip.operator }}</span>
          <span class="font-medium">
            @if (chip.isDate) { {{ chip.value | date: 'dd/MM/yyyy' }} } @else { {{ chip.value }} }
          </span>
          @if (!chip.readonly) {
            <button type="button" class="btn btn-ghost btn-xs btn-circle -mr-1" (click)="remove(chip.index)"
              [attr.aria-label]="'filters.remove' | translate">
              <svg lucideX class="size-3"></svg>
            </button>
          }
        </span>
      }

      @if (removableCount() > 1) {
        <button type="button" class="btn btn-ghost btn-sm" (click)="clearAll()">{{ 'filters.clearAll' | translate }}</button>
      }
    </div>

    @if (open()) {
      <form class="mt-3 flex flex-wrap items-end gap-3 rounded-box border border-base-300 bg-base-100 p-3" (submit)="$event.preventDefault(); apply()">
        <label class="w-full sm:w-52">
          <span class="mb-1 block text-xs text-base-content/70">{{ 'filters.field' | translate }}</span>
          <select class="select select-sm w-full" [value]="field()?.field ?? ''" (change)="selectField($any($event.target).value)">
            <option value="" disabled>{{ 'filters.chooseField' | translate }}</option>
            @for (option of searchOptions(); track option.field) {
              <option [value]="option.field">{{ option.name }}</option>
            }
          </select>
        </label>

        @if (field(); as selected) {
          @if (selected.dataType === 'daterange') {
            <label>
              <span class="mb-1 block text-xs text-base-content/70">{{ 'filters.from' | translate }}</span>
              <input type="date" class="input input-sm" [max]="dateTo()" [value]="dateFrom()" (input)="dateFrom.set($any($event.target).value)" />
            </label>
            <label>
              <span class="mb-1 block text-xs text-base-content/70">{{ 'filters.to' | translate }}</span>
              <input type="date" class="input input-sm" [min]="dateFrom()" [value]="dateTo()" (input)="dateTo.set($any($event.target).value)" />
            </label>
          } @else {
            @if (operators().length > 1) {
              <label class="w-full sm:w-40">
                <span class="mb-1 block text-xs text-base-content/70">{{ 'filters.operator' | translate }}</span>
                <select class="select select-sm w-full" [value]="operator()" (change)="operator.set($any($event.target).value)">
                  @for (op of operators(); track op.value) {
                    <option [value]="op.value">{{ op.key | translate }}</option>
                  }
                </select>
              </label>
            }
            <label class="min-w-48 flex-1">
              <span class="mb-1 block text-xs text-base-content/70">{{ 'filters.value' | translate }}</span>
              @if (selected.dataType === 'array') {
                <select class="select select-sm w-full" [value]="value()" (change)="value.set($any($event.target).value)">
                  <option value="" disabled>{{ 'filters.chooseValue' | translate }}</option>
                  @for (option of selected.options ?? []; track option.value) {
                    <option [value]="option.value">{{ option.label }}</option>
                  }
                </select>
              } @else {
                <input type="text" class="input input-sm w-full" maxlength="300" [placeholder]="selected.placeholder ?? ''"
                  [value]="value()" (input)="value.set($any($event.target).value)" />
              }
            </label>
          }
          <button type="submit" class="btn btn-primary btn-sm" [disabled]="!canApply()">
            <svg lucidePlus class="size-4"></svg>
            {{ 'filters.apply' | translate }}
          </button>
        }
        <button type="button" class="btn btn-ghost btn-sm" (click)="toggle()">{{ 'common.cancel' | translate }}</button>
      </form>
    }
  `
})
export class FilterBarComponent {
  public readonly searchOptions = input<SearchOption[]>([]);
  public readonly filters = input<FilterClause[]>([]);
  public readonly filtersChange = output<FilterClause[]>();

  protected readonly open = signal(false);
  protected readonly field = signal<SearchOption | undefined>(undefined);
  protected readonly operator = signal<FilterOperator>('eq');
  protected readonly value = signal('');
  protected readonly dateFrom = signal('');
  protected readonly dateTo = signal('');

  protected readonly operators = computed<OperatorInfo[]>(() => OperatorOptions[this.field()?.dataType ?? 'string'] ?? [Operators.EQUALS]);

  protected readonly canApply = computed(() => {
    const field = this.field();
    if (!field) {
      return false;
    }
    return field.dataType === 'daterange' ? !!(this.dateFrom() || this.dateTo()) : this.value().trim().length > 0;
  });

  /** Only clauses that match a known search option are shown, in search option order. */
  protected readonly chips = computed(() => {
    const options = this.searchOptions();
    const filters = this.filters();
    return filters
      .map((clause, index) => ({ clause, index, option: options.find(o => o.field === clause.member) }))
      .filter(x => !!x.option)
      .map(({ clause, index, option }) => ({
        key: `${index}-${clause.toString()}`,
        index,
        name: option!.name,
        operator: clause.operatorLabel,
        value: option!.options?.find(o => `${o.value}` === `${clause.value}`)?.label ?? clause.value,
        isDate: clause.dataType === 'datetime',
        readonly: !!option!.readonly
      }));
  });

  protected readonly removableCount = computed(() => this.chips().filter(c => !c.readonly).length);

  protected toggle(): void {
    this.open.update(open => !open);
    this.reset();
  }

  protected selectField(field: string): void {
    this.field.set(this.searchOptions().find(o => o.field === field));
    this.operator.set(this.operators()[0]?.value ?? 'eq');
    this.value.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
  }

  protected apply(): void {
    const field = this.field();
    if (!field || !this.canApply()) {
      return;
    }
    let filters = [...this.filters()];
    if (field.dataType === 'daterange') {
      filters = filters.filter(f => f.member !== field.field);
      if (this.dateFrom()) {
        filters.push(new FilterClause(field.field, new Date(this.dateFrom()), Operators.GREATER_THAN_EQUAL.value, 'datetime'));
      }
      if (this.dateTo()) {
        filters.push(new FilterClause(field.field, new Date(this.dateTo()), Operators.LESS_THAN_EQUAL.value, 'datetime'));
      }
    } else {
      const clause = new FilterClause(field.field, this.value().trim(), this.operator(), field.dataType === 'array' ? 'array' : 'string');
      if (!field.multiTerm) {
        filters = filters.filter(f => f.member !== field.field);
      } else if (filters.some(f => f.toString() === clause.toString())) {
        this.reset();
        return;
      }
      filters.push(clause);
    }
    this.filtersChange.emit(filters);
    this.open.set(false);
    this.reset();
  }

  protected remove(index: number): void {
    this.filtersChange.emit(this.filters().filter((_, i) => i !== index));
  }

  protected clearAll(): void {
    const readonlyFields = new Set(this.searchOptions().filter(o => o.readonly).map(o => o.field));
    this.filtersChange.emit(this.filters().filter(f => readonlyFields.has(f.member)));
  }

  private reset(): void {
    this.field.set(undefined);
    this.value.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
  }
}
