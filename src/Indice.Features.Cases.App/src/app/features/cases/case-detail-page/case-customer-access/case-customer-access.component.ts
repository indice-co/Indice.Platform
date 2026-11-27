import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucidePencil } from '@lucide/angular';
import { CaseCoOwner, ContactPermission } from 'src/app/core/services/cases-api.service';
import { contactPermissionClass, contactPermissionLabel } from '../../contact-permissions';

/** The customers that take part in a case, its owner and its co-owners, each with what they can do on it. */
@Component({
  selector: 'app-case-customer-access',
  imports: [RouterLink, TranslatePipe, LucidePencil],
  host: { class: 'block' },
  template: `
    <section class="panel p-5">
      <div class="mb-3 flex items-start justify-between gap-2">
        <h2 class="text-sm font-semibold">{{ 'customerAccess.title' | translate }}</h2>
        @if (editLink(); as link) {
          <a class="btn btn-ghost btn-xs btn-square -my-0.5 shrink-0" [routerLink]="link" queryParamsHandling="preserve"
            [title]="'common.edit' | translate" [attr.aria-label]="('common.edit' | translate) + ': ' + ('customerAccess.title' | translate)">
            <svg lucidePencil class="size-3.5"></svg>
          </a>
        }
      </div>
      <ul class="space-y-3 text-sm">
        @if (ownerName() || ownerId()) {
          <li class="flex items-start justify-between gap-3">
            <span class="min-w-0">
              <span class="block break-words font-medium">{{ ownerName() || ownerId() }}</span>
              <span class="block text-base-content/65">{{ 'customerAccess.owner' | translate }}</span>
            </span>
            <span class="badge badge-sm mt-0.5 shrink-0" [class]="permissionClass(ownerPermission())">{{ permissionLabel(ownerPermission()) | translate }}</span>
          </li>
        }
        @for (coOwner of coOwners(); track coOwner.userId) {
          <li class="flex items-start justify-between gap-3">
            <span class="min-w-0">
              <span class="block break-words font-medium">{{ coOwnerName(coOwner) }}</span>
              <span class="block text-base-content/65">{{ 'customerAccess.coOwner' | translate }}</span>
            </span>
            <span class="badge badge-sm mt-0.5 shrink-0" [class]="permissionClass(coOwner.permission)">{{ permissionLabel(coOwner.permission) | translate }}</span>
          </li>
        } @empty {
          <li class="text-base-content/65">{{ 'customerAccess.noCoOwners' | translate }}</li>
        }
      </ul>
    </section>
  `
})
export class CaseCustomerAccessComponent {
  public readonly ownerName = input<string>();
  public readonly ownerId = input<string>();
  public readonly ownerPermission = input<ContactPermission>();
  public readonly coOwners = input<CaseCoOwner[]>([]);
  /** Router link commands of the editor: adds a pen next to the title. */
  public readonly editLink = input<any[]>();

  protected readonly permissionLabel = contactPermissionLabel;
  protected readonly permissionClass = contactPermissionClass;

  protected coOwnerName(coOwner: CaseCoOwner): string {
    return [coOwner.firstName, coOwner.lastName].filter(Boolean).join(' ') || coOwner.userId || '';
  }
}
