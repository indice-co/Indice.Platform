import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { PermissionsStore } from '../../core/services/permissions.store';
import { ScheduledTaskDetails, WorkersApiService } from '../../core/services/workers-api.service';
import { ScheduledTaskDetailPageComponent } from './scheduled-task-detail-page.component';

describe('ScheduledTaskDetailPageComponent last-run outcome', () => {
  for (const lastRunFailed of [false, true]) {
    it(`shows Running instead of ${lastRunFailed ? 'Failed' : 'Succeeded'} during execution`, async () => {
      const task: ScheduledTaskDetails = {
        id: 'reports.nightly',
        name: 'nightly',
        status: 'Running',
        enabled: true,
        hasRun: true,
        isRegistered: true,
        executionCount: 1,
        progress: 0,
        lastExecution: '2026-10-09T10:00:00Z',
        lastRunFailed,
      };
      await TestBed.configureTestingModule({
        imports: [ScheduledTaskDetailPageComponent],
        providers: [
          provideZonelessChangeDetection(),
          provideRouter([]),
          { provide: WorkersApiService, useValue: { getScheduledTask: () => of(task) } },
          {
            provide: LiveRefreshService,
            useValue: { tick$: new BehaviorSubject<void>(undefined), markUpdated: () => undefined },
          },
          { provide: PermissionsStore, useValue: { canManage: signal(false) } },
        ],
      }).compileComponents();
      const fixture = TestBed.createComponent(ScheduledTaskDetailPageComponent);
      fixture.componentRef.setInput('id', task.id);
      await fixture.whenStable();
      const el = fixture.nativeElement as HTMLElement;
      const icon = el.querySelector('svg[aria-label="Running"]');

      expect(icon).not.toBeNull();
      expect(icon?.parentElement?.getAttribute('title')).toContain('Running since');
      expect(el.querySelector('svg[aria-label="Succeeded"], svg[aria-label="Failed"]')).toBeNull();
    });
  }
});
