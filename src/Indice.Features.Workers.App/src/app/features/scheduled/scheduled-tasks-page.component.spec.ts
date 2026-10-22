import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';

import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { ScheduledTaskInfo, WorkersApiService } from '../../core/services/workers-api.service';
import { ScheduledTasksPageComponent } from './scheduled-tasks-page.component';

function task(overrides: Partial<ScheduledTaskInfo> = {}): ScheduledTaskInfo {
  return {
    id: 'InternalJobsGroup.cleanup',
    name: 'cleanup',
    group: 'InternalJobsGroup',
    description: 'Removes expired rows',
    jobType: 'MyApp.CleanupHandler',
    cronExpression: null,
    status: 'Idle',
    enabled: true,
    hasRun: true,
    isRegistered: false,
    lastExecution: '2026-10-09T10:00:00Z',
    nextExecution: '2026-10-09T11:00:00Z',
    executionCount: 3,
    lastErrorDate: null,
    lastRunFailed: false,
    progress: 0,
    workerId: 'WORKER-1',
    ...overrides,
  };
}

describe('ScheduledTasksPageComponent', () => {
  let fixture: ComponentFixture<ScheduledTasksPageComponent>;
  let api: jasmine.SpyObj<WorkersApiService>;
  let live: { tick$: Subject<void>; refreshNow: jasmine.Spy; markUpdated: jasmine.Spy };

  /** Renders the page with one poll's worth of tasks. */
  async function mount(tasks: ScheduledTaskInfo[]): Promise<HTMLElement> {
    api.getScheduledTasks.and.returnValue(of(tasks));
    fixture = TestBed.createComponent(ScheduledTasksPageComponent);
    live.tick$.next();
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function toggle(el: HTMLElement): HTMLInputElement {
    return el.querySelector<HTMLInputElement>('input.toggle')!;
  }

  beforeEach(async () => {
    api = jasmine.createSpyObj<WorkersApiService>('WorkersApiService', [
      'getScheduledTasks',
      'setScheduledTaskEnabled',
    ]);
    // A manual clock: the page loads only when the test ticks, so no poll races the assertions.
    live = {
      tick$: new Subject<void>(),
      refreshNow: jasmine.createSpy('refreshNow'),
      markUpdated: jasmine.createSpy('markUpdated'),
    };
    await TestBed.configureTestingModule({
      imports: [ScheduledTasksPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: WorkersApiService, useValue: api },
        { provide: LiveRefreshService, useValue: live },
      ],
    }).compileComponents();
  });

  it('disables a task through the API and refreshes', async () => {
    const response = new Subject<void>();
    api.setScheduledTaskEnabled.and.returnValue(response);
    const el = await mount([task()]);

    toggle(el).click();
    await fixture.whenStable();

    expect(api.setScheduledTaskEnabled).toHaveBeenCalledOnceWith(
      'InternalJobsGroup.cleanup',
      false,
    );
    expect(toggle(el).checked).withContext('flips before the server answers').toBeFalse();
    expect(toggle(el).disabled).withContext('locked while in flight').toBeTrue();

    response.next();
    response.complete();
    await fixture.whenStable();

    expect(toggle(el).checked).toBeFalse();
    expect(toggle(el).disabled).toBeFalse();
    expect(live.refreshNow).toHaveBeenCalled();
  });

  it('reverts the toggle when the call fails', async () => {
    api.setScheduledTaskEnabled.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 500, statusText: 'Server Error' })),
    );
    const el = await mount([task()]);

    toggle(el).click();
    await fixture.whenStable();

    expect(api.setScheduledTaskEnabled).toHaveBeenCalledOnceWith(
      'InternalJobsGroup.cleanup',
      false,
    );
    expect(toggle(el).checked).toBeTrue();
    expect(toggle(el).disabled).toBeFalse();
    expect(live.refreshNow).not.toHaveBeenCalled();
  });

  it('keeps the optimistic value when a poll lands mid-request', async () => {
    const response = new Subject<void>();
    api.setScheduledTaskEnabled.and.returnValue(response);
    const el = await mount([task()]);

    toggle(el).click();
    // A poll that left before the PUT still reports the old value.
    live.tick$.next();
    await fixture.whenStable();

    expect(toggle(el).checked).toBeFalse();
  });

  it('disables the toggle for a task that has never run', async () => {
    const el = await mount([task({ hasRun: false, lastExecution: null })]);

    expect(toggle(el).disabled).toBeTrue();

    toggle(el).click();
    expect(api.setScheduledTaskEnabled).not.toHaveBeenCalled();
  });
});
