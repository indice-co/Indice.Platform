/**
 * Bookkeeping for the Viva Smart Checkout redirect: before leaving, the chat remembers which conversation and case
 * it is paying; when Viva sends the browser back (`?t={transactionId}&s={orderCode}`), the chat page reopens that
 * conversation and the Viva payment part confirms the transaction with the server.
 */
const STORAGE_KEY = 'dex.viva.checkout';

export interface VivaPendingCheckout {
  caseId: string;
  conversationId?: string | null;
  /** The Smart Checkout order code; Viva echoes it back as `s`, tying the return to this exact checkout. */
  orderCode?: string | null;
}

export function rememberVivaCheckout(pending: VivaPendingCheckout): void {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(pending));
  } catch {
    // Storage unavailable: the user can still reopen the conversation manually.
  }
}

function storedVivaCheckout(): VivaPendingCheckout | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    const parsed = raw ? (JSON.parse(raw) as Partial<VivaPendingCheckout>) : null;
    return parsed && typeof parsed.caseId === 'string'
      ? { caseId: parsed.caseId, conversationId: parsed.conversationId ?? null, orderCode: parsed.orderCode ?? null }
      : null;
  } catch {
    return null;
  }
}

/**
 * The checkout this page load is returning from — only when the URL carries Viva's return parameters and they match the
 * stored order. A record left over from an older checkout is discarded, so it can never reopen the wrong chat.
 */
export function pendingVivaCheckout(): VivaPendingCheckout | null {
  const params = new URLSearchParams(window.location.search);
  if (!params.has('t') && !params.has('s')) {
    return null;
  }
  const stored = storedVivaCheckout();
  const orderCode = params.get('s');
  if (stored && orderCode && stored.orderCode && stored.orderCode !== orderCode) {
    clearVivaCheckout();
    return null;
  }
  return stored;
}

/** The order code inside a Smart Checkout URL (`...?ref={orderCode}`). */
export function vivaOrderCode(redirectUrl: string): string | null {
  try {
    return new URL(redirectUrl).searchParams.get('ref');
  } catch {
    return null;
  }
}

/** The transaction id Viva appended to the return URL, if any. */
export function vivaReturnTransactionId(): string | null {
  const params = new URLSearchParams(window.location.search);
  return params.get('t') || null;
}

export function clearVivaCheckout(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // ignore
  }
}
