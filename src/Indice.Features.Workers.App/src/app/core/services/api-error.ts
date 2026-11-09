import { HttpErrorResponse } from '@angular/common/http';

/** Turns an API failure into one line a person can act on. */
export function describeError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 403) {
      return "You don't have access. The workers dashboard requires the `workers` scope or the Administrator role.";
    }
    if (error.status === 0) {
      return 'Cannot reach the server.';
    }
    return error.error?.title ?? error.message;
  }
  return error instanceof Error ? error.message : String(error);
}
