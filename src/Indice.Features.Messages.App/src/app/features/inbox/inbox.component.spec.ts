import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { FilterClause } from 'src/app/ui/list/filter-clause';
import { InboxApiService } from './inbox-api.service';
import { InboxComponent } from './inbox.component';
import { InboxMessage } from './inbox.models';
import { InboxChange, InboxStore } from './inbox.store';

class TestInboxComponent extends InboxComponent {
  public readonly state = this.list;
  public readonly selection = this.selected;
  public read(message: InboxMessage): void { this.markAsRead(message); }
  public unread(message: InboxMessage): void { this.markAsUnread(message); }
}

describe('InboxComponent read-state reconciliation', () => {
  let component: TestInboxComponent;
  let changes: Subject<InboxChange>;
  let store: jasmine.SpyObj<InboxStore>;
  let refresh: jasmine.Spy;

  beforeEach(() => {
    changes = new Subject();
    store = jasmine.createSpyObj('InboxStore', ['markAsRead', 'markAsUnread'], { changes$: changes });
    TestBed.configureTestingModule({
      providers: [
        { provide: InboxStore, useValue: store },
        { provide: InboxApiService, useValue: { getMessageTypes: () => of([]) } },
        { provide: ActivatedRoute, useValue: { queryParamMap: new BehaviorSubject(convertToParamMap({})) } },
        { provide: Router, useValue: {} },
        { provide: DialogService, useValue: {} },
        { provide: AppTranslatedToaster, useValue: {} }
      ]
    });
    component = TestBed.runInInjectionContext(() => new TestInboxComponent());
    refresh = spyOn(component.state, 'refresh');
  });

  it('reloads read-status filtered lists after individual changes', () => {
    component.state.filters.set([new FilterClause('status', 'unread', 'eq', 'string')]);
    changes.next({ kind: 'read', messageId: 'a' });
    expect(refresh).toHaveBeenCalledTimes(1);
    component.state.filters.set([new FilterClause('status', 'read', 'eq', 'string')]);
    changes.next({ kind: 'unread', messageId: 'a' });
    expect(refresh).toHaveBeenCalledTimes(2);
    component.state.filters.set([]);
    changes.next({ kind: 'read', messageId: 'a' });
    expect(refresh).toHaveBeenCalledTimes(2);
  });

  it('clears unread membership and count immediately, then reconciles filtered lists', () => {
    component.state.filters.set([new FilterClause('status', 'unread', 'eq', 'string')]);
    component.state.items.set([{ id: 'a', createdAt: '', isRead: false }]);
    component.state.count.set(1);
    changes.next({ kind: 'all-read' });
    expect(component.state.items()).toEqual([]);
    expect(component.state.count()).toBe(0);
    expect(refresh).not.toHaveBeenCalled();
    changes.next({ kind: 'reconcile' });
    expect(refresh).toHaveBeenCalledTimes(1);
    component.state.filters.set([new FilterClause('status', 'read', 'eq', 'string')]);
    changes.next({ kind: 'reconcile' });
    expect(refresh).toHaveBeenCalledTimes(2);
  });

  for (const action of ['read', 'unread'] as const) {
    it(`does not apply a stale ${action} completion to another selected message`, () => {
      const completion = new Subject<void>();
      store.markAsRead.and.returnValue(completion);
      store.markAsUnread.and.returnValue(completion);
      const first = { id: 'a', createdAt: '', isRead: action === 'unread' };
      const second = { id: 'b', createdAt: '', isRead: action === 'unread' };
      component.selection.set(first);
      component[action](first);
      component.selection.set(second);
      completion.next();
      expect(component.selection()).toBe(second);
    });
  }
});
