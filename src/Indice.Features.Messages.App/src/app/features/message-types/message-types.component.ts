import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, ToastType, ViewAction } from '@indice/ng-components';
import { Observable, Subject, combineLatest } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { MessagesApiClient, MessageType, MessageTypeResultSet } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';

@Component({
  selector: 'app-message-types',
  templateUrl: './message-types.component.html'
})
export class MessageTypesComponent extends BaseListComponent<MessageType> implements OnInit, OnDestroy {
  constructor(
    route: ActivatedRoute,
    private _router: Router,
    private _api: MessagesApiClient,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster,
    private _modalService: ModalService,
    private _languages: AppLanguagesService
  ) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'name';
    this.sortdir = 'asc';
    this.search = '';
    // Fallback uses translation key as initial label.
    this.sortOptions = [new MenuOption('MessageTypes.SortNameOption', 'name')];
  }

  private _destroy$ = new Subject<void>();

  public newItemLink: string | null = 'create-message-type';
  public full = true;

  public override ngOnInit(): void {
    super.ngOnInit();
    // Reactive translation of sort options.
    const sortKeys = this.sortOptions.map(o => o.text);
    combineLatest(sortKeys.map(k => this._languages.translateKey(k)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.sortOptions = this.sortOptions.map((o, i) => new MenuOption(translated[i] || o.text, o.value));
      });
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public loadItems(): Observable<IResultSet<MessageType> | null | undefined> {
    return this._api
      .getMessageTypes(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
      .pipe(map((result: MessageTypeResultSet) => (result as IResultSet<MessageType>)));
  }

  public deleteConfirmation(type: MessageType): void {
    const titleKey = 'MessageTypes.Delete'; // reuse existing 'Delete' label if present; otherwise add key separately if needed
    const messageKey = 'MessageTypes.DeleteConfirmMessage';
    const params = { name: type.name };
    combineLatest([
      this._languages.translateKey(titleKey),
      this._languages.translateKey(messageKey, params)
    ])
      .pipe(takeUntil(this._destroy$))
      .subscribe(([title, message]) => {
        const modal = this._modalService.show(BasicModalComponent, {
          animated: true,
          initialState: {
            title: title || titleKey,
            message: message || messageKey,
            data: type
          },
          keyboard: true
        });
        modal.onHidden?.pipe(takeUntil(this._destroy$)).subscribe((response: any) => {
          if (response.result?.answer) {
            this._api.deleteMessageType(response.result.data.id).subscribe(() => {
              this._toaster.show(ToastType.Success, 'MessageTypes.DeleteSuccessTitle', 'MessageTypes.DeleteSuccessMessage', undefined, { name: response.result.data.name });
              this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['message-types']));
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
