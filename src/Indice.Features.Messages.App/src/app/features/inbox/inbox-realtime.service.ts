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
 * Connects to the configured `inboxHubUrl` with the user's access token. Whatever serves that url (i.e. the SignalR proxy that
 * redirects to Azure SignalR) is transparent to the client. Disabled when no `inboxHubUrl` is configured.
 */
@Injectable({ providedIn: 'root' })
export class InboxRealtimeService {
  private readonly _auth = inject(AuthService);
  private readonly _messageCreated$ = new Subject<InboxMessageNotification>();
  private _connection?: HubConnection;
  private _starting?: Promise<void>;
  private _generation = 0;
  private _retryTimer?: ReturnType<typeof setTimeout>;
  private _retryDelay = 1_000;

  public readonly enabled = !!settings.inboxHubUrl;
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
    if (this._starting) {
      return this._starting;
    }
    if (this._connection || this._retryTimer) {
      return Promise.resolve();
    }
    if (!this._starting) {
      const starting = this.connect(this._generation).finally(() => {
        if (this._starting === starting) {
          this._starting = undefined;
        }
      });
      this._starting = starting;
    }
    return this._starting;
  }

  public async stop(): Promise<void> {
    ++this._generation;
    clearTimeout(this._retryTimer);
    this._retryTimer = undefined;
    this._retryDelay = 1_000;
    const starting = this._starting;
    this._starting = undefined;
    const connection = this._connection;
    this._connection = undefined;
    if (this.enabled) {
      this.status.set('disconnected');
    }
    await connection?.stop().catch(() => undefined);
    await starting;
  }

  private async connect(generation: number): Promise<void> {
    let connection: HubConnection | undefined;
    this.status.set('connecting');
    try {
      const signalR = await import('@microsoft/signalr');
      if (generation !== this._generation) {
        return;
      }
      connection = new signalR.HubConnectionBuilder()
        .withUrl(settings.inboxHubUrl!, {
          accessTokenFactory: () => this._auth.getCurrentUser()?.access_token ?? ''
        })
        .withAutomaticReconnect()
        .configureLogging(settings.production ? signalR.LogLevel.Warning : signalR.LogLevel.Information)
        .build();
      const isCurrent = () => generation === this._generation && this._connection === connection;
      connection.on(INBOX_MESSAGE_CREATED_METHOD, (notification: InboxMessageNotification) => {
        if (isCurrent()) {
          this._messageCreated$.next(notification);
        }
      });
      connection.onreconnecting(() => {
        if (isCurrent()) {
          this.status.set('connecting');
        }
      });
      connection.onreconnected(() => {
        if (isCurrent()) {
          this.status.set('connected');
        }
      });
      connection.onclose(() => {
        if (isCurrent()) {
          this._connection = undefined;
          this.status.set('disconnected');
          this.scheduleRetry(generation);
        }
      });
      this._connection = connection;
      await connection.start();
      if (!isCurrent()) {
        await connection.stop().catch(() => undefined);
        return;
      }
      this._retryDelay = 1_000;
      this.status.set('connected');
    } catch (error) {
      if (generation === this._generation) {
        console.warn('Inbox real time notifications are not available.', error);
        this._connection = undefined;
        this.status.set('disconnected');
      }
      await connection?.stop().catch(() => undefined);
      this.scheduleRetry(generation);
    }
  }

  private scheduleRetry(generation: number): void {
    if (generation !== this._generation || this._retryTimer) {
      return;
    }
    this._retryTimer = setTimeout(() => {
      this._retryTimer = undefined;
      void this.start();
    }, this._retryDelay);
    this._retryDelay = Math.min(this._retryDelay * 2, 60_000);
  }
}
