import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from 'src/app/core/services/auth.service';
import { ApiScopeInfo, IdentityResourceInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { TransferListComponent } from 'src/app/ui/transfer/transfer-list.component';
import { isMachineOnly } from '../client-utils';
import { ClientStore } from './client-store.service';

type ResourceKind = 'apiResources' | 'identityResources';

/** The API scopes and identity resources a client may request. Changes apply immediately. */
@Component({
  selector: 'app-client-resources',
  imports: [TransferListComponent],
  template: `
    @if (showIdentity()) {
      <div role="tablist" class="tabs tabs-box mb-4 w-fit">
        <button type="button" role="tab" class="tab" [class.tab-active]="view() === 'apiResources'" (click)="view.set('apiResources')">
          API scopes <span class="badge badge-sm badge-ghost ml-2 tabular">{{ client()?.apiResources?.length ?? 0 }}</span>
        </button>
        <button type="button" role="tab" class="tab" [class.tab-active]="view() === 'identityResources'" (click)="view.set('identityResources')">
          Identity resources <span class="badge badge-sm badge-ghost ml-2 tabular">{{ client()?.identityResources?.length ?? 0 }}</span>
        </button>
      </div>
    }

    @if (view() === 'apiResources' || !showIdentity()) {
      <p class="mb-4 text-sm text-base-content/70">API scopes this client may request access tokens for.</p>
      @if (apiScopes(); as scopes) {
        <app-transfer-list [available]="scopes" [selected]="selectedApi()" [key]="byName" [label]="byName" [sublabel]="byDescription"
          availableTitle="Available API scopes" selectedTitle="Granted to this client" emptySelected="This client cannot call any API yet."
          [readonly]="!canEdit || busy()" (added)="change('apiResources', $event.name!, true)" (removed)="change('apiResources', $event.name!, false)" />
      } @else {
        <div class="skeleton h-72 w-full"></div>
      }
    } @else {
      <p class="mb-4 text-sm text-base-content/70">Identity information (user claims) this client may request.</p>
      @if (identityResources(); as resources) {
        <app-transfer-list [available]="resources" [selected]="selectedIdentity()" [key]="byName" [label]="byName" [sublabel]="byDescription"
          availableTitle="Available identity resources" selectedTitle="Granted to this client" emptySelected="No identity resources granted."
          [readonly]="!canEdit || busy()" (added)="change('identityResources', $event.name!, true)" (removed)="change('identityResources', $event.name!, false)" />
      } @else {
        <div class="skeleton h-72 w-full"></div>
      }
    }
  `
})
export class ClientResourcesComponent {
  private readonly _store = inject(ClientStore);
  private readonly _toast = inject(ToastService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  protected readonly client = this._store.client;
  protected readonly view = signal<ResourceKind>('apiResources');
  protected readonly busy = signal(false);
  protected readonly showIdentity = computed(() => !isMachineOnly(this._store.grantTypes()));
  protected readonly apiScopes = toSignal(this._store.getApiScopes());
  protected readonly identityResources = toSignal(this._store.getIdentityResources());
  protected readonly selectedApi = computed(() => (this.apiScopes() ?? []).filter(x => this.client()?.apiResources?.includes(x.name!)));
  protected readonly selectedIdentity = computed(() => (this.identityResources() ?? []).filter(x => this.client()?.identityResources?.includes(x.name!)));

  protected readonly byName = (item: IdentityResourceInfo | ApiScopeInfo) => item.name ?? '';
  protected readonly byDescription = (item: IdentityResourceInfo | ApiScopeInfo) => item.displayName || item.description;

  protected change(kind: ResourceKind, name: string, add: boolean): void {
    this.busy.set(true);
    (add ? this._store.addResource(kind, name) : this._store.removeResource(kind, name)).subscribe({
      next: () => {
        this.busy.set(false);
        this._toast.success(add ? 'Access granted' : 'Access removed', `'${name}' was ${add ? 'granted to' : 'removed from'} the client.`);
      },
      error: () => this.busy.set(false)
    });
  }
}
