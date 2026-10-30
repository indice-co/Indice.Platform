import { Component, computed, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { IdentityApiService } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClaimsAssignmentComponent } from '../shared/claims-assignment.component';
import { IdentityResourceStore } from './identity-resource-edit.component';

/** User claims tab of an identity resource. */
@Component({
  selector: 'app-identity-resource-claims',
  imports: [ClaimsAssignmentComponent, TranslatePipe],
  template: `
    <p class="mb-4 max-w-prose text-sm text-base-content/70">
      {{ 'Resources.Identity.ClaimsIntro' | translate }}
    </p>
    <app-claims-assignment [assigned]="claims()" [readonly]="!store.editable()" (added)="add($event)" (removed)="remove($event)" />
  `
})
export class IdentityResourceClaimsComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(IdentityResourceStore);
  protected readonly claims = computed(() => this.store.resource()?.allowedClaims ?? []);

  protected add(claim: string): void {
    this._api.addIdentityResourceClaims(this.store.resource()!.id!, [claim]).subscribe(() => {
      this.store.patch({ allowedClaims: [...this.claims(), claim].sort() });
      this._toast.success(this._translate.instant('Resources.Claims.Added.Title'), this._translate.instant('Resources.Claims.Added.Message', { claim }));
    });
  }

  protected remove(claim: string): void {
    this._api.deleteIdentityResourceClaim(this.store.resource()!.id!, claim).subscribe(() => {
      this.store.patch({ allowedClaims: this.claims().filter(name => name !== claim) });
      this._toast.success(this._translate.instant('Resources.Claims.Removed.Title'), this._translate.instant('Resources.Claims.Removed.Message', { claim }));
    });
  }
}
