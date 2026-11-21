import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ChatFilesService } from '../../../core/services/chat-files.service';
import { ChatFileComponent } from './chat-file.component';
import { FileReference } from './part-contracts';

const FILE: FileReference = {
  id: '3f2504e0-4f89-41d3-9a0c-0305e82c3301',
  fileName: 'risk-evidence-9001234567.html',
  contentType: 'text/html',
  length: 39322,
  sha256: '0123456789abcdef0123456789abcdef',
};

describe('ChatFileComponent', () => {
  let fixture: ComponentFixture<ChatFileComponent>;
  let download: jasmine.Spy<(conversationId: string, fileId: string) => Observable<Blob>>;
  let saved: { download: string; href: string }[];
  let clicks: jasmine.Spy;

  beforeEach(async () => {
    download = jasmine.createSpy('download').and.returnValue(of(new Blob(['<html></html>'], { type: 'text/html' })));
    await TestBed.configureTestingModule({
      imports: [ChatFileComponent],
      providers: [provideZonelessChangeDetection(), { provide: ChatFilesService, useValue: { download } }],
    }).compileComponents();
    fixture = TestBed.createComponent(ChatFileComponent);

    // Capture what the component saves instead of letting the browser start a real download.
    saved = [];
    clicks = spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (this: HTMLAnchorElement) {
      saved.push({ download: this.download, href: this.href });
    });
    spyOn(URL, 'createObjectURL').and.returnValue('blob:test');
    spyOn(URL, 'revokeObjectURL');
  });

  function render(file: FileReference | null, conversationId: string | null = 'chat-1'): HTMLElement {
    fixture.componentRef.setInput('file', file);
    fixture.componentRef.setInput('conversationId', conversationId);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const button = (host: HTMLElement) => host.querySelector('button') as HTMLButtonElement;

  it('shows the file name, its size and the start of its hash', () => {
    const host = render(FILE);
    expect(host.textContent).toContain('risk-evidence-9001234567.html');
    expect(host.textContent).toContain('38.4 KB');
    expect(host.textContent).toContain('SHA-256 0123456789ab…');
  });

  it('renders nothing when the part carried no file', () => {
    expect(render(null).querySelector('button')).toBeNull();
  });

  it('downloads through the authenticated service and saves the file under its own name', () => {
    const host = render(FILE);
    button(host).click();
    expect(download).toHaveBeenCalledOnceWith('chat-1', FILE.id);
    expect(clicks).toHaveBeenCalledTimes(1);
    expect(saved[0].download).toBe('risk-evidence-9001234567.html');
    expect(saved[0].href).toBe('blob:test');
  });

  it('is disabled until the conversation has an id', () => {
    const host = render(FILE, null);
    expect(button(host).disabled).toBe(true);
    button(host).click();
    expect(download).not.toHaveBeenCalled();
  });

  it('says so when the download fails, and can be retried', () => {
    download.and.returnValue(throwError(() => new Error('404')));
    const host = render(FILE);
    button(host).click();
    fixture.detectChanges();
    expect(host.querySelector('[role="alert"]')?.textContent).toContain('could not be downloaded');
    expect(button(host).disabled).toBe(false);
    expect(clicks).not.toHaveBeenCalled();
  });

  it('clears an earlier error when the next attempt starts', () => {
    download.and.returnValues(throwError(() => new Error('500')), of(new Blob(['x'])));
    const host = render(FILE);
    button(host).click();
    fixture.detectChanges();
    expect(host.querySelector('[role="alert"]')).toBeTruthy();
    button(host).click();
    fixture.detectChanges();
    expect(host.querySelector('[role="alert"]')).toBeNull();
    expect(clicks).toHaveBeenCalledTimes(1);
  });

  it('labels the button with the file it downloads, for assistive technology', () => {
    expect(button(render(FILE)).getAttribute('aria-label')).toBe('Download risk-evidence-9001234567.html');
  });
});
