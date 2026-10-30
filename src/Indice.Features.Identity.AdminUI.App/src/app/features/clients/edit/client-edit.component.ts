import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
    RouterLink, RouterOutlet, TranslatePipe, RouteTabsComponent, StatusBadgeComponent, CopyButtonComponent, EmptyStateComponent, ClientLogoComponent,
    LucideArrowLeft, LucideCircleAlert, LucideLock, LucideTrash
  ],
  providers: [ClientStore],
  template: `
    <a routerLink="/clients" class="mb-3 inline-flex items-center gap-1 text-sm text-base-content/60 hover:text-base-content">
      <svg lucideArrowLeft class="size-4"></svg>{{ 'Routes.Clients' | translate }}
    </a>

    @if (store.failed()) {
      <div class="panel">
        <app-empty-state [title]="'Clients.Edit.NotFoundTitle' | translate" [message]="'Clients.Edit.NotFoundMessage' | translate">
          <svg icon lucideCircleAlert class="size-5"></svg>
          <a actions routerLink="/clients" class="btn btn-sm">{{ 'Clients.Edit.BackToClients' | translate }}</a>
        </app-empty-state>
      </div>
    } @else if (store.client(); as client) {
      <header class="panel mb-6 flex flex-col gap-4 p-5 sm:flex-row sm:items-center">
        <app-client-logo [src]="client.logoUri" [name]="client.clientName || client.clientId" [size]="56" />
        <div class="min-w-0 flex-1">
          <div class="flex flex-wrap items-center gap-2">
            <h1 class="truncate text-2xl font-bold tracking-tight">{{ client.clientName || client.clientId }}</h1>
            @if (client.enabled) {
              <app-status-badge tone="success">{{ 'Common.Enabled' | translate }}</app-status-badge>
            } @else {
              <app-status-badge tone="neutral">{{ 'Common.Disabled' | translate }}</app-status-badge>
            }
            @if (client.nonEditable) {
              <span class="badge badge-sm badge-warning badge-soft gap-1"><svg lucideLock class="size-3"></svg>{{ 'Common.Reserved' | translate }}</span>
            }
          </div>
          <div class="mt-1 flex flex-wrap items-center gap-1 text-sm">
            <code class="code-chip">{{ client.clientId }}</code>
            <app-copy-button [value]="client.clientId" [label]="'Clients.Fields.ClientIdLabel' | translate" />
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
              {{ 'Common.Enabled' | translate }}
            </label>
            @if (!client.nonEditable) {
              <button type="button" class="btn btn-ghost btn-sm text-error" (click)="remove()"><svg lucideTrash class="size-4"></svg>{{ 'Common.Delete' | translate }}</button>
            }
          </div>
        }
      </header>

      <app-route-tabs class="mb-6" [tabs]="tabs()" />
      <!-- Keep the outlet inside an HTML element: as a direct child of a control flow block it can inherit the SVG namespace of a preceding icon, which makes the routed tab render as an (invisible) SVG element. -->
      <div><router-outlet /></div>
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
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(ClientStore);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();
  protected readonly toggling = signal(false);

  /** Bound from the `:id` route param. */
  public readonly id = input.required<string>();

  protected readonly tabs = computed<RouteTab[]>(() => {
    const grants = this.store.grantTypes();
    const t = (key: string) => this._translate.instant(key) as string;
    return [
      { label: t('Common.Details'), link: 'details', icon: LucideFileText },
      { label: t('Clients.Tabs.Urls'), link: 'urls', icon: LucideLink, hidden: !usesRedirects(grants) },
      { label: t('Clients.Tabs.Secrets'), link: 'secrets', icon: LucideKeyRound },
      { label: t('Clients.Tabs.Resources'), link: 'resources', icon: LucideShapes },
      { label: t('Clients.Tabs.Tokens'), link: 'tokens', icon: LucideTimer },
      { label: t('Clients.Tabs.Claims'), link: 'claims', icon: LucideListChecks },
      { label: t('Clients.Tabs.GrantTypes'), link: 'grant-types', icon: LucideWorkflow },
      { label: t('Clients.Tabs.Theme'), link: 'theme', icon: LucidePalette }
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
        const name = client.clientName || client.clientId;
        this._toast.success(
          this._translate.instant(enabled ? 'Clients.Edit.EnabledTitle' : 'Clients.Edit.DisabledTitle'),
          this._translate.instant(enabled ? 'Clients.Edit.EnabledMessage' : 'Clients.Edit.DisabledMessage', { name }));
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
        title: this._translate.instant('Clients.Edit.DeleteTitle'),
        message: this._translate.instant('Clients.Edit.DeleteMessage', { name: client.clientName || client.clientId }),
        confirmLabel: this._translate.instant('Common.Delete'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteClient(client.clientId!)))
      .subscribe(() => {
        this._toast.success(this._translate.instant('Clients.Edit.DeletedTitle'),
          this._translate.instant('Clients.Edit.DeletedMessage', { name: client.clientName || client.clientId }));
        this._router.navigate(['/clients']);
      });
  }
}
