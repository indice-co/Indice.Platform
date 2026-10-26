import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import {
  LucideArrowLeft, LucideCircleAlert, LucideFileText, LucideKeyRound, LucideLink, LucideListChecks, LucideLock, LucidePalette, LucideShapes,
  LucideTimer, LucideTrash, LucideWorkflow
} from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { RouteTab, RouteTabsComponent } from 'src/app/ui/tabs/route-tabs.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClientLogoComponent } from '../client-logo.component';
import { usesRedirects } from '../client-utils';
import { ClientStore } from './client-store.service';

/** Client detail page: summary header and tabs; the tabs share the loaded client through {@link ClientStore}. */
@Component({
  selector: 'app-client-edit',
  imports: [
    RouterLink, RouterOutlet, RouteTabsComponent, StatusBadgeComponent, CopyButtonComponent, EmptyStateComponent, ClientLogoComponent,
    LucideArrowLeft, LucideCircleAlert, LucideLock, LucideTrash
  ],
  providers: [ClientStore],
  template: `
    <a routerLink="/clients" class="mb-3 inline-flex items-center gap-1 text-sm text-base-content/60 hover:text-base-content">
      <svg lucideArrowLeft class="size-4"></svg>Clients
    </a>

    @if (store.failed()) {
      <div class="panel">
        <app-empty-state title="Client not found" message="It may have been deleted, or the link is wrong.">
          <svg icon lucideCircleAlert class="size-5"></svg>
          <a actions routerLink="/clients" class="btn btn-sm">Back to clients</a>
        </app-empty-state>
      </div>
    } @else if (store.client(); as client) {
      <header class="panel mb-6 flex flex-col gap-4 p-5 sm:flex-row sm:items-center">
        <app-client-logo [src]="client.logoUri" [name]="client.clientName || client.clientId" [size]="56" />
        <div class="min-w-0 flex-1">
          <div class="flex flex-wrap items-center gap-2">
            <h1 class="truncate text-2xl font-bold tracking-tight">{{ client.clientName || client.clientId }}</h1>
            @if (client.enabled) {
              <app-status-badge tone="success">Enabled</app-status-badge>
            } @else {
              <app-status-badge tone="neutral">Disabled</app-status-badge>
            }
            @if (client.nonEditable) {
              <span class="badge badge-sm badge-warning badge-soft gap-1"><svg lucideLock class="size-3"></svg>Reserved</span>
            }
          </div>
          <div class="mt-1 flex flex-wrap items-center gap-1 text-sm">
            <code class="code-chip">{{ client.clientId }}</code>
            <app-copy-button [value]="client.clientId" label="client id" />
            @if (client.description) {
              <span class="ml-2 truncate text-base-content/60">{{ client.description }}</span>
            }
          </div>
        </div>
        @if (canEdit) {
          <div class="flex items-center gap-3">
            <label class="flex cursor-pointer items-center gap-2 text-sm font-medium">
              <input type="checkbox" class="toggle toggle-success toggle-sm" [checked]="client.enabled" [disabled]="toggling()"
                (change)="setEnabled($any($event.target).checked, $any($event.target))" />
              Enabled
            </label>
            @if (!client.nonEditable) {
              <button type="button" class="btn btn-ghost btn-sm text-error" (click)="remove()"><svg lucideTrash class="size-4"></svg>Delete</button>
            }
          </div>
        }
      </header>

      <app-route-tabs class="mb-6" [tabs]="tabs()" />
      <router-outlet />
    } @else {
      <div class="panel mb-6 flex items-center gap-4 p-5">
        <div class="skeleton size-14"></div>
        <div class="flex-1 space-y-2"><div class="skeleton h-6 w-64"></div><div class="skeleton h-4 w-40"></div></div>
      </div>
      <div class="skeleton h-10 w-full"></div>
    }
  `
})
export class ClientEditComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _router = inject(Router);
  protected readonly store = inject(ClientStore);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();
  protected readonly toggling = signal(false);

  /** Bound from the `:id` route param. */
  public readonly id = input.required<string>();

  protected readonly tabs = computed<RouteTab[]>(() => {
    const grants = this.store.grantTypes();
    return [
      { label: 'Details', link: 'details', icon: LucideFileText },
      { label: 'URLs', link: 'urls', icon: LucideLink, hidden: !usesRedirects(grants) },
      { label: 'Secrets', link: 'secrets', icon: LucideKeyRound },
      { label: 'Resources', link: 'resources', icon: LucideShapes },
      { label: 'Tokens', link: 'tokens', icon: LucideTimer },
      { label: 'Claims', link: 'claims', icon: LucideListChecks },
      { label: 'Grant types', link: 'grant-types', icon: LucideWorkflow },
      { label: 'Theme', link: 'theme', icon: LucidePalette }
    ];
  });

  public ngOnInit(): void {
    this.store.load(this.id());
  }

  protected setEnabled(enabled: boolean, checkbox: HTMLInputElement): void {
    this.toggling.set(true);
    this.store.update({ enabled }).subscribe({
      next: client => {
        this.toggling.set(false);
        this._toast.success(enabled ? 'Client enabled' : 'Client disabled', `'${client.clientName || client.clientId}' ${enabled ? 'can' : 'can no longer'} request tokens.`);
      },
      error: () => {
        this.toggling.set(false);
        checkbox.checked = !enabled;
      }
    });
  }

  protected remove(): void {
    const client = this.store.client()!;
    this._dialog
      .confirm({
        title: 'Delete client?',
        message: `Client '${client.clientName || client.clientId}' will be deleted permanently and applications using it will stop working. This cannot be undone.`,
        confirmLabel: 'Delete',
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteClient(client.clientId!)))
      .subscribe(() => {
        this._toast.success('Client deleted', `Client '${client.clientName || client.clientId}' was deleted.`);
        this._router.navigate(['/clients']);
      });
  }
}
