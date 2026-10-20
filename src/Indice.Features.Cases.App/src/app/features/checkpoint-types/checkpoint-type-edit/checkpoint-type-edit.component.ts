import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Observable } from 'rxjs';
import { catchError, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CaseStatus, CasesApiService, CaseTypePartial, CheckpointType, CheckpointTypeRequest } from 'src/app/core/services/cases-api.service';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';

/** Creates (`/checkpoint-types/create?caseTypeId=…`) or edits (`/checkpoint-types/:id/edit`) a checkpoint type. */
@Component({
  selector: 'app-checkpoint-type-edit',
  imports: [FormsModule, NgIcon, TranslatePipe, PageHeaderComponent],
  template: `
    <div class="page max-w-3xl">
      <app-page-header [title]="(isEdit ? 'checkpointTypes.edit.title' : 'checkpointTypes.create.title') | translate">
        <button eyebrow type="button" class="link link-hover mb-1 inline-flex items-center gap-1 text-[0.8125rem] text-quiet" (click)="goBack()">
          <ng-icon name="lucideArrowLeft" size="0.9rem" /> {{ 'checkpointTypes.title' | translate }}
        </button>
      </app-page-header>

      <form #form="ngForm" class="panel" [class.opacity-60]="busy()" [attr.aria-busy]="busy()" (ngSubmit)="form.valid && submit()">
        <div class="panel-body grid gap-x-5 gap-y-4 sm:grid-cols-2">
          <div class="sm:col-span-2">
            <label class="mb-2 block text-sm font-medium" for="caseTypeId">{{ 'checkpointTypes.column.caseType' | translate }} <span class="text-error">*</span></label>
            <select id="caseTypeId" name="caseTypeId" class="select w-full" required [(ngModel)]="model.caseTypeId">
              @for (caseType of caseTypes(); track caseType.id) {
                <option [ngValue]="caseType.id">{{ caseType.title }} ({{ caseType.code }})</option>
              }
            </select>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium" for="code">{{ 'checkpointTypes.column.code' | translate }} <span class="text-error">*</span></label>
            <input id="code" name="code" type="text" class="input ref w-full" required maxlength="64" [(ngModel)]="model.code" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium" for="title">{{ 'checkpointTypes.column.title' | translate }}</label>
            <input id="title" name="title" type="text" class="input w-full" [(ngModel)]="model.title" />
          </div>
          <div class="sm:col-span-2">
            <label class="mb-2 block text-sm font-medium" for="description">{{ 'checkpointTypes.column.description' | translate }}</label>
            <textarea id="description" name="description" rows="3" class="textarea w-full" [(ngModel)]="model.description"></textarea>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium" for="status">{{ 'checkpointTypes.column.status' | translate }}</label>
            <select id="status" name="status" class="select w-full" [(ngModel)]="model.status">
              @for (status of statuses; track status) {
                <option [ngValue]="status">{{ ('status.' + status) | translate }}</option>
              }
            </select>
          </div>
          <label class="flex cursor-pointer items-center gap-3 self-end pb-2.5" for="private">
            <input id="private" name="private" type="checkbox" class="toggle toggle-primary" [(ngModel)]="model.private" />
            <span class="inline-flex items-center gap-1.5 font-medium"><ng-icon name="lucideLock" size="0.95rem" class="text-quiet" /> {{ 'checkpointTypes.column.private' | translate }}</span>
          </label>
        </div>
        <div class="flex justify-end gap-2 border-t border-base-300 px-5 py-3">
          <button type="button" class="btn btn-ghost" (click)="goBack()">{{ 'common.return' | translate }}</button>
          <button type="submit" class="btn btn-primary" [disabled]="busy() || form.invalid">
            <ng-icon name="lucideSave" size="1rem" /> {{ 'common.save' | translate }}
          </button>
        </div>
      </form>
    </div>
  `
})
export class CheckpointTypeEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(CasesApiService);
  private readonly caseTypeService = inject(CaseTypeService);
  private readonly toaster = inject(ToastService);

  protected checkpointTypeId: string | undefined;
  protected model = new CheckpointTypeRequest({ caseTypeId: '', code: '', status: CaseStatus.Submitted, private: false });
  protected readonly caseTypes = signal<CaseTypePartial[]>([]);
  protected readonly statuses = Object.values(CaseStatus);
  protected readonly busy = signal(false);

  protected get isEdit(): boolean {
    return !!this.checkpointTypeId;
  }

  public ngOnInit(): void {
    this.caseTypeService.getCaseTypes().pipe(take(1)).subscribe((result: { items?: CaseTypePartial[] }) => this.caseTypes.set(result.items ?? []));
    this.checkpointTypeId = this.route.snapshot.paramMap.get('checkpointTypeId') ?? undefined;
    const caseTypeId = this.route.snapshot.queryParamMap.get('caseTypeId');
    if (caseTypeId) {
      this.model.caseTypeId = caseTypeId;
    }
    if (this.checkpointTypeId) {
      this.busy.set(true);
      this.api.getCheckpointTypeById(this.checkpointTypeId).pipe(
        catchError(err => {
          this.busy.set(false);
          this.toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title ?? err?.message);
          this.goBack();
          return EMPTY;
        })
      ).subscribe((item: CheckpointType) => {
        this.model = new CheckpointTypeRequest({
          caseTypeId: item.caseTypeId!,
          code: item.code!,
          title: item.title,
          description: item.description,
          status: item.status,
          private: item.private,
          translations: item.translations
        });
        // Setting the signal also re-renders the form with the loaded model.
        this.busy.set(false);
      });
    }
  }

  protected submit(): void {
    this.busy.set(true);
    const request$: Observable<CheckpointType> = this.isEdit
      ? this.api.updateCheckpointType(this.checkpointTypeId!, this.model)
      : this.api.createCheckpointType(this.model);
    request$.pipe(
      catchError(err => {
        this.busy.set(false);
        this.toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
        return EMPTY;
      })
    ).subscribe(_ => {
      this.busy.set(false);
      this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.checkpointTypeSaved.body');
      this.goBack();
    });
  }

  protected goBack(): void {
    this.router.navigate(['/checkpoint-types'], { queryParams: this.model.caseTypeId ? { caseTypeId: this.model.caseTypeId } : {} });
  }
}
