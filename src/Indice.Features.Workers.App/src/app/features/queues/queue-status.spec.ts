import { QueueInfo } from '../../core/services/workers-api.service';
import { describeActivity, formatDuration, formatRate } from './queue-status';

describe('formatRate', () => {
  it('keeps one decimal only below 10/min', () => {
    expect(formatRate(0)).toBe('0/min');
    expect(formatRate(0.5)).toBe('0.5/min');
    expect(formatRate(2)).toBe('2/min');
    expect(formatRate(42.4)).toBe('42/min');
  });

  it('shortens thousands', () => {
    expect(formatRate(1000)).toBe('1k/min');
    expect(formatRate(1250)).toBe('1.3k/min');
  });
});

describe('formatDuration', () => {
  it('picks the unit by size', () => {
    expect(formatDuration(0.4)).toBe('<1 ms');
    expect(formatDuration(120.4)).toBe('120 ms');
    expect(formatDuration(2500)).toBe('2.5 s');
    expect(formatDuration(60_000)).toBe('1 min');
    expect(formatDuration(5_400_000)).toBe('1.5 h');
  });
});

describe('describeActivity', () => {
  const queue = { name: 'q', total: 0 } as QueueInfo;

  it('has nothing to say when no live worker reports metrics', () => {
    expect(describeActivity(queue)).toBeNull();
  });

  it('lists only what was measured', () => {
    expect(describeActivity({ ...queue, processedPerMinute: 12, failedPerMinute: 0, averageDuration: 850 })).toBe(
      'Last minute, as reported by the live workers: 12/min processed, 850 ms average handler time.',
    );
    expect(
      describeActivity({ ...queue, processedPerMinute: 0, failedPerMinute: 1, averageWait: 2000, processing: 1 }),
    ).toBe('Last minute, as reported by the live workers: 0/min processed, 1/min failed, 2 s average wait, 1 message in progress.');
  });
});
