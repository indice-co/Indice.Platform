import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

/** Base URL of the workers dashboard API, including its prefix (e.g. `https://host/api/workers`). */
export const WORKERS_API_BASE_URL = new InjectionToken<string>('WORKERS_API_BASE_URL');

export type QueueMessageStatus = 'Enqueued' | 'Scheduled' | 'Retrying' | 'Poison' | 'Dequeued';
export type ScheduledTaskState = 'Idle' | 'Running';

export interface ResultSet<T> {
  count: number;
  items: T[];
}

export interface DashboardStats {
  serverTimeUtc: string;
  queues: number;
  enqueued: number;
  scheduled: number;
  retrying: number;
  poison: number;
  dequeued: number;
  scheduledTasks: number;
  runningTasks: number;
  failingTasks: number;
  disabledTasks: number;
  activeLocks: number;
  servers: number;
  /** Messages the live workers are processing now; `null` when no live worker reports metrics. */
  processing?: number | null;
  /** Messages processed per minute by the live workers, successfully or not. */
  processedPerMinute?: number | null;
  /** Messages that failed per minute (retried or poisoned). */
  failedPerMinute?: number | null;
}

export interface QueueInfo {
  /** Logical queue name — `orders` and `orders-poison` are one row. */
  name: string;
  enqueued: number;
  scheduled: number;
  retrying: number;
  poison: number;
  dequeued: number;
  total: number;
  /** Min visible-at of Enqueued/Retrying messages — the lag indicator. */
  oldestEnqueuedAt?: string | null;
  /** A live worker consumes the queue. The settings below come from the registered workers. */
  isRegistered: boolean;
  /** Live workers that consume the queue; `null` when the server table isn't available. */
  consumers?: number | null;
  /** The live consumers have different settings, e.g. during a rolling deployment. */
  hasConfigurationMismatch: boolean;
  workItemType?: string | null;
  handlerType?: string | null;
  /** Milliseconds. */
  pollingInterval?: number | null;
  /** Dequeue instances across the live workers. */
  instanceCount?: number | null;
  maxRetryCount?: number | null;
  /**
   * Activity measured by the live workers and stored with their heartbeat (about the last minute, up to a heartbeat old).
   * `null` when no live worker reports metrics for the queue.
   */
  processing?: number | null;
  processedPerMinute?: number | null;
  failedPerMinute?: number | null;
  /** Average handler time, milliseconds; `null` when nothing was processed recently. */
  averageDuration?: number | null;
  /** Average time in the queue before a worker dequeued the message, milliseconds. */
  averageWait?: number | null;
}

export interface QueueMessageListItem {
  id: string;
  /** Physical queue name (may end with `-poison`). */
  queueName: string;
  status: QueueMessageStatus;
  /** Visible-at, UTC. */
  date: string;
  dequeueCount: number;
  /** First 200 characters of the payload. */
  payloadPreview?: string | null;
}

export interface QueueMessageDetails extends QueueMessageListItem {
  logicalQueueName: string;
  /** Full JSON payload. */
  payload?: string | null;
  maxRetryCount?: number | null;
}

export interface QueueMessageListParams {
  status: QueueMessageStatus;
  page?: number;
  size?: number;
  search?: string;
}

export interface BulkActionResult {
  affected: number;
}

export interface ScheduledTaskInfo {
  /** `"{group}.{name}"`. */
  id: string;
  name: string;
  group?: string | null;
  description?: string | null;
  jobType?: string | null;
  cronExpression?: string | null;
  status: ScheduledTaskState;
  enabled: boolean;
  /** A DB row exists — the task has run at least once. */
  hasRun: boolean;
  /** A live worker hosts the task, so it can run now even if it has never run. */
  isRegistered: boolean;
  lastExecution?: string | null;
  nextExecution?: string | null;
  executionCount: number;
  lastErrorDate?: string | null;
  lastRunFailed: boolean;
  progress: number;
  workerId?: string | null;
  /** When a pending "run now" request was made; `null` when there is none. */
  runRequestedAt?: string | null;
}

export interface ScheduledTaskDetails extends ScheduledTaskInfo {
  singleton?: boolean | null;
  /** Full exception text of the last failure. */
  errors?: string | null;
  /** Task state (payload) JSON. */
  state?: string | null;
  /** Average run time on the live workers since they started, milliseconds. */
  averageDuration?: number | null;
  /** Time the last run took on a live worker, milliseconds. */
  lastDuration?: number | null;
}

export interface LockInfo {
  id: string;
  name: string;
  expirationDate: string;
  durationSeconds: number;
  isExpired: boolean;
}

/**
 * `Alive` sends its heartbeat, `Stale` missed its last heartbeats (stopping, hung or gone), and
 * `Unknown` isn't registered — only seen from the scheduled tasks it ran (e.g. an older worker).
 */
export type WorkerServerStatus = 'Alive' | 'Stale' | 'Unknown';

export interface WorkerServerInfo {
  /** `{machine}:{process}:{random}`, or the machine name when the worker isn't registered. */
  workerId: string;
  machineName: string;
  status: WorkerServerStatus;
  processId?: number | null;
  applicationName?: string | null;
  version?: string | null;
  startedAt?: string | null;
  /** Last heartbeat, or the max last execution of its tasks when the worker isn't registered. */
  lastSeen?: string | null;
  /** Seconds. */
  heartbeatInterval?: number | null;
  /** Queues the worker consumes. */
  queues: string[];
  /** Ids of the scheduled tasks the worker hosts. */
  scheduledTasks: string[];
  /** Tasks whose last run was on this machine. */
  tasks: number;
  runningTasks: number;
  /** Messages processed since the worker started; `null` when the worker doesn't report metrics. */
  processed?: number | null;
  /** Messages that failed since the worker started. */
  failed?: number | null;
  /** Messages being processed now. Live workers only. */
  processing?: number | null;
  /** Messages processed per minute recently. Live workers only. */
  processedPerMinute?: number | null;
  failedPerMinute?: number | null;
}

/** What the signed-in user may do. Reading needs the `WorkersReader` role, changing anything `WorkersAdmin`. */
export interface WorkerDashboardPermissions {
  canManage: boolean;
}

export interface EnqueueMessageRequest {
  /** JSON of the queue's work item. */
  payload: string;
  /** UTC; omit to make the message visible now. */
  visibleAt?: string | null;
}

export interface RunScheduledTaskResult {
  requestId: string;
  requestedAt: string;
}

/** Typed client for the workers dashboard API (`/api/workers`). Mirrors Indice.Hosting.UI Models. */
@Injectable({ providedIn: 'root' })
export class WorkersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = (inject(WORKERS_API_BASE_URL, { optional: true }) ?? '').replace(
    /\/$/u,
    '',
  );

  getPermissions(): Observable<WorkerDashboardPermissions> {
    return this.http.get<WorkerDashboardPermissions>(`${this.baseUrl}/me`);
  }

  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.baseUrl}/stats`);
  }

  getQueues(): Observable<QueueInfo[]> {
    return this.http.get<QueueInfo[]>(`${this.baseUrl}/queues`);
  }

  /** One logical queue; a queue without messages comes back with zero counters. */
  getQueue(name: string): Observable<QueueInfo> {
    return this.http.get<QueueInfo>(`${this.baseUrl}/queues/${encodeURIComponent(name)}`);
  }

  enqueueMessage(queueName: string, request: EnqueueMessageRequest): Observable<QueueMessageDetails> {
    return this.http.post<QueueMessageDetails>(
      `${this.baseUrl}/queues/${encodeURIComponent(queueName)}/messages`,
      request,
    );
  }

  /** Replaces the payload and requeues the message (visible now, attempts reset, out of poison). */
  updateMessage(id: string, payload: string): Observable<QueueMessageDetails> {
    return this.http.put<QueueMessageDetails>(`${this.baseUrl}/messages/${id}`, { payload });
  }

  getQueueMessages(
    queueName: string,
    p: QueueMessageListParams,
  ): Observable<ResultSet<QueueMessageListItem>> {
    let params = new HttpParams()
      .set('status', p.status)
      .set('page', p.page ?? 1)
      .set('size', p.size ?? 20);
    if (p.search) {
      params = params.set('search', p.search);
    }
    return this.http.get<ResultSet<QueueMessageListItem>>(
      `${this.baseUrl}/queues/${encodeURIComponent(queueName)}/messages`,
      { params },
    );
  }

  getMessage(id: string): Observable<QueueMessageDetails> {
    return this.http.get<QueueMessageDetails>(`${this.baseUrl}/messages/${id}`);
  }

  requeueMessages(ids: string[]): Observable<BulkActionResult> {
    return this.http.post<BulkActionResult>(`${this.baseUrl}/messages/requeue`, { ids });
  }

  deleteMessages(ids: string[]): Observable<BulkActionResult> {
    return this.http.post<BulkActionResult>(`${this.baseUrl}/messages/delete`, { ids });
  }

  getScheduledTasks(): Observable<ScheduledTaskInfo[]> {
    return this.http.get<ScheduledTaskInfo[]>(`${this.baseUrl}/scheduled-tasks`);
  }

  getScheduledTask(id: string): Observable<ScheduledTaskDetails> {
    return this.http.get<ScheduledTaskDetails>(
      `${this.baseUrl}/scheduled-tasks/${encodeURIComponent(id)}`,
    );
  }

  setScheduledTaskEnabled(id: string, enabled: boolean): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/scheduled-tasks/${encodeURIComponent(id)}/enabled`,
      { enabled },
    );
  }

  /** Asks a worker that hosts the task to run it now; returns the pending request if there is one already. */
  runScheduledTask(id: string): Observable<RunScheduledTaskResult> {
    return this.http.post<RunScheduledTaskResult>(
      `${this.baseUrl}/scheduled-tasks/${encodeURIComponent(id)}/run`,
      null,
    );
  }

  getLocks(): Observable<LockInfo[]> {
    return this.http.get<LockInfo[]>(`${this.baseUrl}/locks`);
  }

  releaseLock(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/locks/${id}`);
  }

  getServers(): Observable<WorkerServerInfo[]> {
    return this.http.get<WorkerServerInfo[]>(`${this.baseUrl}/servers`);
  }
}
