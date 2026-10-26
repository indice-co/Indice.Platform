import { Component, Injectable, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { Observable, map, shareReplay } from 'rxjs';
import { ClaimTypeInfo, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { TransferListComponent } from 'src/app/ui/transfer/transfer-list.component';

/** Caches the full list of claim types for the claim pickers of the resource pages. */
@Injectable({ providedIn: 'root' })
export class ClaimTypesCatalog {
  private readonly _api = inject(IdentityApiService);
  private _all$?: Observable<ClaimTypeInfo[]>;

  public all(): Observable<ClaimTypeInfo[]> {
    this._all$ ??= this._api.getClaimTypes(1, 2147483647, 'name+').pipe(map(result => result.items ?? []), shareReplay(1));
    return this._all$;
  }
}

/**
 * Picks the user claims of a resource or scope. The host owns the assigned claim names and reacts to `added` / `removed`
 * (usually by calling the API and then updating `assigned`).
 */
@Component({
  selector: 'app-claims-assignment',
  imports: [TransferListComponent],
  template: `
    @if (loading()) {
      <div class="grid gap-4 md:grid-cols-2">
        <div class="skeleton h-64 w-full"></div>
        <div class="skeleton h-64 w-full"></div>
      </div>
    } @else {
      <app-transfer-list [available]="catalog()" [selected]="selected()" [key]="key" [label]="label" [sublabel]="sublabel"
        availableTitle="Available claims" selectedTitle="Included claims" emptySelected="No claims included yet."
        [readonly]="readonly()" (added)="added.emit($event.name!)" (removed)="removed.emit($event.name!)" />
    }
  `
})
export class ClaimsAssignmentComponent implements OnInit {
  private readonly _catalog = inject(ClaimTypesCatalog);

  /** Names of the claims currently assigned. */
  public readonly assigned = input<string[]>([]);
  public readonly readonly = input(false);
  public readonly added = output<string>();
  public readonly removed = output<string>();

  protected readonly loading = signal(true);
  protected readonly catalog = signal<ClaimTypeInfo[]>([]);
  protected readonly selected = computed(() => {
    const byName = new Map(this.catalog().map(claim => [claim.name!, claim]));
    // claims assigned on the server but missing from the catalog are still shown so they can be removed
    return this.assigned().map(name => byName.get(name) ?? new ClaimTypeInfo({ name }));
  });

  protected readonly key = (claim: ClaimTypeInfo) => claim.name ?? '';
  protected readonly label = (claim: ClaimTypeInfo) => claim.name ?? '';
  protected readonly sublabel = (claim: ClaimTypeInfo) => claim.displayName || claim.description;

  public ngOnInit(): void {
    this._catalog.all().subscribe({
      next: claims => {
        this.catalog.set(claims);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
