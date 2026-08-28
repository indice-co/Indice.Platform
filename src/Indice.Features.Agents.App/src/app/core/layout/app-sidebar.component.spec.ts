import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AUTH_SETTINGS, AuthService } from '@indice/ng-auth';

import { AppSidebarComponent } from './app-sidebar.component';
import { ConversationListItem } from '../services/dex-api.service';

const SESSIONS = [
  new ConversationListItem({ id: 'c1', title: 'Vector search', lastActivityAt: new Date() }),
  new ConversationListItem({ id: 'c2', title: 'Onboarding', lastActivityAt: new Date() }),
  new ConversationListItem({ id: 'c3', title: undefined, lastActivityAt: new Date() }),
];

const authStub = {
  getDisplayName: () => 'Krikor Tzevachirian',
  getEmail: () => 'k@indice.gr',
  getSubjectId: () => 'sub-1',
  signoutRedirect: () => undefined,
} as unknown as AuthService;

describe('AppSidebarComponent', () => {
  let fixture: ComponentFixture<AppSidebarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppSidebarComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: authStub },
        { provide: AUTH_SETTINGS, useValue: { authority: 'https://my.indice.gr' } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AppSidebarComponent);
    fixture.componentRef.setInput('sessions', SESSIONS);
    fixture.componentRef.setInput('activeId', 'c1');
    await fixture.whenStable();
  });

  it('expanded: renders search, every conversation, and the account block last', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('input[type=search]')).withContext('search field').toBeTruthy();
    expect(el.querySelectorAll('nav ul > li').length).toBe(3);
    expect(el.textContent).toContain('Vector search');
    expect(el.textContent).toContain('Untitled conversation');
    expect(el.textContent).toContain('New chat');
    expect(el.querySelector('app-powered-by')).withContext('powered-by').toBeTruthy();
    const rail = el.querySelector('aside')!;
    expect(rail.lastElementChild!.querySelector('app-sidebar-account'))
      .withContext('account anchors the bottom')
      .toBeTruthy();
  });

  it('expanded: delete is visible on touch and hover-revealed from md up', () => {
    const trash = (fixture.nativeElement as HTMLElement).querySelector(
      'button[aria-label="Delete conversation"]',
    )!;
    expect(trash).toBeTruthy();
    expect(trash.className).toContain('md:opacity-0');
    expect(trash.className).toContain('md:group-hover:opacity-100');
    expect(trash.className).withContext('never hidden on touch').not.toMatch(/(^|\s)opacity-0(\s|$)/);
  });

  it('collapsed: swaps to the icon rail — dots with tooltips, no search field', async () => {
    fixture.componentRef.setInput('collapsed', true);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('input[type=search]')).withContext('search hidden').toBeNull();
    const dots = el.querySelectorAll('li.tooltip');
    expect(dots.length).toBe(3);
    expect(dots[0].getAttribute('data-tip')).toBe('Vector search');
    expect(dots[2].getAttribute('data-tip')).toBe('Untitled conversation');
    expect(el.querySelector('button[aria-label="New chat"]')).toBeTruthy();
    expect(el.querySelector('button[aria-label="Expand sidebar"]')).toBeTruthy();
    expect(el.querySelector('app-sidebar-account')).withContext('account stays').toBeTruthy();
  });

  it('delete asks for confirmation before emitting', async () => {
    const removed: string[] = [];
    fixture.componentInstance.removed.subscribe((id: string) => removed.push(id));
    const el = fixture.nativeElement as HTMLElement;

    el.querySelector<HTMLButtonElement>('button[aria-label="Delete conversation"]')!.click();
    await fixture.whenStable();

    const dialog = el.querySelector<HTMLDialogElement>('dialog')!;
    expect(dialog.open).withContext('confirm dialog opened').toBe(true);
    expect(removed).withContext('nothing deleted yet').toEqual([]);
    expect(dialog.textContent).toContain('Vector search');

    Array.from(dialog.querySelectorAll('button'))
      .find((b) => b.textContent?.trim() === 'Delete')!
      .click();
    await fixture.whenStable();

    expect(removed).toEqual(['c1']);
    expect(dialog.open).withContext('dialog closed').toBe(false);
  });

  it('cancel closes the dialog without deleting', async () => {
    const removed: string[] = [];
    fixture.componentInstance.removed.subscribe((id: string) => removed.push(id));
    const el = fixture.nativeElement as HTMLElement;

    el.querySelector<HTMLButtonElement>('button[aria-label="Delete conversation"]')!.click();
    await fixture.whenStable();
    const dialog = el.querySelector<HTMLDialogElement>('dialog')!;
    Array.from(dialog.querySelectorAll('button'))
      .find((b) => b.textContent?.trim() === 'Cancel')!
      .click();
    await fixture.whenStable();

    expect(removed).toEqual([]);
    expect(dialog.open).toBe(false);
  });

  it('account popover opens upward and offers Profile and Sign out', () => {
    const account = (fixture.nativeElement as HTMLElement).querySelector('app-sidebar-account')!;
    expect(account.textContent).toContain('Krikor Tzevachirian');
    expect(account.textContent).toContain('k@indice.gr');
    expect(account.querySelector('a[href="/profile"]')).withContext('profile link').toBeTruthy();
    expect(account.textContent).toContain('Sign out');
    expect(account.querySelector('.dropdown-top')).withContext('opens upward').toBeTruthy();
  });
});
