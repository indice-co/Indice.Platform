import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { APP_LANGUAGES, ModalService, ToastType } from '@indice/ng-components';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { MessagesApiClient, Template } from 'src/app/core/services/messages-api.service';
import { TemplateEditStore } from '../template-edit-store.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';
import { Subject, combineLatest } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
    selector: 'app-campaign-details-edit',
    templateUrl: './template-edit-details.component.html',
    standalone: false
})
export class TemplateDetailsEditComponent implements OnInit, OnDestroy {
  private _templateId: string | undefined;

  constructor(
    private _modalService: ModalService,
    private _api: MessagesApiClient,
    private _templateStore: TemplateEditStore,
    private _router: Router,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster,
    private _activatedRoute: ActivatedRoute,
    @Inject(APP_LANGUAGES) private _lang: AppLanguagesService
  ) { }

  public template: Template | undefined;
  private _destroy$ = new Subject<void>();

  public ngOnInit(): void {
    this._templateId = this._activatedRoute.parent?.snapshot.params['templateId'];
    if (this._templateId) {
      this._templateStore.getTemplate(this._templateId!).pipe(takeUntil(this._destroy$)).subscribe((template: Template) => {
        this.template = template;
      });
    }
  }

  public ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public deleteTemplate(): void {
    const titleKey = 'Templates.DeleteTemplateTitle'; // existing label (keep)
    const messageKey = 'Templates.DeleteConfirmMessage';
    const params = { name: this.template?.name };
    combineLatest([
      this._lang.translateKey(titleKey),
      this._lang.translateKey(messageKey, params)
    ]).pipe(takeUntil(this._destroy$))
      .subscribe(([title, message]) => {
        const modal = this._modalService.show(BasicModalComponent, {
          animated: true,
          initialState: {
            title: title || titleKey,
            message: message || messageKey,
            data: this.template
          },
          keyboard: true
        });
        modal.onHidden?.pipe(takeUntil(this._destroy$)).subscribe((response: any) => {
          if (response.result?.answer) {
            this._api.deleteTemplate(response.result.data.id).pipe(takeUntil(this._destroy$)).subscribe(() => {
              this._toaster.show(ToastType.Success, 'Templates.DeleteSuccessTitle', 'Templates.DeleteSuccessMessage', undefined, { name: response.result.data.name });
              this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates']));
            });
          }
        });
      });
  }

  public openEditPane(action: string): void {
    this._router.navigate(['', { outlets: { rightpane: ['edit-template'] } }], { queryParams: { action: action } });
  }
}
