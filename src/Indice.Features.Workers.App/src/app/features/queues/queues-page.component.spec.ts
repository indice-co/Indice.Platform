import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { PermissionsStore } from '../../core/services/permissions.store';
import { QueueInfo, WorkersApiService } from '../../core/services/workers-api.service';
import { QueuesPageComponent } from './queues-page.component';

describe('QueuesPageComponent worker column', () => {
  async function mount(overrides: Partial<QueueInfo>): Promise<HTMLElement> {
    const queue: QueueInfo = {
      name: 'orders',
      enqueued: 1,
      scheduled: 0,
      retrying: 0,
      poison: 0,
      dequeued: 0,
      total: 1,
      isRegistered: false,
      hasConfigurationMismatch: false,
      ...overrides,
    };
    await TestBed.configureTestingModule({
      imports: [QueuesPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: WorkersApiService, useValue: { getQueues: () => of([queue]) } },
        {
          provide: LiveRefreshService,
          useValue: { tick$: new BehaviorSubject<void>(undefined), markUpdated: () => undefined },
        },
        { provide: PermissionsStore, useValue: { canManage: signal(false) } },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(QueuesPageComponent);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows No worker when consumer counts are known but configuration is missing', async () => {
    const el = await mount({ consumers: 0, instanceCount: null });

    expect(el.textContent).toContain('Workers');
    expect(el.textContent).toContain('No worker');
  });

  it('hides the column for legacy queues with no registration data', async () => {
    const el = await mount({ consumers: null, instanceCount: null });

    expect(el.textContent).not.toContain('Workers');
    expect(el.textContent).not.toContain('No worker');
  });

  it('shows the column when dequeue configuration is available', async () => {
    const el = await mount({ consumers: null, instanceCount: 2, isRegistered: true });

    expect(el.textContent).toContain('Workers');
  });
});
