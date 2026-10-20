import { formatDate } from '@angular/common';

/**
 * Filter model of the list pages. The string format (`member::op::value`, or `member::op::(type)value`) is what the
 * Cases API and the saved searches (`Query.parameters`) understand, so it must not change.
 */
export namespace Operators {
  export const EQUALS = { label: '=', value: 'eq', description: 'Equal to' };
  export const NOT_EQUALS = { label: '≠', value: 'neq', description: 'Not equal to' };
  export const GREATER_THAN = { label: '>', value: 'gt', description: 'Greater than' };
  export const LESS_THAN = { label: '<', value: 'lt', description: 'Less than' };
  export const GREATER_THAN_EQUAL = { label: '≥', value: 'gte', description: 'Greater than or equal to' };
  export const LESS_THAN_EQUAL = { label: '≤', value: 'lte', description: 'Less than or equal to' };
  export const CONTAINS = { label: '∋', value: 'contains', description: 'Contains' };
  export const IN = { label: '1..n', value: 'in', description: 'In' };
}

export enum QueryParameters {
  VIEW = 'view',
  PAGE_SIZE = 'pagesize',
  PAGE = 'page',
  SEARCH = 'search',
  SORT_FIELD = 'sort',
  SORT_DIRECTION = 'dir',
  FILTER = 'filter'
}

export interface SelectInputOption {
  value: any;
  label: string;
  description?: string;
}

/** A field the user can filter a list by. */
export interface SearchOption {
  name: string;
  field: string;
  dataType: FilterClause.Dt;
  /** The same field can be added more than once (OR-ed by the API), e.g. several case types. */
  multiTerm?: boolean;
  /** Choices for `array` fields. */
  options?: SelectInputOption[];
  placeholder?: string;
  /** Shown as a chip but cannot be removed (e.g. the case type of a case-type page). */
  readonly?: boolean;
}

export namespace FilterClause {
  export type Op = 'eq' | 'neq' | 'gt' | 'lt' | 'gte' | 'lte' | 'contains' | 'in';
  export type Dt = 'string' | 'integer' | 'number' | 'boolean' | 'datetime' | 'array' | 'daterange' | undefined;
}

/** Operators offered per data type, in display order (the first one is the default). */
export const OperatorOptions: { [dataType: string]: { label: string; value: string; description: string }[] } = {
  'string': [Operators.EQUALS, Operators.NOT_EQUALS, Operators.CONTAINS],
  'array': [Operators.EQUALS, Operators.NOT_EQUALS]
};

export class FilterClause {
  /** Member path to compare. */
  public member: string;
  /** Value to compare against. */
  public value: any;
  public operator: FilterClause.Op;
  public dataType: FilterClause.Dt;
  /** Display parts of the chip. */
  public uiName: string | undefined;
  public uiOperator: string | undefined;
  public uiValue: string | undefined;

  public constructor(member: string, value: any, operator?: FilterClause.Op, dataType?: FilterClause.Dt, searchOptions?: SearchOption[]) {
    this.member = member;
    this.value = value;
    this.operator = operator || Operators.EQUALS.value as FilterClause.Op;
    this.dataType = dataType === undefined ? 'string' : dataType;
    const searchOption = searchOptions?.find(option => option.field === this.member);
    this.uiName = searchOption?.name || this.member;
    this.uiOperator = this.dataType === 'datetime'
      ? (this.operator === Operators.GREATER_THAN_EQUAL.value ? Operators.GREATER_THAN_EQUAL.label : Operators.LESS_THAN_EQUAL.label)
      : Object.values(Operators).find(op => op.value === this.operator)?.label;
    this.uiValue = this.dataType === 'datetime' ? formatDate(new Date(this.value), 'dd/MM/yyyy', 'en-US') : this.value;
    if (searchOption?.options) {
      // A field with options is an array field: show the option label instead of its value.
      this.dataType = 'array';
      this.uiValue = searchOption.options.find(option => option.value === this.value)?.label ?? this.value;
    }
  }

  public static parse(text: string): FilterClause | undefined {
    const pattern = /^\s*([A-Za-z_][A-Za-z0-9_\.]+)::(eq|neq|gt|lt|gte|lte|contains|in)::(\((string|integer|number|boolean|datetime)\))?(.+)\s*$/i;
    const matches = text != null ? pattern.exec(text) : null;
    if (!matches) {
      return undefined;
    }
    return new FilterClause(matches[1], matches[5], matches[2] as FilterClause.Op, matches[4] as FilterClause.Dt);
  }

  public toString(): string {
    return this.dataType !== 'string' && this.dataType !== 'array'
      ? `${this.member}::${this.operator}::(${this.dataType})${this.value}`
      : `${this.member}::${this.operator}::${this.value}`;
  }
}

/** Serializes filters for the `filter` query parameter. Dates are normalized to ISO strings. */
export function stringifyFilters(filters: FilterClause[] | undefined): string | undefined {
  if (!filters?.length) {
    return undefined;
  }
  return filters.map(f => {
    if (f.dataType === 'datetime') {
      f.value = new Date(f.value).toISOString();
    }
    return f.toString();
  }).join(',');
}

/** Parses the `filter` query parameter, resolving display names from the search options. */
export function parseFilters(filter: string | null | undefined, searchOptions: SearchOption[]): FilterClause[] {
  if (!filter) {
    return [];
  }
  return filter.split(',')
    .map(text => FilterClause.parse(text))
    .filter((clause): clause is FilterClause => !!clause)
    .map(clause => new FilterClause(clause.member, clause.value, clause.operator, clause.dataType, searchOptions));
}

/** A page of results, as returned by the list endpoints. */
export interface IResultSet<T> {
  count?: number;
  items?: T[];
}
