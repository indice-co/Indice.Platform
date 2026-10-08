import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AUTH_SETTINGS, AuthService } from '@indice/ng-auth';
import { of } from 'rxjs';

import { ShellComponent } from './shell.component';
import { ConversationListItem, DexApiService } from '../services/dex-api.service';

const SESSIONS = [
  new ConversationListItem({ id: 'c1', title: 'Vector search', lastActivityAt: new Date() }),
  new ConversationListItem({ id: 'c2', title: 'Onboarding', lastActivityAt: new Date() }),
];

const authStub = {
  user$: of({ expired: false, profile: {} }),
  getAuthorizationHeaderValue: () => 'Bearer user',
  getDisplayName: () => 'Krikor Tzevachirian',
  getEmail: () => 'k@indice.gr',
  getSubjectId: () => 'sub-1',
  signinRedirect: () => undefined,
  signoutRedirect: () => undefined,
} as unknown as AuthService;

const dexStub = {
  list: () => of({ count: SESSIONS.length, items: SESSIONS }),
  delete: () => of(void 0),
} as unknown as DexApiService;

describe('ShellComponent', () => {
  let fixture: ComponentFixture<ShellComponent>;

  beforeEach(async () => {
    localStorage.removeItem('dex.rail.collapsed');
    sessionStorage.removeItem('dex.guest.session');
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: authStub },
        { provide: AUTH_SETTINGS, useValue: { authority: 'https://my.indice.gr' } },
        { provide: DexApiService, useValue: dexStub },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ShellComponent);
    await fixture.whenStable();
  });

  it('frames the routed outlet with the rail, replacing the old top nav', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('app-sidebar').length).withContext('desktop + drawer').toBe(2);
    expect(el.querySelector('router-outlet')).toBeTruthy();
    expect(el.innerHTML).withContext('no top-bar nav links').not.toContain('routerlinkactive');
  });

  it('loads the conversation list on startup', () => {
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Vector search');
  });

  it('shows no guest CTA and offers the profile route to a signed-in user', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain("You're chatting as a guest");
    expect(el.querySelectorAll('a[href="/profile"]').length).withContext('both rails').toBe(2);
    expect(el.textContent).toContain('Krikor Tzevachirian');
  });

  it('collapse toggle switches the rail width and remembers it', async () => {
    const el = fixture.nativeElement as HTMLElement;
    const rail = el.querySelector('.md\\:block')!;
    expect(rail.classList.contains('w-72')).withContext('starts expanded').toBe(true);

    el.querySelector<HTMLButtonElement>('button[aria-label="Collapse sidebar"]')!.click();
    await fixture.whenStable();

    expect(rail.classList.contains('w-16')).withContext('now the icon rail').toBe(true);
    expect(rail.classList.contains('w-72')).toBe(false);
    expect(localStorage.getItem('dex.rail.collapsed')).toBe('true');
  });

  it('burger opens the mobile drawer and the backdrop closes it', async () => {
    const el = fixture.nativeElement as HTMLElement;
    const drawer = el.querySelector('[role="dialog"]')!;
    expect(drawer.classList.contains('-translate-x-full')).withContext('off-canvas').toBe(true);
    expect(drawer.hasAttribute('inert')).withContext('unfocusable while closed').toBe(true);

    el.querySelector<HTMLButtonElement>('button[aria-label="Open conversations"]')!.click();
    await fixture.whenStable();
    expect(drawer.classList.contains('-translate-x-full')).withContext('slid in').toBe(false);
    expect(drawer.hasAttribute('inert')).toBe(false);

    el.querySelector<HTMLElement>('.bg-black\\/40')!.click();
    await fixture.whenStable();
    expect(drawer.classList.contains('-translate-x-full')).withContext('backdrop closed it').toBe(true);
  });

  it('claims aria-modal only while open, and never uses aria-hidden', async () => {
    const el = fixture.nativeElement as HTMLElement;
    const drawer = el.querySelector('[role="dialog"]')!;
    expect(drawer.getAttribute('aria-modal')).withContext('no claim while closed').toBeNull();

    el.querySelector<HTMLButtonElement>('button[aria-label="Open conversations"]')!.click();
    await fixture.whenStable();
    expect(drawer.getAttribute('aria-modal')).toBe('true');
    expect(drawer.hasAttribute('aria-hidden')).withContext('inert covers this').toBe(false);
  });

  it('makes the page behind the drawer inert, so the modal claim is true', async () => {
    const el = fixture.nativeElement as HTMLElement;
    const rail = el.querySelector('.md\\:block')!;
    // The column holding the mobile bar and the routed outlet.
    const main = el.querySelector('header')!.parentElement!;
    expect(rail.hasAttribute('inert')).withContext('reachable while closed').toBe(false);
    expect(main.hasAttribute('inert')).toBe(false);

    el.querySelector<HTMLButtonElement>('button[aria-label="Open conversations"]')!.click();
    await fixture.whenStable();

    expect(rail.hasAttribute('inert')).withContext('background out of the tab order').toBe(true);
    expect(main.hasAttribute('inert')).toBe(true);
    expect(el.querySelector('.bg-black\\/40')!.hasAttribute('inert'))
      .withContext('backdrop stays clickable')
      .toBe(false);
  });

  it('moves focus into the drawer on open and back to the burger on close', async () => {
    const el = fixture.nativeElement as HTMLElement;
    const burger = el.querySelector<HTMLButtonElement>('button[aria-label="Open conversations"]')!;
    const drawer = el.querySelector<HTMLElement>('[role="dialog"]')!;
    expect(document.activeElement).withContext('no focus grab on first render').not.toBe(burger);

    burger.click();
    await fixture.whenStable();
    expect(drawer.contains(document.activeElement)).withContext('focus entered drawer').toBe(true);

    el.querySelector<HTMLElement>('.bg-black\\/40')!.click();
    await fixture.whenStable();
    expect(document.activeElement).withContext('focus returned to the trigger').toBe(burger);
  });

  it('picking a conversation in the drawer closes it', async () => {
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector<HTMLButtonElement>('button[aria-label="Open conversations"]')!.click();
    await fixture.whenStable();

    const drawer = el.querySelector('[role="dialog"]')!;
    drawer.querySelectorAll<HTMLElement>('nav ul > li > div')[1].click();
    await fixture.whenStable();

    expect(drawer.classList.contains('-translate-x-full')).toBe(true);
  });

  it('deleting the open conversation drops it from the list', async () => {
    const el = fixture.nativeElement as HTMLElement;
    const rail = el.querySelector('.md\\:block')!;
    rail.querySelectorAll<HTMLElement>('nav ul > li > div')[0].click();
    await fixture.whenStable();

    rail.querySelector<HTMLButtonElement>('button[aria-label="Delete conversation"]')!.click();
    await fixture.whenStable();
    const dialog = rail.querySelector<HTMLDialogElement>('dialog')!;
    Array.from(dialog.querySelectorAll('button'))
      .find((b) => b.textContent?.trim() === 'Delete')!
      .click();
    await fixture.whenStable();

    expect(rail.querySelector('nav')!.textContent).not.toContain('Vector search');
    expect(rail.querySelector('nav')!.textContent).toContain('Onboarding');
  });
});

describe('ShellComponent (guest)', () => {
  let fixture: ComponentFixture<ShellComponent>;
  let signinRedirect: jasmine.Spy;
  let list: jasmine.Spy;

  beforeEach(async () => {
    localStorage.removeItem('dex.rail.collapsed');
    sessionStorage.removeItem('dex.guest.session');
    signinRedirect = jasmine.createSpy('signinRedirect');
    list = jasmine.createSpy('list').and.returnValue(of({ count: 0, items: [] }));
    const guestAuth = {
      user$: of(null),
      getAuthorizationHeaderValue: () => '',
      getDisplayName: () => '',
      getEmail: () => undefined,
      getSubjectId: () => undefined,
      signinRedirect,
      signoutRedirect: () => undefined,
    } as unknown as AuthService;
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: guestAuth },
        { provide: AUTH_SETTINGS, useValue: { authority: 'https://my.indice.gr' } },
        { provide: DexApiService, useValue: { list, delete: () => of(void 0) } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ShellComponent);
    await fixture.whenStable();
  });

  it('does not fetch the conversation list without a credential', () => {
    expect(list).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No conversations yet.');
  });

  it('shows the guest CTA bar atop the main column and the Log in button starts sign-in', () => {
    const el = fixture.nativeElement as HTMLElement;
    const main = el.querySelector('header')!.parentElement!;
    const strip = main.querySelector('header')!.nextElementSibling!;
    expect(strip.textContent).toContain("You're chatting as a guest.");
    expect(strip.nextElementSibling?.tagName).withContext('sits right above the page').toBe('MAIN');

    const buttons = Array.from(strip.querySelectorAll('button'));
    expect(buttons.some((b) => b.textContent?.trim() === 'Sign up'))
      .withContext('sign up removed')
      .toBeFalse();

    buttons.find((b) => b.textContent?.trim() === 'Log in')!.click();
    expect(signinRedirect).toHaveBeenCalledWith(jasmine.objectContaining({ location: '/' }));
  });

  it('renders a "G" guest account with Log in instead of Profile / Sign out', () => {
    const el = fixture.nativeElement as HTMLElement;
    const account = el.querySelector('.md\\:block app-sidebar-account')!;
    expect(account.querySelector('img[userpicture], img')).withContext('no picture lookup').toBeNull();
    expect(account.querySelector('[aria-hidden="true"]')!.textContent!.trim()).toBe('G');
    expect(account.textContent).toContain('Guest');
    expect(account.textContent).toContain('Not signed in');
    expect(account.querySelector('a[href="/profile"]')).toBeNull();
    expect(account.textContent).not.toContain('Sign out');
    expect(account.textContent).toContain('Log in');
    expect(account.textContent).not.toContain('Sign up');
  });
});
