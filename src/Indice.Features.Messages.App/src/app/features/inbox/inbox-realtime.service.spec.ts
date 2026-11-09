import { TestBed } from '@angular/core/testing';
import { AuthService } from '@indice/ng-auth';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { settings } from 'src/app/core/models/settings';
import { InboxRealtimeService } from './inbox-realtime.service';

describe('InboxRealtimeService', () => {
  let service: InboxRealtimeService;
  let connection: jasmine.SpyObj<HubConnection>;
  let build: jasmine.Spy;
  let originalHubUrl: string | undefined;

  beforeEach(() => {
    originalHubUrl = settings.inboxHubUrl;
    settings.inboxHubUrl = '/hubs/messaging';
    connection = jasmine.createSpyObj('HubConnection', ['start', 'stop', 'on', 'onclose', 'onreconnecting', 'onreconnected']);
    connection.start.and.resolveTo();
    connection.stop.and.resolveTo();
    build = spyOn(HubConnectionBuilder.prototype, 'build').and.returnValue(connection);
    TestBed.configureTestingModule({ providers: [{ provide: AuthService, useValue: { getCurrentUser: () => null } }] });
    service = TestBed.inject(InboxRealtimeService);
    jasmine.clock().install();
  });

  afterEach(async () => {
    await service.stop();
    jasmine.clock().uninstall();
    settings.inboxHubUrl = originalHubUrl;
  });

  it('invalidates startup while the lazy import is pending', async () => {
    const starting = service.start();
    await service.stop();
    await starting;
    expect(build).not.toHaveBeenCalled();
    expect(service.status()).toBe('disconnected');
  });

  it('stops a pending connection without publishing a stale connected status', async () => {
    let completeStart!: () => void;
    let enteredStart!: () => void;
    const entered = new Promise<void>(resolve => enteredStart = resolve);
    connection.start.and.callFake(() => {
      enteredStart();
      return new Promise<void>(resolve => completeStart = resolve);
    });
    const starting = service.start();
    await entered;
    const stopping = service.stop();
    completeStart();
    await Promise.all([starting, stopping]);
    expect(connection.stop).toHaveBeenCalled();
    expect(service.status()).toBe('disconnected');
  });

  it('retries initial failures and exhausted reconnects, but not intentional stops', async () => {
    spyOn(console, 'warn');
    connection.start.and.rejectWith(new Error('unavailable'));
    await service.start();
    expect(service.status()).toBe('disconnected');
    connection.start.and.resolveTo();
    jasmine.clock().tick(1_000);
    await service.start();
    expect(service.status()).toBe('connected');
    connection.onclose.calls.mostRecent().args[0]();
    jasmine.clock().tick(1_000);
    await service.start();
    expect(connection.start).toHaveBeenCalledTimes(3);
    connection.onclose.calls.mostRecent().args[0]();
    await service.stop();
    jasmine.clock().tick(60_000);
    expect(connection.start).toHaveBeenCalledTimes(3);
  });
});
