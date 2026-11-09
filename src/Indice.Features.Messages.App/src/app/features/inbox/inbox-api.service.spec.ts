import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InboxApiService } from './inbox-api.service';

describe('InboxApiService', () => {
  let api: InboxApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(InboxApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests personalized Inbox content', () => {
    api.getMessage('message-1').subscribe();
    const request = http.expectOne('/my/messages/message-1?channel=Inbox');
    expect(request.request.method).toBe('GET');
    request.flush({ id: 'message-1' });
  });

  it('includes expired messages when marking all as read', () => {
    api.markAllAsRead().subscribe();
    const request = http.expectOne('/my/messages/all/read');
    expect(request.request.body).toEqual({ showExpired: true });
    request.flush(null);
  });
});
