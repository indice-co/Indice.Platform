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
  isRegistered: boolean;
  workItemType?: string | null;
  handlerType?: string | null;
  /** Milliseconds. */
  pollingInterval?: number | null;
  instanceCount?: number | null;
  maxRetryCount?: number | null;
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
  isRegistered: boolean;
  lastExecution?: string | null;
  nextExecution?: string | null;
  executionCount: number;
  lastErrorDate?: string | null;
  lastRunFailed: boolean;
  progress: number;
  workerId?: string | null;
}

export interface ScheduledTaskDetails extends ScheduledTaskInfo {
  singleton?: boolean | null;
  /** Full exception text of the last failure. */
  errors?: string | null;
  /** Task state (payload) JSON. */
  state?: string | null;
}

export interface LockInfo {
  id: string;
  name: string;
  expirationDate: string;
  durationSeconds: number;
  isExpired: boolean;
}

export interface WorkerServerInfo {
  workerId: string;
  /** Max last execution of its tasks. */
  lastSeen?: string | null;
  tasks: number;
  runningTasks: number;
}

/** Typed client for the workers dashboard API (`/api/workers`). Mirrors Indice.Hosting.UI Models. */
@Injectable({ providedIn: 'root' })
export class WorkersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = (inject(WORKERS_API_BASE_URL, { optional: true }) ?? '').replace(
    /\/$/u,
    '',
  );

  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.baseUrl}/stats`);
  }

  getQueues(): Observable<QueueInfo[]> {
    return this.http.get<QueueInfo[]>(`${this.baseUrl}/queues`);
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

  getLocks(): Observable<LockInfo[]> {
    return this.http.get<LockInfo[]>(`${this.baseUrl}/locks`);
  }

  getServers(): Observable<WorkerServerInfo[]> {
    return this.http.get<WorkerServerInfo[]>(`${this.baseUrl}/servers`);
  }
}
