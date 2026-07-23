import { ConversationListItem } from '../../core/services/dex-api.service';

/** Aggregated, display-ready usage figures derived from the caller's chat sessions. */
export interface UsageStats {
  /** Total conversations server-side (the result-set `count`, not just the fetched page). */
  totalConversations: number;
  totalPromptTokens: number;
  totalCompletionTokens: number;
  totalTokens: number;
  /** Earliest session creation seen — "member since". `null` when there are no sessions. */
  activeSince: Date | null;
  /** Most recent activity across sessions. `null` when there are no sessions. */
  lastActivity: Date | null;
  /** A handful of the most recently active sessions, for a quick-glance list. */
  recent: ConversationListItem[];
}

/** Reduce a page of sessions + the server-side total into the profile's usage view model. */
export function toUsageStats(items: ConversationListItem[], total: number): UsageStats {
  let prompt = 0;
  let completion = 0;
  let since: Date | null = null;
  let last: Date | null = null;

  for (const s of items) {
    // Token sums cover the fetched page; for a demo that is the whole history (size 100).
    prompt += s.totalPromptTokens ?? 0;
    completion += s.totalCompletionTokens ?? 0;
    if (s.createdAt && (!since || s.createdAt < since)) {
      since = s.createdAt;
    }
    if (s.lastActivityAt && (!last || s.lastActivityAt > last)) {
      last = s.lastActivityAt;
    }
  }

  return {
    totalConversations: total,
    totalPromptTokens: prompt,
    totalCompletionTokens: completion,
    totalTokens: prompt + completion,
    activeSince: since,
    lastActivity: last,
    recent: items.slice(0, 5),
  };
}

/** Compact token/number formatting: 980 → "980", 48 230 → "48.2k", 1 200 000 → "1.2M". */
export function formatCount(value: number): string {
  if (value < 1000) {
    return String(value);
  }
  if (value < 1_000_000) {
    return `${trimZero(value / 1000)}k`;
  }
  return `${trimZero(value / 1_000_000)}M`;
}

function trimZero(n: number): string {
  return n.toFixed(1).replace(/\.0$/, '');
}

/** Two-letter initials from a display name (mirrors the shell's avatar logic). */
export function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return '?';
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 1).toUpperCase();
  }
  return (parts[0].slice(0, 1) + parts[parts.length - 1].slice(0, 1)).toUpperCase();
}

/** Map a language code from the Dex taxonomy to a friendly label (falls back to the upper-cased code). */
const LANGUAGE_LABELS: Record<string, string> = { en: 'English', el: 'Greek' };
export function languageLabel(code: string | undefined): string {
  if (!code) {
    return 'Not set';
  }
  return LANGUAGE_LABELS[code.toLowerCase()] ?? code.toUpperCase();
}

/** Capitalize a response-style value (`concise` → `Concise`); empty → "Not set". */
export function styleLabel(style: string | undefined): string {
  if (!style) {
    return 'Not set';
  }
  return style.charAt(0).toUpperCase() + style.slice(1);
}

/** Pull role(s) out of OIDC id-token claims — the `role` claim may be a string or string[]. */
export function rolesFromClaims(claims: Record<string, unknown> | undefined): string[] {
  if (!claims) {
    return [];
  }
  const raw = claims['role'] ?? claims['roles'];
  if (Array.isArray(raw)) {
    return raw.map(String).filter(Boolean);
  }
  if (typeof raw === 'string' && raw.length) {
    return [raw];
  }
  return [];
}
