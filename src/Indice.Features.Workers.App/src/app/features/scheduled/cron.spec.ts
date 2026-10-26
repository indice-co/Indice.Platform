import { clampProgress, describeCron, loadCronstrue } from './cron';

describe('describeCron', () => {
  it('has nothing to describe without an expression', () => {
    expect(describeCron(null)).toBeNull();
    expect(describeCron('')).toBeNull();
  });

  it('describes a Quartz expression (with seconds) once cronstrue has loaded', async () => {
    await loadCronstrue();

    expect(describeCron('0 0 2 * * ?')).toBe('At 02:00');
    expect(describeCron('0 */5 * * * ?')).toBe('Every 5 minutes');
  });

  it('does not throw on an expression it cannot parse', async () => {
    await loadCronstrue();

    expect(() => describeCron('not a cron')).not.toThrow();
  });
});

describe('clampProgress', () => {
  it('keeps progress within 0–100', () => {
    expect(clampProgress(-5)).toBe(0);
    expect(clampProgress(42.5)).toBe(42.5);
    expect(clampProgress(250)).toBe(100);
  });
});
