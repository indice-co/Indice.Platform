import { Injectable } from '@angular/core';
import { Observable, Subject, filter, map } from 'rxjs';

/**
 * Tells pages that data they show has changed elsewhere, typically a side pane form telling the list behind it to
 * reload after a save. Scopes are free-form names such as `'message-types'`.
 */
@Injectable({ providedIn: 'root' })
export class RefreshBus {
  private readonly _changes = new Subject<string>();

  public notify(scope: string): void {
    this._changes.next(scope);
  }

  public on(scope: string): Observable<void> {
    return this._changes.pipe(filter(changed => changed === scope), map(() => undefined));
  }
}
