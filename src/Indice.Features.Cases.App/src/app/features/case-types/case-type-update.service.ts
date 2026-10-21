import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY, Observable } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypeRequest, CaseTypeTranslation, ICaseTypeRequest } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';

/** Create / update calls for case types, with cache invalidation and user feedback. */
@Injectable({ providedIn: 'root' })
export class CaseTypeUpdateService {
  private readonly _api = inject(CasesApiService);
  private readonly _toaster = inject(TranslatedToasterService);
  private readonly _router = inject(Router);
  private readonly _caseTypeService = inject(CaseTypeService);

  public onCreateSubmit(event: ICaseTypeRequest): Observable<unknown> {
    const request = new CaseTypeRequest(this.toRequest(event));
    return this._api.createCaseType(request).pipe(
      tap(() => {
        this._caseTypeService.invalidateCaseTypes();
        this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.caseTypeCreated.body');
        this._router.navigate(['/case-types']);
      }),
      catchError(() => EMPTY)
    );
  }

  public onEditSubmit(caseTypeId: string, event: ICaseTypeRequest): Observable<unknown> {
    const request = new CaseTypeRequest({ ...this.toRequest(event), id: caseTypeId });
    return this._api.updateCaseType(caseTypeId, request).pipe(
      tap(() => {
        this._caseTypeService.invalidateCaseTypes();
        this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.caseTypeUpdated.body');
      }),
      catchError(() => EMPTY)
    );
  }

  private toRequest(event: ICaseTypeRequest): ICaseTypeRequest {
    const parse = (value: any) => typeof value === 'string' ? JSON.parse(value) : value;
    // the generated CaseTypeRequest.toJSON() expects CaseTypeTranslation instances, not plain objects
    const translations = parse(event.translations) as Record<string, unknown> | undefined;
    return {
      title: event.title,
      order: event.order,
      description: event.description,
      code: event.code,
      dataSchema: parse(event.dataSchema),
      layout: parse(event.layout),
      translations: translations
        ? Object.fromEntries(Object.entries(translations).map(([lang, value]) => [lang, value instanceof CaseTypeTranslation ? value : CaseTypeTranslation.fromJS(value)]))
        : undefined,
      layoutTranslations: parse(event.layoutTranslations),
      tags: event.tags,
      config: event.config,
      canCreateRoles: event.canCreateRoles,
      isMenuItem: event.isMenuItem,
      gridFilterConfig: event.gridFilterConfig,
      gridColumnConfig: event.gridColumnConfig
    };
  }
}
