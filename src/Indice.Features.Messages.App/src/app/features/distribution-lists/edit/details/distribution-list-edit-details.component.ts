import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalService, ToastType } from '@indice/ng-components';

import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { DistributionList, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { DistributionListEditStore } from '../distribution-list-edit-store.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';
import { combineLatest, takeUntil, Subject, Subscription } from 'rxjs';

@Component({
  selector: 'app-distribution-list-details-edit',
  templateUrl: './distribution-list-edit-details.component.html'
})
export class DistributionListDetailsEditComponent implements OnInit, OnDestroy {
  private _distributionListId: string | undefined;

  constructor(
    private _modalService: ModalService,
    private _api: MessagesApiClient,
    private _distributionListStore: DistributionListEditStore,
    private _router: Router,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster,
    private _activatedRoute: ActivatedRoute,
    private _languages: AppLanguagesService
  ) { }

  public list: DistributionList | undefined;

  // Added for takeUntil cleanup.
  private _destroy$ = new Subject<void>();
  private _modalHiddenSub?: Subscription;

  public ngOnInit(): void {
    this._distributionListId = this._activatedRoute.parent?.snapshot.params['distributionListId'];
    if (this._distributionListId) {
      this._distributionListStore.getDistributionList(this._distributionListId!).subscribe((list: DistributionList) => {
        this.list = list;
      });
    }
  }

  public ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
    this._modalHiddenSub?.unsubscribe();
  }

  public deleteTemplate(): void {
    const titleKey = 'DistributionLists.Delete';
    const messageKey = 'DistributionLists.DeleteConfirmMessage';
    const nameParam = { name: this.list?.name };
    combineLatest([
      this._languages.translateKey(titleKey),
      this._languages.translateKey(messageKey, nameParam)
    ])
      .pipe(takeUntil(this._destroy$))
      .subscribe(([title, message]) => {
        const modal = this._modalService.show(BasicModalComponent, {
          animated: true,
          initialState: {
            title: title || titleKey,
            message: message || messageKey,
            data: this.list
          },
          keyboard: true
        });
        this._modalHiddenSub = modal.onHidden?.subscribe((response: any) => {
          if (response.result?.answer) {
            this._api.deleteDistributionList(response.result.data.id).subscribe(() => {
              this._toaster.show(ToastType.Success, 'DistributionLists.DeleteSuccessTitle', 'DistributionLists.DeleteSuccessMessage', undefined, { name: response.result.data.name });
              this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
            });
          }
        });
      });
  }

  public openEditPane(action: string): void {
    this._router.navigate(['', { outlets: { rightpane: ['edit-distribution-list'] } }], { queryParams: { action: action } });
  }
}
