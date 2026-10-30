import { Component, OnChanges, inject, input } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, switchMap } from 'rxjs';
import { LucideArrowLeft, LucideInfo, LucidePencil, LucideTrash, LucideUsers } from '@lucide/angular';
import { MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { RouteTab, RouteTabsComponent } from 'src/app/ui/tabs/route-tabs.component';
import { DistributionListEditStore } from './distribution-list-edit-store.service';

/** Distribution list page: header with actions, tabs for details and contacts. */
@Component({
  selector: 'app-distribution-list',
  imports: [RouterLink, RouterOutlet, TranslatePipe, PageHeaderComponent, RouteTabsComponent, LucideArrowLeft, LucidePencil, LucideTrash],
  providers: [DistributionListEditStore],
  template: `
    <app-page-header [title]="store.distributionList()?.name || ''" [subtitle]="'DistributionLists.BasicInformationDescription' | translate">
      <a leading class="btn btn-ghost btn-xs -ml-2 mb-2" routerLink="..">
        <svg lucideArrowLeft class="size-4"></svg>{{ 'DistributionLists.ListsTitle' | translate }}
      </a>
      @if (!store.distributionList()) {
        <div class="skeleton mt-1 h-8 w-64"></div>
      } @else if (store.distributionList()?.alias) {
        <code class="mt-2 inline-block rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ store.distributionList()?.alias }}</code>
      }
      <div actions class="flex flex-wrap gap-2">
        @if (store.distributionList(); as list) {
          <button type="button" class="btn btn-sm btn-ghost text-error" (click)="delete()">
            <svg lucideTrash class="size-4"></svg>{{ 'DistributionLists.Delete' | translate }}
          </button>
          <a class="btn btn-sm btn-primary" [routerLink]="pane.link('edit-distribution-list', { distributionListId: list.id! })">
            <svg lucidePencil class="size-4"></svg>{{ 'DistributionLists.EditList' | translate }}
          </a>
        }
      </div>
    </app-page-header>

    <app-route-tabs class="mb-6" [tabs]="tabs" />
    <router-outlet />
  `
})
export class DistributionListEditComponent implements OnChanges {
  private readonly _api = inject(MessagesApiClient);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _router = inject(Router);
  protected readonly store = inject(DistributionListEditStore);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `:distributionListId` route param. */
  public readonly distributionListId = input.required<string>();

  protected readonly tabs: RouteTab[] = [
    { label: 'DistributionLists.BasicInformation', link: 'distribution-list-details', icon: LucideInfo },
    { label: 'DistributionLists.Contacts', link: 'distribution-list-contacts', icon: LucideUsers }
  ];

  constructor() {
    inject(RefreshBus).on('distribution-list').pipe(takeUntilDestroyed()).subscribe(() => this.store.reload());
  }

  public ngOnChanges(): void {
    // runs before the tabs are created, so they can read the list id from the store right away
    this.store.load(this.distributionListId());
  }

  protected delete(): void {
    const list = this.store.distributionList();
    if (!list) {
      return;
    }
    this._dialog
      .confirm({ title: 'DistributionLists.Delete', message: 'DistributionLists.DeleteConfirmMessage', messageParams: { name: list.name }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._api.deleteDistributionList(list.id!))
      )
      .subscribe(() => {
        this._toaster.show(ToastType.Success, 'DistributionLists.DeleteSuccessTitle', 'DistributionLists.DeleteSuccessMessage', undefined, { name: list.name });
        this._router.navigate(['/distribution-lists']);
      });
  }
}
