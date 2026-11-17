import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
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
  public attach(message: InboxMessage): void { this.openAttachment(message); }
  public readonly downloading = this.attachmentLoading;
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

describe('InboxComponent attachments', () => {
  const apiUrl = `${window.location.origin}/api/messages/attachments/file`;
  let component: TestInboxComponent;
  let api: jasmine.SpyObj<InboxApiService>;
  let toaster: jasmine.SpyObj<AppTranslatedToaster>;
  let open: jasmine.Spy;

  const message = (attachmentUrl: string): InboxMessage => ({ id: 'a', createdAt: '', attachmentUrl });

  beforeEach(() => {
    api = jasmine.createSpyObj<InboxApiService>('InboxApiService', ['getMessageTypes', 'resolveAttachmentUrl', 'getAttachment']);
    api.getMessageTypes.and.returnValue(of([]));
    api.resolveAttachmentUrl.and.callFake(m => m.attachmentUrl ?? undefined);
    toaster = jasmine.createSpyObj<AppTranslatedToaster>('AppTranslatedToaster', ['show']);
    TestBed.configureTestingModule({
      providers: [
        { provide: InboxStore, useValue: { changes$: new Subject() } },
        { provide: InboxApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { queryParamMap: new BehaviorSubject(convertToParamMap({})) } },
        { provide: Router, useValue: {} },
        { provide: DialogService, useValue: {} },
        { provide: AppTranslatedToaster, useValue: toaster }
      ]
    });
    component = TestBed.runInInjectionContext(() => new TestInboxComponent());
    open = spyOn(window, 'open');
  });

  it('downloads documents through the authenticated api instead of navigating to them', () => {
    const clicked: HTMLAnchorElement[] = [];
    spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (this: HTMLAnchorElement) { clicked.push(this); });
    api.getAttachment.and.returnValue(of(new Blob(['%PDF'], { type: 'application/pdf' })));
    component.attach(message(`${apiUrl}.pdf`));
    expect(api.getAttachment).toHaveBeenCalledOnceWith(`${apiUrl}.pdf`);
    expect(open).not.toHaveBeenCalled();
    expect(clicked.length).toBe(1);
    expect(clicked[0].href).toMatch(/^blob:/u);
    expect(clicked[0].download).toBe('file.pdf');
    expect(component.downloading()).toBeFalse();
  });

  it('downloads images the same way, without opening them in the browser', () => {
    const clicked: HTMLAnchorElement[] = [];
    spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (this: HTMLAnchorElement) { clicked.push(this); });
    const response = new Subject<Blob>();
    api.getAttachment.and.returnValue(response);
    component.attach(message(`${apiUrl}.png`));
    expect(component.downloading()).toBeTrue();
    response.next(new Blob(['png'], { type: 'image/png' }));
    response.complete();
    expect(open).not.toHaveBeenCalled();
    expect(clicked.length).toBe(1);
    expect(clicked[0].download).toBe('file.png');
    expect(component.downloading()).toBeFalse();
  });

  it('reports failed downloads', () => {
    api.getAttachment.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    component.attach(message(`${apiUrl}.png`));
    expect(open).not.toHaveBeenCalled();
    expect(toaster.show).toHaveBeenCalledTimes(1);
    expect(component.downloading()).toBeFalse();
  });

  it('leaves unauthorized downloads to the sign in flow', () => {
    api.getAttachment.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    component.attach(message(`${apiUrl}.pdf`));
    expect(toaster.show).not.toHaveBeenCalled();
  });

  it('does not open attachments it refuses to download', () => {
    api.getAttachment.and.returnValue(throwError(() => new Error('Only attachments of the messaging api can be downloaded.')));
    component.attach(message('https://files.example/file.pdf'));
    expect(open).not.toHaveBeenCalled();
    expect(toaster.show).toHaveBeenCalledTimes(1);
  });
});
