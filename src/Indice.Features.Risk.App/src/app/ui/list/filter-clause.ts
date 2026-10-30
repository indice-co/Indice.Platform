export type FilterOperator = 'eq' | 'neq' | 'contains';

export const OPERATOR_LABELS: Record<FilterOperator, string> = {
  eq: 'is',
  neq: 'is not',
  contains: 'contains'
};

/** A field the user can filter a list by. */
export interface FilterField {
  field: string;
  label: string;
  /** Defaults to `eq`, `neq` and `contains`. */
  operators?: FilterOperator[];
  placeholder?: string;
}

/** Members of the date range clauses understood by the Risk API (`from::eq::…`, `to::eq::…`). */
export const DATE_FROM = 'from';
export const DATE_TO = 'to';

/**
 * A list filter in the `member::op::value` form used both in the `filter` URL query param (comma separated) and in the
 * API's `Filter` parameters. Date range clauses keep a `yyyy-MM-dd` value in the URL and become ISO instants for the API.
 */
export class FilterClause {
  constructor(public readonly member: string, public readonly value: string, public readonly operator: FilterOperator = 'eq') { }

  public static parse(text: string): FilterClause | undefined {
    const matches = /^\s*([A-Za-z_][A-Za-z0-9_.]*)::(eq|neq|contains)::(.+?)\s*$/i.exec(text);
    return matches ? new FilterClause(matches[1], matches[3], matches[2].toLowerCase() as FilterOperator) : undefined;
  }

  public static parseList(text: string | null | undefined): FilterClause[] {
    return (text ?? '')
      .split(',')
      .map(part => FilterClause.parse(part))
      .filter((clause): clause is FilterClause => !!clause);
  }

  public static stringifyList(filters: FilterClause[]): string {
    return filters.map(f => f.toString()).join(',');
  }

  /** API filter terms for a list of clauses. */
  public static toApiTerms(filters: FilterClause[]): string[] {
    return filters.map(f => f.toApiTerm());
  }

  public get isDate(): boolean {
    return this.member === DATE_FROM || this.member === DATE_TO;
  }

  public toApiTerm(): string {
    if (this.isDate) {
      const [year, month, day] = this.value.split('-').map(Number);
      // `to` is inclusive: the whole selected day, in the user's time zone
      const instant = this.member === DATE_TO ? new Date(year, month - 1, day, 23, 59, 59, 999) : new Date(year, month - 1, day);
      return `${this.member}::${this.operator}::${instant.toISOString()}`;
    }
    return this.toString();
  }

  public toString(): string {
    return `${this.member}::${this.operator}::${this.value}`;
  }
}
