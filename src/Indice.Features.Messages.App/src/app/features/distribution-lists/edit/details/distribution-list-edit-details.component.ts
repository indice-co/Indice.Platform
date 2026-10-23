import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucidePencil } from '@lucide/angular';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { DistributionListEditStore } from '../distribution-list-edit-store.service';

/** "Basic information" tab of the distribution list page. */
@Component({
  selector: 'app-distribution-list-details-edit',
  imports: [DatePipe, RouterLink, TranslatePipe, LucidePencil],
  template: `
    <section class="panel p-6">
      <div class="mb-5 flex items-start justify-between gap-4">
        <div>
          <h2 class="text-lg font-semibold">{{ 'DistributionLists.BasicInformation' | translate }}</h2>
          <p class="mt-0.5 text-sm text-base-content/60">{{ 'DistributionLists.BasicInformationDescription' | translate }}</p>
        </div>
        @if (store.distributionList(); as list) {
          <a class="btn btn-ghost btn-sm" [routerLink]="pane.link('edit-distribution-list', { distributionListId: list.id! })">
            <svg lucidePencil class="size-4"></svg>{{ 'DistributionLists.EditList' | translate }}
          </a>
        }
      </div>
      @if (store.distributionList(); as list) {
        <dl class="grid grid-cols-1 gap-x-6 gap-y-5 sm:grid-cols-2">
          <div>
            <dt class="text-xs text-base-content/60">{{ 'DistributionLists.ListTitle' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ list.name || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'DistributionLists.Alias' | translate }}</dt>
            <dd class="mt-1 font-medium">
              @if (list.alias) { <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ list.alias }}</code> } @else { — }
            </dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'DistributionLists.CreatedBy' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ list.createdBy || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'DistributionLists.CreatedAt' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ list.createdAt ? (list.createdAt | date: 'medium') : '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'DistributionLists.UpdatedBy' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ list.updatedBy || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'DistributionLists.UpdatedAt' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ list.updatedAt ? (list.updatedAt | date: 'medium') : '—' }}</dd>
          </div>
        </dl>
      } @else {
        <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
          @for (row of [1, 2, 3, 4]; track row) {
            <div class="space-y-2"><div class="skeleton h-3 w-24"></div><div class="skeleton h-4 w-40"></div></div>
          }
        </div>
      }
    </section>
  `
})
export class DistributionListDetailsEditComponent {
  protected readonly store = inject(DistributionListEditStore);
  protected readonly pane = inject(SidePaneService);
}
