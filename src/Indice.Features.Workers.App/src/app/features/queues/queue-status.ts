import { formatInterval } from '../../core/services/live-refresh.service';
import { QueueInfo, QueueMessageStatus } from '../../core/services/workers-api.service';

/** Every message status (Hangfire's job states); the first one is the default. */
export const MESSAGE_STATUSES: readonly QueueMessageStatus[] = [
  'Enqueued',
  'Scheduled',
  'Retrying',
  'Poison',
  'Dequeued',
];

/** The order a message moves through: delayed → ready → failed → out of retries. */
export const LIFECYCLE: readonly QueueMessageStatus[] = ['Scheduled', 'Enqueued', 'Retrying', 'Poison', 'Dequeued'];

/** How a status paints, in the colours of `StatusBadgeComponent`. */
export interface StatusTone {
  /** A non-zero count. */
  text: string;
  /** The status's share of a mix bar. */
  bar: string;
  /** The tinted square behind the status icon. */
  chip: string;
}

/** Literal class names, so Tailwind's scanner generates them. */
export const STATUS_TONE: Record<QueueMessageStatus, StatusTone> = {
  Scheduled: { text: 'text-secondary', bar: 'bg-secondary', chip: 'bg-secondary/10 text-secondary' },
  Enqueued: { text: 'text-info', bar: 'bg-info', chip: 'bg-info/10 text-info' },
  Retrying: { text: 'text-warning', bar: 'bg-warning', chip: 'bg-warning/15 text-warning' },
  Poison: { text: 'text-error', bar: 'bg-error', chip: 'bg-error/10 text-error' },
  Dequeued: { text: 'text-base-content', bar: 'bg-base-content/30', chip: 'bg-base-content/10 text-base-content/70' },
};

/** The tint of an icon with nothing to report. */
export const NEUTRAL_CHIP = 'bg-base-content/8 text-base-content/70';

/** A queue's icon takes the colour of its worst status, so a queue with poison stands out in a long list. */
export function queueChip(queue: QueueInfo): string {
  if (queue.poison > 0) {
    return STATUS_TONE.Poison.chip;
  }
  return queue.retrying > 0 ? STATUS_TONE.Retrying.chip : NEUTRAL_CHIP;
}

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

/** A polling interval: sub-second ones stay in ms, the rest read like the refresh picker (`30 s`, `1 min`). */
export function formatPolling(ms: number): string {
  return ms < 1000 ? `${ms} ms` : formatInterval(ms);
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
