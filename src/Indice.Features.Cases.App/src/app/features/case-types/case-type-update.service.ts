import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypeRequest, ICaseTypeRequest } from 'src/app/core/services/cases-api.service';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';

/** Creates and updates case types, keeping the cached case-type list (menu, filters) in sync. */
@Injectable({ providedIn: 'root' })
export class CaseTypeUpdateService {
  private readonly api = inject(CasesApiService);
  private readonly toaster = inject(ToastService);
  private readonly router = inject(Router);
  private readonly caseTypeService = inject(CaseTypeService);

  public onCreateSubmit(event: ICaseTypeRequest): void {
    this.api.createCaseType(this.toRequest(event)).pipe(
      tap(_ => {
        this.caseTypeService.invalidateCaseTypes();
        this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.caseTypeCreated.body');
        this.router.navigate(['/case-types']);
      }),
      catchError(err => {
        this.toaster.show(ToastType.Error, 'toasts.error.title', err.detail);
        return EMPTY;
      })
    ).subscribe();
  }

  public onEditSubmit(caseTypeId: string, event: ICaseTypeRequest): void {
    this.api.updateCaseType(caseTypeId, this.toRequest(event, caseTypeId)).pipe(
      tap(_ => {
        this.caseTypeService.invalidateCaseTypes();
        this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.caseTypeUpdated.body');
      }),
      catchError(err => {
        this.toaster.show(ToastType.Error, 'toasts.error.title', err.detail);
        return EMPTY;
      })
    ).subscribe();
  }

  private toRequest(event: ICaseTypeRequest, id?: string): CaseTypeRequest {
    return new CaseTypeRequest({
      id,
      title: event.title,
      order: event.order,
      description: event.description,
      code: event.code,
      dataSchema: typeof event.dataSchema === 'string' ? JSON.parse(event.dataSchema) : event.dataSchema,
      layout: typeof event.layout === 'string' ? JSON.parse(event.layout) : event.layout,
      translations: typeof event.translations === 'string' ? JSON.parse(event.translations) : event.translations,
      layoutTranslations: typeof event.layoutTranslations === 'string' ? JSON.parse(event.layoutTranslations) : event.layoutTranslations,
      tags: event?.tags,
      config: event?.config,
      canCreateRoles: event?.canCreateRoles,
      isMenuItem: event?.isMenuItem,
      gridFilterConfig: event?.gridFilterConfig,
      gridColumnConfig: event?.gridColumnConfig
    });
  }
}
