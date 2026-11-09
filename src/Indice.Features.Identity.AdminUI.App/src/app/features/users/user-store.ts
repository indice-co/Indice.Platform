import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { Observable, map, shareReplay } from 'rxjs';
import { ClaimTypeInfo, IdentityApiService, RoleInfo, SingleUserInfo } from 'src/app/core/services/identity-api.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { nameFromClaims } from './user-utils';

/** Refresh bus scope telling the open user page to reload (e.g. after a side pane changed a claim). */
export const USER_REFRESH_SCOPE = 'user';

/**
 * State of the user shown by the user page and its tabs. Provided by `UserEditComponent`, so it reads the `:id` of that
 * route and is shared with the routed tabs.
 */
@Injectable()
export class UserStore {
  private readonly _api = inject(IdentityApiService);

  public readonly id = toSignal(inject(ActivatedRoute).paramMap.pipe(map(params => params.get('id') ?? '')), { requireSync: true });
  public readonly user = signal<SingleUserInfo | null>(null);
  public readonly failed = signal(false);
  public readonly displayName = computed(() => {
    const user = this.user();
    return nameFromClaims(user?.claims) || user?.userName || user?.email || '';
  });

  /** All claim types, loaded once per user page. */
  public readonly claimTypes$: Observable<ClaimTypeInfo[]> = this._api.getClaimTypes(1, 2147483647, 'name+', undefined)
    .pipe(map(result => result.items ?? []), shareReplay(1));
  /** All roles, loaded once per user page. */
  public readonly roles$: Observable<RoleInfo[]> = this._api.getRoles(1, 2147483647, 'name+', undefined)
    .pipe(map(result => result.items ?? []), shareReplay(1));

  constructor() {
    inject(ActivatedRoute).paramMap.pipe(takeUntilDestroyed()).subscribe(() => {
      this.user.set(null);
      this.reload();
    });
    inject(RefreshBus).on(USER_REFRESH_SCOPE).pipe(takeUntilDestroyed()).subscribe(() => this.reload());
  }

  public reload(): void {
    const id = this.id();
    if (!id) {
      return;
    }
    this.failed.set(false);
    this._api.getUser(id).subscribe({
      next: user => this.user.set(user),
      error: () => this.failed.set(true)
    });
  }

  /** Applies a local change to the loaded user. */
  public patch(changes: Partial<SingleUserInfo>): void {
    const user = this.user();
    if (user) {
      this.user.set(SingleUserInfo.fromJS({ ...user.toJSON(), ...changes }));
    }
  }
}
