import { InjectionToken } from '@angular/core';
import { Observable, Subject } from 'rxjs';

/** Data passed to `DialogService.open`, injectable in the dialog content component. */
export const DIALOG_DATA = new InjectionToken<unknown>('DIALOG_DATA');

/** Handle to an open dialog. Inject it in the content component to close the dialog with a result. */
export class DialogRef<R = unknown> {
  private readonly _closed = new Subject<R | undefined>();
  /** Emits once, with the result (or `undefined` when dismissed), then completes. */
  public readonly closed: Observable<R | undefined> = this._closed.asObservable();
  /** @internal */
  public _onClose: () => void = () => void 0;

  public close(result?: R): void {
    this._onClose();
    this._closed.next(result);
    this._closed.complete();
  }
}
