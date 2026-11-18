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

  it('downloads attachments through the http client as a blob', () => {
    const url = api.resolveAttachmentUrl({ id: 'message-1', createdAt: '', attachmentUrl: '/api/messages/attachments/file.pdf' })!;
    let received: Blob | undefined;
    api.getAttachment(url).subscribe(blob => received = blob);
    const request = http.expectOne(`${window.location.origin}/api/messages/attachments/file.pdf`);
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    const body = new Blob(['%PDF'], { type: 'application/pdf' });
    request.flush(body);
    expect(received).toBe(body);
  });

  it('refuses to send the access token to attachments of other hosts', () => {
    let failed = false;
    api.getAttachment('https://attacker.example/api/messages/attachments/file.pdf').subscribe({ error: () => failed = true });
    expect(failed).toBeTrue();
    expect(api.isApiUrl('https://attacker.example/file.pdf')).toBeFalse();
    expect(api.isApiUrl(`${window.location.origin}/api/messages/attachments/file.pdf`)).toBeTrue();
  });
});
