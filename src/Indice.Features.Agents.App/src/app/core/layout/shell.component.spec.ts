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
  getDisplayName: () => 'Krikor Tzevachirian',
  getEmail: () => 'k@indice.gr',
  getSubjectId: () => 'sub-1',
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
