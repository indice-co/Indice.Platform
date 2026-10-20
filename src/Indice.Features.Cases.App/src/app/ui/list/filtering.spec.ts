import { FilterClause, parseFilters, SearchOption, stringifyFilters } from './filtering';

describe('FilterClause', () => {
  it('round-trips the query-string format the API and saved searches use', () => {
    const clause = FilterClause.parse('caseTypeCodes::eq::MORTGAGE')!;
    expect(clause.member).toBe('caseTypeCodes');
    expect(clause.operator).toBe('eq');
    expect(clause.value).toBe('MORTGAGE');
    expect(clause.toString()).toBe('caseTypeCodes::eq::MORTGAGE');
  });

  it('keeps the data type of typed values', () => {
    const clause = FilterClause.parse('dateRange::gte::(datetime)2026-10-01T00:00:00.000Z')!;
    expect(clause.dataType).toBe('datetime');
    expect(clause.toString()).toBe('dateRange::gte::(datetime)2026-10-01T00:00:00.000Z');
  });

  it('rejects text that is not a clause', () => {
    expect(FilterClause.parse('not a filter')).toBeUndefined();
  });

  it('shows option labels for array fields', () => {
    const options: SearchOption[] = [{ field: 'caseTypeCodes', name: 'Case type', dataType: 'array', options: [{ value: 'MORTGAGE', label: 'Mortgage application' }] }];
    const clause = new FilterClause('caseTypeCodes', 'MORTGAGE', 'eq', 'array', options);
    expect(clause.uiName).toBe('Case type');
    expect(clause.uiValue).toBe('Mortgage application');
  });
});

describe('filter query parameter', () => {
  it('serializes several clauses and parses them back', () => {
    const filters = [
      new FilterClause('caseTypeCodes', 'MORTGAGE', 'eq', 'array'),
      new FilterClause('ownerName', 'Παπα', 'contains', 'string'),
      new FilterClause('dateRange', new Date(Date.UTC(2026, 9, 1)), 'gte', 'datetime')
    ];
    const text = stringifyFilters(filters)!;
    expect(text).toBe('caseTypeCodes::eq::MORTGAGE,ownerName::contains::Παπα,dateRange::gte::(datetime)2026-10-01T00:00:00.000Z');
    expect(parseFilters(text, []).map(f => f.toString()).join(',')).toBe(text);
  });

  it('is empty without filters', () => {
    expect(stringifyFilters([])).toBeUndefined();
    expect(parseFilters(null, [])).toEqual([]);
  });
});
