import { Component, inject } from '@angular/core';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, switchMap } from 'rxjs';
import { LucidePencil, LucideTrash } from '@lucide/angular';
import { MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { BoolIconComponent, MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { templateTypeBadge, templateTypeLabel } from '../../template-type';
import { TemplateEditStore } from '../template-edit-store.service';
import { TemplateEditSection } from './rightpane/template-edit-details-rightpane.component';

@Component({
  selector: 'app-template-details-edit',
  imports: [DatePipe, NgTemplateOutlet, RouterLink, TranslatePipe, BoolIconComponent, MessageTypeBadgeComponent, LucidePencil, LucideTrash],
  template: `
    @let template = store.template();
    <section class="panel">
      <div class="flex flex-col gap-3 border-b border-base-300 p-5 sm:flex-row sm:items-start sm:justify-between sm:p-6">
        <div>
          <h2 class="font-semibold">{{ 'Templates.BasicInformationHeader' | translate }}</h2>
          <p class="text-sm text-base-content/70">{{ 'Templates.BasicInformationDescription' | translate }}</p>
        </div>
        <button type="button" class="btn btn-sm btn-error btn-soft" [disabled]="!template" (click)="delete()">
          <svg lucideTrash class="size-4"></svg>{{ 'Templates.Delete' | translate }}
        </button>
      </div>

      @if (template) {
        <dl class="grid gap-x-6 gap-y-5 p-5 sm:grid-cols-2 sm:p-6">
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Templates.TitleLabel' | translate }}
              <ng-container *ngTemplateOutlet="edit; context: { $implicit: 'name' }" />
            </dt>
            <dd class="font-medium">{{ template.name || '—' }}</dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Templates.AliasLabel' | translate }}
              <ng-container *ngTemplateOutlet="edit; context: { $implicit: 'name' }" />
            </dt>
            <dd class="font-medium">
              @if (template.alias) { <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ template.alias }}</code> } @else { — }
            </dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Templates.KindLabel' | translate }}</dt>
            <dd class="mt-0.5"><span class="badge badge-sm" [class]="typeBadge(template.type)">{{ typeLabel(template.type) | translate }}</span></dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Templates.MessageTypeLabel' | translate }}
              <ng-container *ngTemplateOutlet="edit; context: { $implicit: 'messageType' }" />
            </dt>
            <dd class="mt-0.5"><app-message-type-badge [name]="template.messageType?.name" [classification]="template.messageType?.classification" /></dd>
          </div>
          <div>
            <dt class="flex items-center gap-1 text-xs text-base-content/60">
              {{ 'Templates.IgnoreUserPreferencesLabel' | translate }}
              <ng-container *ngTemplateOutlet="edit; context: { $implicit: 'preferences' }" />
            </dt>
            <dd class="mt-0.5 flex items-center gap-1.5 font-medium">
              <app-bool-icon [value]="template.ignoreUserPreferences" />
              {{ (template.ignoreUserPreferences ? 'Templates.Yes' : 'Templates.No') | translate }}
            </dd>
          </div>
          <div class="hidden sm:block"></div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Templates.CreatedByLabel' | translate }}</dt>
            <dd class="font-medium">{{ template.createdBy || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Templates.CreatedAtLabel' | translate }}</dt>
            <dd class="font-medium">{{ template.createdAt ? (template.createdAt | date: 'medium') : '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Templates.UpdatedByLabel' | translate }}</dt>
            <dd class="font-medium">{{ template.updatedBy || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Templates.UpdatedAtLabel' | translate }}</dt>
            <dd class="font-medium">{{ template.updatedAt ? (template.updatedAt | date: 'medium') : '—' }}</dd>
          </div>
        </dl>
      } @else {
        <div class="grid gap-5 p-5 sm:grid-cols-2 sm:p-6">
          @for (row of [1, 2, 3, 4, 5, 6]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      }
    </section>

    <ng-template #edit let-section>
      <a class="btn btn-ghost btn-xs btn-square -my-1" [routerLink]="editLink(section)" [attr.aria-label]="'Templates.EditTemplateTitle' | translate"
        [title]="'Templates.EditTemplateTitle' | translate">
        <svg lucidePencil class="size-3.5"></svg>
      </a>
    </ng-template>
  `
})
export class TemplateDetailsEditComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _router = inject(Router);
  private readonly _pane = inject(SidePaneService);
  protected readonly store = inject(TemplateEditStore);

  protected readonly typeLabel = templateTypeLabel;
  protected readonly typeBadge = templateTypeBadge;

  protected editLink(section: TemplateEditSection): any[] {
    return this._pane.link(['edit-template', this.store.template()?.id ?? ''], { section });
  }

  protected delete(): void {
    const template = this.store.template();
    if (!template) {
      return;
    }
    this._dialog
      .confirm({ title: 'Templates.DeleteTemplateTitle', message: 'Templates.DeleteConfirmMessage', messageParams: { name: template.name }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._api.deleteTemplate(template.id!))
      )
      .subscribe(() => {
        this._toaster.show(ToastType.Success, 'Templates.DeleteSuccessTitle', 'Templates.DeleteSuccessMessage', undefined, { name: template.name });
        this.store.template.set(undefined);
        this._router.navigate(['templates']);
      });
  }
}
