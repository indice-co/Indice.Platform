import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subscription } from 'rxjs';

import { LiveRefreshService } from './live-refresh.service';

describe('LiveRefreshService', () => {
  let hidden: boolean;
  let subscription: Subscription | undefined;

  beforeEach(() => {
    hidden = false;
    // `document.hidden` is a getter on Document.prototype; shadow it on the instance for the test.
    Object.defineProperty(document, 'hidden', { configurable: true, get: () => hidden });
    localStorage.removeItem('workers.live');
    localStorage.removeItem('workers.interval');
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
  });

  afterEach(() => {
    subscription?.unsubscribe();
    delete (document as { hidden?: boolean }).hidden;
  });

  async function ticksOf(live: LiveRefreshService): Promise<number> {
    let ticks = 0;
    subscription = live.tick$.subscribe(() => ticks++);
    // Let toObservable's effects emit, then the timer(0) fire.
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve, 10));
    return ticks;
  }

  it('loads a page opened after the tab became visible, even when the app started in a hidden tab', async () => {
    hidden = true;
    const live = TestBed.inject(LiveRefreshService);
    hidden = false;
    // Another subscriber (the shell) consumed the visibility change before the page subscribed.
    document.dispatchEvent(new Event('visibilitychange'));

    expect(await ticksOf(live)).toBe(1);
  });

  it('does not load while the tab is hidden', async () => {
    const live = TestBed.inject(LiveRefreshService);
    hidden = true;

    expect(await ticksOf(live)).toBe(0);
  });
});
