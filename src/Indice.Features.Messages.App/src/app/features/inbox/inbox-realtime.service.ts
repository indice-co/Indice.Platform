import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import type { HubConnection } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { settings } from 'src/app/core/models/settings';
import { InboxMessageNotification } from './inbox.models';

/** The client method the server invokes for every new inbox message (see `MessageInboxNotificationOptions.MethodName`). */
export const INBOX_MESSAGE_CREATED_METHOD = 'inboxMessageCreated';

export type InboxRealtimeStatus = 'disabled' | 'disconnected' | 'connecting' | 'connected';

/**
 * Receives personalized real time inbox notifications through SignalR.
 *
 * Connects to the SignalR proxy endpoints (`MapSignalRProxy()`) of the host: the client negotiates at
 * `{signalRProxyUrl}/hubs/{inboxHubName}/negotiate` with the user's access token and is then redirected to the actual
 * (Azure) SignalR service. Disabled when no `inboxHubName` is configured.
 */
@Injectable({ providedIn: 'root' })
export class InboxRealtimeService {
  private readonly _auth = inject(AuthService);
  private readonly _messageCreated$ = new Subject<InboxMessageNotification>();
  private _connection?: HubConnection;
  private _starting?: Promise<void>;

  public readonly enabled = !!settings.inboxHubName;
  public readonly status = signal<InboxRealtimeStatus>(this.enabled ? 'disconnected' : 'disabled');
  /** Emits every time the server pushes a new inbox message notification. */
  public readonly messageCreated$: Observable<InboxMessageNotification> = this._messageCreated$.asObservable();

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stop());
  }

  /** Starts the connection (if enabled). Safe to call multiple times. */
  public start(): Promise<void> {
    if (!this.enabled) {
      return Promise.resolve();
    }
    this._starting ??= this.connect().finally(() => this._starting = undefined);
    return this._starting;
  }

  public async stop(): Promise<void> {
    const connection = this._connection;
    this._connection = undefined;
    await connection?.stop().catch(() => undefined);
    if (this.enabled) {
      this.status.set('disconnected');
    }
  }

  private async connect(): Promise<void> {
    if (this._connection) {
      return;
    }
    this.status.set('connecting');
    try {
      const signalR = await import('@microsoft/signalr');
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${settings.signalRProxyUrl}/hubs/${encodeURIComponent(settings.inboxHubName!)}`, {
          accessTokenFactory: () => this._auth.getCurrentUser()?.access_token ?? ''
        })
        .withAutomaticReconnect()
        .configureLogging(settings.production ? signalR.LogLevel.Warning : signalR.LogLevel.Information)
        .build();
      connection.on(INBOX_MESSAGE_CREATED_METHOD, (notification: InboxMessageNotification) => this._messageCreated$.next(notification));
      connection.onreconnecting(() => this.status.set('connecting'));
      connection.onreconnected(() => this.status.set('connected'));
      connection.onclose(() => this.status.set('disconnected'));
      this._connection = connection;
      await connection.start();
      this.status.set('connected');
    } catch (error) {
      console.warn('Inbox real time notifications are not available.', error);
      this._connection = undefined;
      this.status.set('disconnected');
    }
  }
}
