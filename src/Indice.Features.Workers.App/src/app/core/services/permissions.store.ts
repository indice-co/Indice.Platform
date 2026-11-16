import { Injectable, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';

import { WorkersApiService } from './workers-api.service';

/**
 * What the signed-in user may change. Loaded once from `/me`; until it answers (or when it fails)
 * the write actions stay hidden. The server enforces the same rule on every write anyway.
 */
@Injectable({ providedIn: 'root' })
export class PermissionsStore {
  private readonly api = inject(WorkersApiService);

  /** Requeue, delete, edit, enqueue, enable/disable, run now and release lock. */
  readonly canManage = signal(false);

  private loaded = false;

  load(): void {
    if (this.loaded) {
      return;
    }
    this.loaded = true;
    this.api
      .getPermissions()
      .pipe(
        catchError(() => {
          this.loaded = false;
          return of({ canManage: false });
        }),
      )
      .subscribe((permissions) => this.canManage.set(permissions.canManage));
  }
}
