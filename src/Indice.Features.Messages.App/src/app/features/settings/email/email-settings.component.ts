import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, IResultSet, ListViewType, MenuOption, ModalService, ToastType, ToasterService } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subscription } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { MessageSender, MessageSenderResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
  selector: 'app-email-settings',
  templateUrl: './email-settings.component.html'
})
export class EmailSettingsComponent extends BaseListComponent<MessageSender> implements OnInit, OnDestroy {

  private langChangeSubscription: Subscription | null = null;
  constructor(
    route: ActivatedRoute,
    private _router: Router,
    private _translate: TranslateService,
    private _api: MessagesApiClient,
    @Inject(ToasterService) private _toaster: ToasterService,
    private _modalService: ModalService
) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 10;
    this.sort = 'isDefault';
    this.sortdir = 'desc';
    this.search = '';
}

  public newItemLink: string | null = 'settings';
  public defaultSender: MessageSender | undefined;

  public override ngOnInit(): void {
    // this._api.getMessageSenders(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, true)
    //   .subscribe((result) => {
    //     this.defaultSender = result.items?.find(x => x);
    //   });
    super.ngOnInit();
    this.langChangeSubscription = this._translate.onLangChange.subscribe(() => {
            this.updateMenuOptions(); 
      });
    this.updateMenuOptions(); 
  }

  public override ngOnDestroy(): void {
    if (this.langChangeSubscription) {
        this.langChangeSubscription.unsubscribe();
    }
  }

  private updateMenuOptions(): void {
    this.sortOptions = [
      new MenuOption(this._translate.instant('settings.email.sender-list.title'), 'sender'),
      new MenuOption(this._translate.instant('settings.email.sender-list.name'), 'displayName'),
      new MenuOption(this._translate.instant('settings.email.sender-list.created-at'), 'createdAt')
    ];
  }

  public loadItems(): Observable<IResultSet<MessageSender> | null | undefined> {
    return this._api
      .getMessageSenders(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
      .pipe(tap((result: MessageSenderResultSet) => {
        this.defaultSender = result?.items?.find(i => i.isDefault);
      }), map((result: MessageSenderResultSet) => (result as IResultSet<MessageSender>)));
  }

  public deleteConfirmation(sender: MessageSender): void {
    const modal = this._modalService.show(BasicModalComponent, {
      animated: true,
      initialState: {
        title: this._translate.instant('settings.email.sender-list.delete'),
        message: `'${this._translate.instant('settings.email.sender-list.delete-warning')}' '${sender.displayName}';`,
        data: sender
      },
      keyboard: true
    });
    modal.onHidden?.subscribe((response: any) => {
      if (response.result?.answer) {
        const sender = response.result.data;
        this._api.deleteMessageSender(sender.id).subscribe(() => {
          this._toaster.show(ToastType.Success, this._translate.instant('settings.email.sender-list.success-delete'), `'${this._translate.instant('settings.email.sender-list.success-delete-message')}' '${sender.displayName}' `);
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['settings']));
        });
      }
    });
  }

  public override actionHandler(): void {
    this._router.navigate(['', { outlets: { rightpane: 'create-message-sender' } }]);
  }
}
