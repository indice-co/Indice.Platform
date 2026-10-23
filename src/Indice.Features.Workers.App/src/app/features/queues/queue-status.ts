import { QueueInfo, QueueMessageStatus } from '../../core/services/workers-api.service';

/** Every message status, in the order the queue detail menu lists them (Hangfire's job states). */
export const MESSAGE_STATUSES: readonly QueueMessageStatus[] = [
  'Enqueued',
  'Scheduled',
  'Retrying',
  'Poison',
  'Dequeued',
];

/**
 * Count badges in the same colours as `StatusBadgeComponent`, which renders a label rather than a
 * number — so a "3" in the Poison column reads as poison at a glance.
 */
export const COUNT_BADGE: Record<QueueMessageStatus, string> = {
  Enqueued: 'badge-info',
  Scheduled: 'badge-secondary badge-soft',
  Retrying: 'badge-warning',
  Poison: 'badge-error',
  Dequeued: 'badge-ghost',
};

/** The `?status=` query argument, falling back to Enqueued for missing or unknown values. */
export function toStatus(value: string | null | undefined): QueueMessageStatus {
  return MESSAGE_STATUSES.find((s) => s === value) ?? 'Enqueued';
}

/** How many messages of `queue` are in `status`; 0 when the queue has no waiting messages. */
export function countOf(queue: QueueInfo | null | undefined, status: QueueMessageStatus): number {
  if (!queue) {
    return 0;
  }
  switch (status) {
    case 'Enqueued':
      return queue.enqueued;
    case 'Scheduled':
      return queue.scheduled;
    case 'Retrying':
      return queue.retrying;
    case 'Poison':
      return queue.poison;
    case 'Dequeued':
      return queue.dequeued;
  }
}

/** `"My.App.OrderHandler`1[[...]], My.App"` → `"OrderHandler"`: CLR names don't fit a cell. */
export function shortTypeName(type: string | null | undefined): string | null {
  if (!type) {
    return null;
  }
  const name = type.split(/[`[,]/u)[0];
  return name.slice(name.lastIndexOf('.') + 1) || type;
}

/** `plural(1, 'queue')` → "1 queue", `plural(3, 'queue')` → "3 queues". */
export function plural(count: number, word: string): string {
  return `${count} ${count === 1 ? word : `${word}s`}`;
}

/** A bulk action's toast. Fewer affected than requested means a worker got there first. */
export function describeBulkResult(
  verb: 'requeued' | 'enqueued' | 'deleted',
  affected: number,
  requested: number,
): string {
  if (requested === 1 && affected === 0) {
    return 'The message was already processed';
  }
  const text = `${plural(affected, 'message')} ${verb}`;
  return affected < requested ? `${text} · some were already processed` : text;
}
