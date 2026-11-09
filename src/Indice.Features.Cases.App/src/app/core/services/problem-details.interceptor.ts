import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService, ToastType } from 'src/app/ui/toast/toast.service';

interface Problem {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/**
 * Reports failed requests as an error toast, using the server's problem details (validation errors included) when present.
 * Reads are only reported for 400: pages render their own failed / not found / hidden states. 401 is left to the auth flow.
 * The original error is rethrown, so callers still reset their busy state or navigate away.
 */
export const problemDetailsInterceptor: HttpInterceptorFn = (request, next) => {
  const toasts = inject(ToastService);
  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status !== 401 && (request.method !== 'GET' || error.status === 400)) {
        readProblem(error.error).then(problem => toasts.show(ToastType.Error, 'toasts.error.title', toMessage(error.status, problem), 8000));
      }
      return throwError(() => error);
    })
  );
};

/** The generated client requests blobs, so the body usually arrives as a Blob; other callers may get text or JSON. */
async function readProblem(body: unknown): Promise<Problem | null> {
  try {
    const value = body instanceof Blob ? await body.text() : body;
    const problem = typeof value === 'string' ? JSON.parse(value) : value;
    return problem && typeof problem === 'object' ? problem as Problem : null;
  } catch {
    return null;
  }
}

function toMessage(status: number, problem: Problem | null): string {
  const errors = Object.entries(problem?.errors ?? {}).flatMap(([field, messages]) =>
    (Array.isArray(messages) ? messages : [messages]).map(message => withField(field, String(message))));
  if (errors.length > 0) {
    return [...new Set(errors)].join('\n');
  }
  if (problem?.detail) {
    return problem.detail;
  }
  if (status === 0) {
    return 'toasts.offline.body';
  }
  if (status === 403) {
    return 'toasts.forbidden.body';
  }
  return problem?.title || 'toasts.error.body';
}

/** Prefixes the field name unless the message already mentions it (e.g. "'Code' must not be empty."). */
function withField(field: string, message: string): string {
  return !field || message.toLowerCase().includes(field.toLowerCase()) ? message : `${field}: ${message}`;
}
