/**
 * Filter model for list pages.
 *
 * The wire format (`member::op::(type)value`, comma separated in the `filter` query param) is kept identical to
 * the one previously produced by `@indice/ng-components`, so saved searches and bookmarked URLs keep working.
 */

export type FilterOperator = 'eq' | 'neq' | 'gt' | 'lt' | 'gte' | 'lte' | 'contains' | 'in';
export type FilterDataType = 'string' | 'integer' | 'number' | 'boolean' | 'datetime' | 'array';

export interface OperatorInfo {
  label: string;
  value: FilterOperator;
  /** Translation key for the operator name. */
  key: string;
}

export const Operators = {
  EQUALS: { label: '=', value: 'eq', key: 'filters.op.eq' },
  NOT_EQUALS: { label: '≠', value: 'neq', key: 'filters.op.neq' },
  GREATER_THAN: { label: '>', value: 'gt', key: 'filters.op.gt' },
  LESS_THAN: { label: '<', value: 'lt', key: 'filters.op.lt' },
  GREATER_THAN_EQUAL: { label: '≥', value: 'gte', key: 'filters.op.gte' },
  LESS_THAN_EQUAL: { label: '≤', value: 'lte', key: 'filters.op.lte' },
  CONTAINS: { label: '∋', value: 'contains', key: 'filters.op.contains' },
  IN: { label: '1..n', value: 'in', key: 'filters.op.in' }
} satisfies Record<string, OperatorInfo>;

/** Operators offered by the filter bar per search option data type. */
export const OperatorOptions: Record<string, OperatorInfo[]> = {
  string: [Operators.EQUALS, Operators.NOT_EQUALS, Operators.CONTAINS],
  array: [Operators.EQUALS, Operators.NOT_EQUALS]
};

export interface SelectOption {
  value: any;
  label: string;
  description?: string;
}

/** Describes a field the user can filter a list by. */
export interface SearchOption {
  field: string;
  name: string;
  dataType: 'string' | 'array' | 'daterange' | string;
  options?: SelectOption[];
  /** Allow more than one clause for the same field. */
  multiTerm?: boolean;
  /** Clause is shown but cannot be removed by the user. */
  readonly?: boolean;
  placeholder?: string;
}

export class FilterClause {
  public member: string;
  public value: any;
  public operator: FilterOperator;
  public dataType: FilterDataType;

  constructor(member: string, value: any, operator?: FilterOperator, dataType?: FilterDataType) {
    this.member = member;
    this.value = value;
    this.operator = operator || 'eq';
    this.dataType = dataType ?? 'string';
  }

  public static parse(text: string | null | undefined): FilterClause | undefined {
    if (!text) {
      return undefined;
    }
    const pattern = /^\s*([A-Za-z_][A-Za-z0-9_.]+)::(eq|neq|gt|lt|gte|lte|contains|in)::(\((string|integer|number|boolean|datetime)\))?(.+)\s*$/i;
    const matches = pattern.exec(text);
    if (!matches) {
      return undefined;
    }
    return new FilterClause(matches[1], matches[5], matches[2] as FilterOperator, matches[4] as FilterDataType | undefined);
  }

  /** Parses the comma separated `filter` query param. */
  public static parseList(text: string | null | undefined): FilterClause[] {
    return (text ?? '')
      .split(',')
      .map(part => FilterClause.parse(part))
      .filter((clause): clause is FilterClause => !!clause);
  }

  public static stringifyList(filters: FilterClause[] | undefined): string {
    return (filters ?? []).map(f => f.toString()).join(',');
  }

  /** The `member::op::value` form expected by the API's list filter parameters. */
  public toApiTerm(): string {
    return `${this.member}::${this.operator}::${this.value}`;
  }

  public toString(): string {
    if (this.dataType === 'datetime') {
      return `${this.member}::${this.operator}::(datetime)${new Date(this.value).toISOString()}`;
    }
    if (this.dataType !== 'string' && this.dataType !== 'array') {
      return `${this.member}::${this.operator}::(${this.dataType})${this.value}`;
    }
    return `${this.member}::${this.operator}::${this.value}`;
  }

  public get operatorLabel(): string {
    return Object.values(Operators).find(o => o.value === this.operator)?.label ?? this.operator;
  }
}
