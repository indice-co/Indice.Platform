import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, firstValueFrom } from 'rxjs';
import { LucideTriangleAlert, LucideUserMinus, LucideUserPlus } from '@lucide/angular';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import {
  AssignCoOwnerRequest, CaseCoOwner, CasesApiService, Contact, ContactMeta, ContactPermission, UpdateCaseOwnerPermissionRequest
} from 'src/app/core/services/cases-api.service';
import { SearchContactComponent } from 'src/app/shared/components/search-contact/search-contact.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { SidePaneLayoutComponent } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { CONTACT_PERMISSIONS } from '../contact-permissions';

/**
 * Side pane (administrators): what the customers of a case can do on it. Sets the permission of the owner, and adds,
 * changes and removes the co-owners.
 */
@Component({
  selector: 'app-case-customer-access-pane',
  imports: [
    DatePipe, TranslatePipe, EmptyStateComponent, SearchContactComponent, SidePaneLayoutComponent,
    LucideTriangleAlert, LucideUserMinus, LucideUserPlus
  ],
  template: `
    <app-side-pane-layout [title]="'customerAccess.title' | translate" [subtitle]="'customerAccess.subtitle' | translate">
      @if (case(); as caseModel) {
        <div class="space-y-8">
          <dl class="space-y-1.5 rounded-box bg-base-200 px-4 py-3 text-sm">
            @for (option of permissions; track option.value) {
              <div class="grid grid-cols-[7rem_minmax(0,1fr)] gap-3">
                <dt class="font-medium">{{ option.label | translate }}</dt>
                <dd class="text-base-content/70">{{ option.hint | translate }}</dd>
              </div>
            }
          </dl>

          @if (caseModel.ownerName || caseModel.ownerId) {
            <section>
              <h3 class="mb-3 font-semibold">{{ 'customerAccess.owner' | translate }}</h3>
              <div class="space-y-2.5 rounded-box border border-base-300 px-4 py-3">
                <div class="min-w-0">
                  <span class="block break-words font-medium">{{ caseModel.ownerName || caseModel.ownerId }}</span>
                  @if (caseModel.ownerName && caseModel.ownerId) {
                    <span class="docket block break-all text-sm text-base-content/70">{{ caseModel.ownerId }}</span>
                  }
                </div>
                <div class="flex items-center gap-2">
                  <select class="select select-sm w-44" [disabled]="locked()"
                    [attr.aria-label]="'customerAccess.permissionOf' | translate: { name: caseModel.ownerName || caseModel.ownerId }"
                    (change)="setOwnerPermission($any($event.target))">
                    @for (option of permissions; track option.value) {
                      <option [value]="option.value" [selected]="option.value === ownerPermission()">{{ option.label | translate }}</option>
                    }
                  </select>
                  @if (busy() === 'owner') { <span class="loading loading-spinner loading-xs"></span> }
                </div>
              </div>
            </section>
          }

          <section>
            <h3 class="mb-3 flex items-center gap-2 font-semibold">
              {{ 'customerAccess.coOwners' | translate }}
              <span class="badge badge-sm badge-ghost">{{ coOwners().length }}</span>
            </h3>
            @if (coOwners().length) {
              <ul class="divide-y divide-base-300 rounded-box border border-base-300">
                @for (coOwner of coOwners(); track coOwner.userId) {
                  <li class="space-y-2.5 py-3 pl-4 pr-2">
                    <div class="min-w-0 pr-2">
                      <span class="block break-words font-medium">{{ name(coOwner) }}</span>
                      <span class="docket block break-all text-sm text-base-content/70">{{ coOwner.userId }}</span>
                      @if (coOwner.tin) {
                        <span class="block text-sm text-base-content/70">{{ 'cases.searchContact.taxId' | translate }}: <span class="docket">{{ coOwner.tin }}</span></span>
                      }
                      @if (coOwner.addedBy; as addedBy) {
                        <span class="mt-0.5 block text-xs text-base-content/60">
                          {{ 'customerAccess.addedBy' | translate: { name: addedBy.name || addedBy.email || addedBy.id } }}
                          @if (addedBy.when) { · {{ addedBy.when | date: 'dd/MM/yyyy, HH:mm' }} }
                        </span>
                      }
                    </div>
                    <div class="flex items-center gap-2">
                      <select class="select select-sm w-44" [disabled]="locked()"
                        [attr.aria-label]="'customerAccess.permissionOf' | translate: { name: name(coOwner) }"
                        (change)="setCoOwnerPermission(coOwner, $any($event.target))">
                        @for (option of permissions; track option.value) {
                          <option [value]="option.value" [selected]="option.value === permissionOf(coOwner)">{{ option.label | translate }}</option>
                        }
                      </select>
                      @if (busy() === coOwner.userId) { <span class="loading loading-spinner loading-xs"></span> }
                      <button type="button" class="btn btn-ghost btn-sm ml-auto shrink-0 text-error" [disabled]="locked()" (click)="remove(coOwner)"
                        [attr.aria-label]="('customerAccess.remove' | translate) + ': ' + name(coOwner)">
                        <svg lucideUserMinus class="size-4"></svg>
                        {{ 'customerAccess.removeAction' | translate }}
                      </button>
                    </div>
                  </li>
                }
              </ul>
            } @else {
              <p class="rounded-box bg-base-200 px-4 py-3 text-sm text-base-content/70">{{ 'customerAccess.noCoOwners' | translate }}</p>
            }
          </section>

          <section>
            <h3 class="mb-1 font-semibold">{{ 'customerAccess.add.title' | translate }}</h3>
            <p class="mb-3 text-sm text-base-content/70">{{ 'customerAccess.add.hint' | translate }}</p>
            <app-search-contact [caseTypeCode]="caseModel.caseType?.code" (selectedContact)="contact.set($event)" />
            @if (contact()) {
              @if (addBlocked(); as reason) {
                <p role="alert" class="mt-4 rounded-box bg-warning/15 px-4 py-3 text-sm">{{ reason | translate }}</p>
              } @else {
                <div class="mt-4 flex flex-wrap items-end justify-end gap-3">
                  <label class="flex min-w-0 flex-1 basis-40 flex-col gap-1 text-sm">
                    <span class="text-base-content/70">{{ 'customerAccess.permission' | translate }}</span>
                    <select class="select w-full" [disabled]="locked()" (change)="newPermission.set($any($event.target).value)">
                      @for (option of permissions; track option.value) {
                        <option [value]="option.value" [selected]="option.value === newPermission()">{{ option.label | translate }}</option>
                      }
                    </select>
                  </label>
                  <button type="button" class="btn btn-primary" [disabled]="locked()" (click)="add()">
                    @if (busy() === 'add') { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideUserPlus class="size-4"></svg> }
                    {{ 'customerAccess.add.action' | translate }}
                  </button>
                </div>
              }
            }
          </section>
        </div>
      } @else if (caseResource.error()) {
        <app-empty-state [title]="'caseDetails.loadError.title' | translate" [message]="'caseDetails.loadError.message' | translate">
          <svg icon lucideTriangleAlert class="size-6"></svg>
          <button actions type="button" class="btn btn-sm" (click)="caseResource.reload()">{{ 'common.retry' | translate }}</button>
        </app-empty-state>
      } @else {
        <div class="space-y-3">
          @for (i of [1, 2, 3]; track i) { <div class="skeleton h-11 w-full"></div> }
        </div>
      }
    </app-side-pane-layout>
  `
})
export class CaseCustomerAccessPaneComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _caseDetails = inject(CaseDetailsService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);

  /** The case whose customer access is edited (`case-customer-access;caseId=X`). */
  public readonly caseId = input.required<string>();

  protected readonly permissions = CONTACT_PERMISSIONS;
  protected readonly caseResource = rxResource({
    params: () => this.caseId(),
    stream: ({ params }) => this._api.getCaseById(params)
  });
  protected readonly case = computed(() => this.caseResource.hasValue() ? this.caseResource.value() : undefined);
  protected readonly ownerPermission = computed(() => this.case()?.ownerPermission ?? ContactPermission.Reader);
  protected readonly coOwners = computed(() => this.case()?.coOwners ?? []);

  /** The contact picked to become a co-owner. */
  protected readonly contact = linkedSignal<string, Contact | undefined>({ source: this.caseId, computation: () => undefined });
  protected readonly newPermission = linkedSignal<string, ContactPermission>({ source: this.caseId, computation: () => ContactPermission.Reader });
  /** Translation key of why the picked contact cannot be added, if it cannot. */
  protected readonly addBlocked = computed(() => {
    const userId = this.contact()?.userId;
    if (!userId) {
      return 'customerAccess.add.noUserId';
    }
    // `ownerId` is the customer reference of the owner; `userId` is the user to compare with
    if (sameUser(userId, this.case()?.userId)) {
      return 'customerAccess.add.isOwner';
    }
    return this.coOwners().some(coOwner => sameUser(userId, coOwner.userId)) ? 'customerAccess.add.alreadyCoOwner' : undefined;
  });

  /** What is being saved: 'owner', 'add', or the user id of the co-owner being changed or removed. */
  protected readonly busy = signal<string | undefined>(undefined);
  /** No new change while one is saved or the pane reloads after it. */
  protected readonly locked = computed(() => !!this.busy() || this.caseResource.isLoading());

  protected name(coOwner: CaseCoOwner): string {
    return [coOwner.firstName, coOwner.lastName].filter(Boolean).join(' ') || coOwner.userId || '';
  }

  protected permissionOf(coOwner: CaseCoOwner): ContactPermission {
    return coOwner.permission ?? ContactPermission.Reader;
  }

  protected async setOwnerPermission(select: HTMLSelectElement): Promise<void> {
    const caseId = this.caseId();
    const permission = select.value as ContactPermission;
    const saved = await this.save('owner', caseId, this._api.updateCaseOwnerPermission(caseId, new UpdateCaseOwnerPermissionRequest({ permission })));
    if (!saved) {
      select.value = this.ownerPermission();
    }
  }

  protected async setCoOwnerPermission(coOwner: CaseCoOwner, select: HTMLSelectElement): Promise<void> {
    const saved = await this.assign(coOwner.userId!, coOwner, select.value as ContactPermission);
    if (!saved) {
      select.value = this.permissionOf(coOwner);
    }
  }

  protected async add(): Promise<void> {
    const contact = this.contact();
    if (!contact?.userId || this.addBlocked()) {
      return;
    }
    await this.assign('add', contact, this.newPermission());
  }

  protected async remove(coOwner: CaseCoOwner): Promise<void> {
    const caseId = this.caseId();
    const confirmed = await firstValueFrom(this._dialog.confirm({
      title: 'customerAccess.remove',
      message: 'customerAccess.removeConfirm',
      messageParams: { name: this.name(coOwner) },
      confirmLabel: 'customerAccess.remove',
      tone: 'danger'
    }), { defaultValue: false });
    if (confirmed) {
      await this.save(coOwner.userId!, caseId, this._api.removeCoOwnerFromCase(caseId, coOwner.userId!));
    }
  }

  /** Adds the contact as a co-owner, or updates the permission of that co-owner: the server tells the two apart by the user id. */
  private assign(what: string, contact: Contact | CaseCoOwner, permission: ContactPermission): Promise<boolean> {
    const caseId = this.caseId();
    const coOwner = new ContactMeta({
      userId: contact.userId,
      reference: contact.reference,
      firstName: contact.firstName,
      lastName: contact.lastName,
      tin: contact.tin,
      permission
    });
    return this.save(what, caseId, this._api.assignCoOwner(caseId, new AssignCoOwnerRequest({ coOwner })));
  }

  /** Saves a change, then reloads the pane and tells the details page. Answers whether it was saved. */
  private async save(what: string, caseId: string, change: Observable<void>): Promise<boolean> {
    this.busy.set(what);
    try {
      await firstValueFrom(change);
      this._toast.success('toasts.success.title', 'customerAccess.saved');
      this._caseDetails.notifyCaseChanged(caseId);
      this.caseResource.reload();
      return true;
    } catch {
      // the problem details interceptor reports the failure
      return false;
    } finally {
      this.busy.set(undefined);
    }
  }
}

/** User ids are compared ignoring case, as the server does. */
function sameUser(a: string | undefined, b: string | undefined): boolean {
  return !!a && !!b && a.toLowerCase() === b.toLowerCase();
}
