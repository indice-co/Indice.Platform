import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of } from 'rxjs';

import { ConversationsStore } from './conversations.store';
import { ConversationListItem, ConversationListItemResultSet, DexApiService } from './dex-api.service';

function listOf(...ids: string[]): ConversationListItemResultSet {
  return new ConversationListItemResultSet({
    count: ids.length,
    items: ids.map((id) => new ConversationListItem({ id, title: id.toUpperCase() })),
  });
}

/** Hands out a fresh Subject per `list()` call so response ordering can be driven by the test. */
class ListController {
  readonly pending: Subject<ConversationListItemResultSet>[] = [];

  readonly api = {
    list: () => {
      const subject = new Subject<ConversationListItemResultSet>();
      this.pending.push(subject);
      return subject.asObservable();
    },
    delete: () => of(void 0) as Observable<void>,
  } as unknown as DexApiService;

  emit(index: number, result: ConversationListItemResultSet): void {
    this.pending[index].next(result);
    this.pending[index].complete();
  }
}

describe('ConversationsStore', () => {
  let controller: ListController;
  let store: ConversationsStore;

  beforeEach(() => {
    controller = new ListController();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: DexApiService, useValue: controller.api },
      ],
    });
    store = TestBed.inject(ConversationsStore);
  });

  it('populates the list and clears loading on a plain refresh', () => {
    store.refresh();
    expect(store.loading()).toBe(true);

    controller.emit(0, listOf('c1', 'c2'));

    expect(store.sessions().map((s) => s.id)).toEqual(['c1', 'c2']);
    expect(store.loading()).toBe(false);
  });

  it('does not resurrect a deleted conversation when an older fetch lands after the delete', () => {
    store.refresh();
    store.select('c1');

    // Deletion happens while the list request is still on the wire.
    store.remove('c1');
    expect(store.sessions().map((s) => s.id)).withContext('optimistically dropped').toEqual([]);

    // The response was issued before the delete and still contains c1.
    controller.emit(0, listOf('c1', 'c2'));

    expect(store.sessions().map((s) => s.id)).withContext('no ghost row').toEqual([]);
    expect(store.loading()).withContext('not stranded on skeletons').toBe(false);
    expect(store.activeId()).withContext('open conversation cleared').toBeNull();
  });

  it('ignores a superseded fetch when two refreshes overlap', () => {
    store.refresh();
    store.refresh();
    expect(controller.pending.length).toBe(2);

    controller.emit(1, listOf('fresh'));
    controller.emit(0, listOf('stale'));

    expect(store.sessions().map((s) => s.id)).toEqual(['fresh']);
    expect(store.loading()).toBe(false);
  });

  it('restores the row when the server refuses the delete', () => {
    const failing = {
      list: () => of(listOf('c1')),
      delete: () => new Observable<void>((s) => s.error(new Error('nope'))),
    } as unknown as DexApiService;
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), { provide: DexApiService, useValue: failing }],
    });
    const s = TestBed.inject(ConversationsStore);

    s.refresh();
    expect(s.sessions().map((x) => x.id)).toEqual(['c1']);

    s.remove('c1');

    expect(s.sessions().map((x) => x.id)).withContext('rolled back').toEqual(['c1']);
    expect(s.error()).toBe('Could not delete the conversation.');
  });
});
