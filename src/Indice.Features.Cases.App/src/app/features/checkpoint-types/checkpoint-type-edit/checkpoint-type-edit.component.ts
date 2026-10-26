import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink, UrlTree } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Observable, catchError, finalize, take } from 'rxjs';
import { LucideSave } from '@lucide/angular';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CaseStatus, CasesApiService, CaseTypePartial, CheckpointType, CheckpointTypeRequest } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

/** Create (no `:checkpointTypeId`) or edit a checkpoint type. A `caseTypeId` query param pre-selects the case type. */
@Component({
  selector: 'app-checkpoint-type-edit',
  imports: [RouterLink, TranslatePipe, PageHeaderComponent, LucideSave],
  template: `
    <app-page-header [title]="(isEdit() ? 'checkpointTypes.edit.title' : 'checkpointTypes.create.title') | translate" />

    @if (loading()) {
      <div class="panel max-w-3xl space-y-4 p-6">
        <div class="skeleton h-10 w-full"></div>
        <div class="skeleton h-10 w-full"></div>
        <div class="skeleton h-24 w-full"></div>
      </div>
    } @else {
      <form class="panel max-w-3xl" novalidate (submit)="$event.preventDefault(); submit()">
        <fieldset class="fieldset grid gap-x-6 gap-y-3 p-5 sm:p-6 md:grid-cols-2">
          <label class="md:col-span-2">
            <span class="label mb-1">{{ 'checkpointTypes.column.caseType' | translate }} <span class="text-error">*</span></span>
            <select class="select w-full" required [value]="model().caseTypeId ?? ''" (change)="patch({ caseTypeId: value($event) })">
              <option value="" disabled>{{ 'checkpointTypes.chooseCaseType' | translate }}</option>
              @for (caseType of caseTypes(); track caseType.id) {
                <option [value]="caseType.id" [selected]="caseType.id === model().caseTypeId">{{ caseType.title }} ({{ caseType.code }})</option>
              }
            </select>
          </label>
          <label>
            <span class="label mb-1">{{ 'checkpointTypes.column.code' | translate }} <span class="text-error">*</span></span>
            <input class="input w-full font-mono" type="text" required maxlength="64" [value]="model().code ?? ''" (input)="patch({ code: value($event) })" />
          </label>
          <label>
            <span class="label mb-1">{{ 'checkpointTypes.column.title' | translate }}</span>
            <input class="input w-full" type="text" [value]="model().title ?? ''" (input)="patch({ title: value($event) })" />
          </label>
          <label class="md:col-span-2">
            <span class="label mb-1">{{ 'checkpointTypes.column.description' | translate }}</span>
            <textarea class="textarea w-full" rows="3" [value]="model().description ?? ''" (input)="patch({ description: value($event) })"></textarea>
          </label>
          <label>
            <span class="label mb-1">{{ 'checkpointTypes.column.status' | translate }}</span>
            <select class="select w-full" [value]="model().status" (change)="patch({ status: $any(value($event)) })">
              @for (status of statuses; track status) {
                <option [value]="status" [selected]="status === model().status">{{ status }}</option>
              }
            </select>
          </label>
          <label class="label cursor-pointer gap-3 self-end pb-2 text-base-content">
            <input type="checkbox" class="toggle toggle-primary toggle-sm" [checked]="!!model().private" (change)="patch({ private: $any($event.target).checked })" />
            <span>
              <span class="block font-medium">{{ 'checkpointTypes.column.private' | translate }}</span>
              <span class="block text-xs text-base-content/60">{{ 'checkpointTypes.privateHint' | translate }}</span>
            </span>
          </label>
        </fieldset>
        <div class="flex justify-end gap-2 border-t border-base-300 px-5 py-3 sm:px-6">
          <a class="btn btn-ghost" [routerLink]="backLink()">{{ 'common.cancel' | translate }}</a>
          <button type="submit" class="btn btn-primary" [disabled]="!canSave()">
            @if (busy()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideSave class="size-4"></svg> }
            {{ 'common.save' | translate }}
          </button>
        </div>
      </form>
    }
  `
})
export class CheckpointTypeEditComponent implements OnInit {
  private readonly _router = inject(Router);
  private readonly _api = inject(CasesApiService);
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _toaster = inject(TranslatedToasterService);

  /** Bound from the `:checkpointTypeId` route param (edit mode). */
  public readonly checkpointTypeId = input<string | undefined>(undefined);
  /** Bound from the `caseTypeId` query param (create mode pre-selection). */
  public readonly caseTypeId = input<string | undefined>(undefined);
  /** Bound from the `returnUrl` query param: the app URL to go back to (e.g. the case type's checkpoints), instead of the list. */
  public readonly returnUrl = input<string | undefined>(undefined);

  protected readonly statuses = Object.values(CaseStatus);
  protected readonly caseTypes = signal<CaseTypePartial[]>([]);
  protected readonly model = signal(new CheckpointTypeRequest({ caseTypeId: '', code: '', status: CaseStatus.Submitted, private: false }));
  protected readonly busy = signal(false);
  protected readonly loading = signal(false);
  protected readonly isEdit = computed(() => !!this.checkpointTypeId());
  protected readonly canSave = computed(() => !this.busy() && !!this.model().caseTypeId && !!this.model().code?.trim());
  /** Only app-relative return URLs are followed. */
  protected readonly backLink = computed<UrlTree>(() => {
    const returnUrl = this.returnUrl();
    return returnUrl?.startsWith('/') && !returnUrl.startsWith('//')
      ? this._router.parseUrl(returnUrl)
      : this._router.createUrlTree(['/checkpoint-types'], { queryParams: this.backQueryParams() });
  });

  public ngOnInit(): void {
    this._caseTypeService.getCaseTypes().pipe(take(1)).subscribe(result => this.caseTypes.set(result.items ?? []));
    if (this.caseTypeId()) {
      this.patch({ caseTypeId: this.caseTypeId()! });
    }
    const id = this.checkpointTypeId();
    if (id) {
      this.loading.set(true);
      this._api.getCheckpointTypeById(id).pipe(
        catchError(err => {
          this._toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title ?? err?.message);
          this.goBack();
          return EMPTY;
        }),
        finalize(() => this.loading.set(false))
      ).subscribe((item: CheckpointType) => {
        this.model.set(new CheckpointTypeRequest({
          caseTypeId: item.caseTypeId!,
          code: item.code!,
          title: item.title,
          description: item.description,
          status: item.status,
          private: item.private,
          translations: item.translations
        }));
      });
    }
  }

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected patch(changes: Partial<CheckpointTypeRequest>): void {
    this.model.update(model => new CheckpointTypeRequest({ ...model, ...changes }));
  }

  protected backQueryParams(): Record<string, string> {
    const caseTypeId = this.model().caseTypeId;
    return caseTypeId ? { caseTypeId } : {};
  }

  protected submit(): void {
    if (!this.canSave()) {
      return;
    }
    this.busy.set(true);
    const request$: Observable<CheckpointType> = this.isEdit()
      ? this._api.updateCheckpointType(this.checkpointTypeId()!, this.model())
      : this._api.createCheckpointType(this.model());
    request$.pipe(
      catchError(() => EMPTY),
      finalize(() => this.busy.set(false))
    ).subscribe(() => {
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.checkpointTypeSaved.body');
      this.goBack();
    });
  }

  private goBack(): void {
    this._router.navigateByUrl(this.backLink());
  }
}
