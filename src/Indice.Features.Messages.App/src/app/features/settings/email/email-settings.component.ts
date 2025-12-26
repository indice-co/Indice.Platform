import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, IResultSet, ListViewType, MenuOption, ModalService, ToastType } from '@indice/ng-components';
import { Observable, Subject, combineLatest } from 'rxjs';
import { map, tap, takeUntil } from 'rxjs/operators';
import { MessageSender, MessageSenderResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';
import { TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-email-settings',
    templateUrl: './email-settings.component.html',
    standalone: false
})
export class EmailSettingsComponent extends BaseListComponent<MessageSender> implements OnInit, OnDestroy {

  constructor(
    route: ActivatedRoute,
    private _router: Router,
    private _api: MessagesApiClient,
    private _toaster: AppTranslatedToaster,
    private _modalService: ModalService,
    private _translate: TranslateService
  ) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'isDefault';
    this.sortdir = 'desc';
    this.search = '';
  }

  public newItemLink: string | null = 'settings';
  public defaultSender: MessageSender | undefined;
  private _destroy$ = new Subject<void>();

  public override ngOnInit(): void {
    super.ngOnInit();
    // Load localized sort options dynamically
    combineLatest([
      this._translate.get('Settings.SortSenderOption'),
      this._translate.get('Settings.SortNameOption'),
      this._translate.get('Settings.SortCreatedAtOption')
    ]).pipe(takeUntil(this._destroy$)).subscribe(([senderText, nameText, createdAtText]) => {
      this.sortOptions = [
        new MenuOption(senderText || 'Settings.SortSenderOption', 'sender'),
        new MenuOption(nameText || 'Settings.SortNameOption', 'displayName'),
        new MenuOption(createdAtText || 'Settings.SortCreatedAtOption', 'createdAt')
      ];
    });
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public loadItems(): Observable<IResultSet<MessageSender> | null | undefined> {
    return this._api
      .getMessageSenders(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
      .pipe(
        tap((result: MessageSenderResultSet) => {
          this.defaultSender = result?.items?.find(i => i.isDefault);
        }),
        map((result: MessageSenderResultSet) => (result as IResultSet<MessageSender>))
      );
  }

  public deleteConfirmation(sender: MessageSender): void {
    combineLatest([
      this._translate.get('Settings.DeleteSenderTitle'),
      this._translate.get('Settings.DeleteSenderConfirmMessage', { sender: sender.displayName })
    ]).pipe(takeUntil(this._destroy$)).subscribe(([title, message]) => {
      const modal = this._modalService.show(BasicModalComponent, {
        animated: true,
        initialState: {
          title: title || 'Settings.DeleteSenderTitle',
          message: message || 'Settings.DeleteSenderConfirmMessage',
          data: sender
        },
        keyboard: true
      });
    modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
          const sender = response.result.data;
          this._api.deleteMessageSender(sender.id).subscribe(() => {
            this._toaster.show(ToastType.Success, 'Settings.DeleteSenderSuccessTitle', 'Settings.DeleteSenderSuccessMessage', undefined, { sender: sender.displayName }); // localized toast single line
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['settings']));
          });
        }
      });
    });
  }

  public override actionHandler(): void {
    this._router.navigate(['', { outlets: { rightpane: 'create-message-sender' } }]);
  }
}
