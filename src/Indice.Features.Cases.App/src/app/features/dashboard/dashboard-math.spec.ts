import { CaseStatus } from 'src/app/core/services/cases-api.service';
import { ageBands, daysBetween, foldRoute, listQueryParams, niceMax, openRoute, percentDelta, weekWindows } from './dashboard-math';

describe('dashboard math', () => {
  const today = new Date(2026, 9, 20, 15, 30);

  it('splits the last weeks into contiguous seven-day windows ending today', () => {
    const weeks = weekWindows(today, 8);
    expect(weeks.length).toBe(8);
    expect(weeks[7].to.getDate()).toBe(20);
    expect(weeks[7].from.getDate()).toBe(14);
    for (let i = 1; i < weeks.length; i++) {
      // Each window starts the millisecond after the previous one ends.
      expect(weeks[i].from.getTime() - weeks[i - 1].to.getTime()).toBe(1);
    }
  });

  it('orders age bands from youngest to oldest without gaps', () => {
    const bands = ageBands(today);
    expect(bands[0].to).toBeUndefined();
    expect(bands[3].from).toBeUndefined();
    expect(bands[1].to!.getTime()).toBe(bands[0].from!.getTime());
    expect(bands[2].to!.getTime()).toBe(bands[1].from!.getTime());
    expect(bands[3].to!.getTime()).toBe(bands[2].from!.getTime());
    expect(daysBetween(bands[3].to!, today)).toBe(30);
  });

  it('computes rounded percent change, or nothing without a baseline', () => {
    expect(percentDelta(65, 31)).toBe(110);
    expect(percentDelta(10, 20)).toBe(-50);
    expect(percentDelta(5, 0)).toBeNull();
  });

  it('rounds axis maxima to clean numbers', () => {
    expect(niceMax(3)).toBe(4);
    expect(niceMax(34)).toBe(40);
    expect(niceMax(65)).toBe(80);
    expect(niceMax(120)).toBe(150);
  });

  it('builds case-list drill-downs in the list filter format', () => {
    const params = listQueryParams({ checkpointTypeCodes: ['Submitted', 'UnderReview'], window: { from: new Date(Date.UTC(2026, 9, 1)) } });
    expect(params['filter']).toBe('checkpointTypeCodes::eq::Submitted,checkpointTypeCodes::eq::UnderReview,dateRange::gte::(datetime)2026-10-01T00:00:00.000Z');
    expect(params['page']).toBe(1);
    expect(params['sort']).toBe('createdByWhen');
  });

  it('lists open checkpoints once each, Submitted before In progress', () => {
    const route = openRoute([
      { code: 'UnderReview', title: 'Under review', status: CaseStatus.InProgress },
      { code: 'Approved', title: 'Approved', status: CaseStatus.Completed },
      { code: 'Submitted', title: 'Received', status: CaseStatus.Submitted },
      { code: 'UnderReview', title: 'Under review (cards)', status: CaseStatus.InProgress },
      { code: 'PendingDocuments', status: CaseStatus.InProgress }
    ]);
    expect(route.map(stop => stop.code)).toEqual(['Submitted', 'UnderReview', 'PendingDocuments']);
    expect(route[2].title).toBe('PendingDocuments');
  });

  it('folds the quietest stops of a long route into one, keeping the total', () => {
    const stops = [5, 40, 1, 12, 3, 30, 2].map((value, i) => ({ key: `s${i}`, value }));
    const folded = foldRoute(stops, 4, rest => ({ key: 'other', value: rest.reduce((sum, stop) => sum + stop.value, 0) }));
    expect(folded.map(stop => stop.key)).toEqual(['s1', 's3', 's5', 'other']);
    expect(folded.reduce((sum, stop) => sum + (stop.value ?? 0), 0)).toBe(93);
    expect(foldRoute(stops.slice(0, 3), 4, () => ({ key: 'x', value: 0 }))).toHaveLength(3);
  });
});
