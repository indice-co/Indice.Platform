import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, ToastType, ViewAction } from '@indice/ng-components';
import { Observable, Subject, combineLatest } from 'rxjs';
import { map, take, takeUntil } from 'rxjs/operators';
import { DistributionList, DistributionListResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';

@Component({
  selector: 'app-distribution-lists',
  templateUrl: './distribution-lists.component.html'
})
export class DistributionListsComponent extends BaseListComponent<DistributionList> implements OnInit, OnDestroy {
  constructor(
    route: ActivatedRoute,
    private _router: Router,
    private _api: MessagesApiClient,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster,
    private _modalService: ModalService,
    private _lang: AppLanguagesService
  ) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'name';
    this.sortdir = 'asc';
    this.search = '';
    // Fallback: use translation key as initial label.
    this.sortOptions = [new MenuOption('DistributionLists.SortNameOption', 'name')];
  }

  public newItemLink: string | null = 'create-distribution-list';
  public full = true;

  private _isSystemGeneratedFilter = false;
  private _destroy$ = new Subject<void>();

  public override ngOnInit(): void {
    super.ngOnInit();
    // Reactive translation for sort options.
    const keys = this.sortOptions.map(o => o.text);
    combineLatest(keys.map(k => this._lang.translateKey(k)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.sortOptions = this.sortOptions.map((o, i) => new MenuOption(translated[i] || o.text, o.value));
      });
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();

  }

  public loadItems(): Observable<IResultSet<DistributionList> | null | undefined> {
    return this._api
      .getDistributionLists(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, this._isSystemGeneratedFilter)
      .pipe(map((result: DistributionListResultSet) => (result as IResultSet<DistributionList>)));
  }

  public deleteConfirmation(list: DistributionList): void {
    const titleKey = 'DistributionLists.Delete'; // existing key (button label) reused as modal title
    const messageKey = 'DistributionLists.DeleteConfirmMessage';
    combineLatest([
      this._lang.translateKey(titleKey),
      this._lang.translateKey(messageKey, { name: list.name })
    ]).pipe(take(1)).subscribe(([title, message]) => {
      const modal = this._modalService.show(BasicModalComponent, {
        animated: true,
        initialState: {
          title: title || titleKey,
          message: message || messageKey,
          data: list
        },
        keyboard: true
      });
      modal.onHidden?.pipe(take(1)).subscribe((response: any) => {
        if (response.result?.answer) {
          this._api.deleteDistributionList(response.result.data.id).subscribe(() => {
            this._toaster.show(ToastType.Success, 'DistributionLists.DeleteSuccessTitle', 'DistributionLists.DeleteSuccessMessage', undefined, { name: response.result.data.name });
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
          });
        }
      });
    });
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }
}
