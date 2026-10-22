import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { LiveRefreshService } from '../../core/services/live-refresh.service';
import {
  QueueInfo,
  QueueMessageListItem,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { QueueDetailPageComponent } from './queue-detail-page.component';

const IDS = [
  '11111111-0000-0000-0000-000000000000',
  '22222222-0000-0000-0000-000000000000',
  '33333333-0000-0000-0000-000000000000',
];

const MESSAGES: QueueMessageListItem[] = IDS.map((id) => ({
  id,
  queueName: 'orders',
  status: 'Enqueued',
  date: new Date().toISOString(),
  dequeueCount: 0,
  payloadPreview: '{"orderId":1}',
}));

const QUEUE: QueueInfo = {
  name: 'orders',
  enqueued: 45,
  scheduled: 0,
  retrying: 0,
  poison: 2,
  dequeued: 0,
  total: 47,
  isRegistered: false,
};

/** A poll clock that ticks once per subscriber, so each stream loads exactly once. */
function liveStub() {
  const tick$ = new BehaviorSubject<void>(undefined);
  return {
    tick$,
    enabled: signal(true),
    intervalMs: signal(5000),
    lastUpdated: signal<Date | null>(null),
    markUpdated: () => undefined,
    refreshNow: () => tick$.next(),
  };
}

describe('QueueDetailPageComponent', () => {
  let fixture: ComponentFixture<QueueDetailPageComponent>;
  let api: jasmine.SpyObj<WorkersApiService>;
  let el: HTMLElement;

  beforeEach(async () => {
    api = jasmine.createSpyObj<WorkersApiService>('WorkersApiService', [
      'getQueueMessages',
      'getQueues',
      'requeueMessages',
      'deleteMessages',
    ]);
    api.getQueueMessages.and.returnValue(of({ count: 45, items: MESSAGES }));
    api.getQueues.and.returnValue(of([QUEUE]));
    api.deleteMessages.and.returnValue(of({ affected: 2 }));
    api.requeueMessages.and.returnValue(of({ affected: 1 }));

    await TestBed.configureTestingModule({
      imports: [QueueDetailPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: WorkersApiService, useValue: api },
        { provide: LiveRefreshService, useValue: liveStub() },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(QueueDetailPageComponent);
    fixture.componentRef.setInput('name', 'orders');
    await fixture.whenStable();
    el = fixture.nativeElement as HTMLElement;
  });

  function button(text: string): HTMLButtonElement {
    const match = Array.from(el.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === text,
    );
    expect(match).withContext(`button "${text}"`).toBeTruthy();
    return match!;
  }

  async function select(...indexes: number[]): Promise<void> {
    const boxes = el.querySelectorAll<HTMLInputElement>('tbody input[type=checkbox]');
    indexes.forEach((i) => boxes[i].click());
    await fixture.whenStable();
  }

  it('deletes both selected messages once the confirmation is accepted', async () => {
    await select(0, 1);
    expect(button('Delete selected').disabled).toBeFalse();

    button('Delete selected').click();
    await fixture.whenStable();
    expect(api.deleteMessages).withContext('waits for the confirmation').not.toHaveBeenCalled();

    const confirm = el.querySelector<HTMLButtonElement>(
      'dialog[open] .modal-action button.btn-error',
    );
    expect(confirm).withContext('the delete confirmation is open').toBeTruthy();
    confirm!.click();
    await fixture.whenStable();

    expect(api.deleteMessages).toHaveBeenCalledOnceWith([IDS[0], IDS[1]]);
  });

  it('requeues a single message without asking first', async () => {
    await select(2);

    button('Requeue selected').click();
    await fixture.whenStable();

    expect(el.querySelector('dialog[open]')).toBeNull();
    expect(api.requeueMessages).toHaveBeenCalledOnceWith([IDS[2]]);
  });

  it('goes back to page 1 when the status changes', async () => {
    el.querySelector<HTMLButtonElement>('button[aria-label="Next page"]')!.click();
    await fixture.whenStable();
    expect(api.getQueueMessages.calls.mostRecent().args).toEqual([
      'orders',
      jasmine.objectContaining({ status: 'Enqueued', page: 2 }),
    ]);

    fixture.componentRef.setInput('status', 'Poison');
    await fixture.whenStable();

    expect(api.getQueueMessages.calls.mostRecent().args).toEqual([
      'orders',
      jasmine.objectContaining({ status: 'Poison', page: 1 }),
    ]);
  });

  it('returns to the last valid page when the current page becomes empty', async () => {
    api.getQueueMessages.and.returnValues(
      of({ count: 10, items: [] }),
      of({ count: 10, items: MESSAGES }),
    );

    el.querySelector<HTMLButtonElement>('button[aria-label="Next page"]')!.click();
    await fixture.whenStable();

    expect(api.getQueueMessages.calls.mostRecent().args).toEqual([
      'orders',
      jasmine.objectContaining({ status: 'Enqueued', page: 1 }),
    ]);
  });

  it('clears the selection when the status changes', async () => {
    await select(0);
    expect(el.textContent).toContain('1 selected');

    fixture.componentRef.setInput('status', 'Poison');
    await fixture.whenStable();

    expect(el.textContent).not.toContain('1 selected');
    expect(button('Delete selected').disabled).toBeTrue();
  });
});
