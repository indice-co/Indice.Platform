import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, IResultSet, ListViewType, MenuOption, ModalService, ToastType, ToasterService } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { MessageSender, MessageSenderResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
  selector: 'app-email-settings',
  templateUrl: './email-settings.component.html'
})
export class EmailSettingsComponent extends BaseListComponent<MessageSender> implements OnInit {

  constructor(
    route: ActivatedRoute,
    private _router: Router,
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
    this.sortOptions = [
      new MenuOption('Αποστολέας', 'sender'),
      new MenuOption('Όνομα', 'displayName'),
      new MenuOption('Δημιουργήθηκε', 'createdAt')
    ];
}

  public newItemLink: string | null = 'settings';
  public defaultSender: MessageSender | undefined;

  public override ngOnInit(): void {
    // this._api.getMessageSenders(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, true)
    //   .subscribe((result) => {
    //     this.defaultSender = result.items?.find(x => x);
    //   });
    super.ngOnInit();
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
        title: 'Διαγραφή',
        message: `Είστε σίγουρος ότι θέλετε να διαγράψετε τον αποστολέα '${sender.displayName}';`,
        data: sender
      },
      keyboard: true
    });
    modal.onHidden?.subscribe((response: any) => {
      if (response.result?.answer) {
        const sender = response.result.data;
        this._api.deleteMessageSender(sender.id).subscribe(() => {
          this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Ο αποστολέας '${sender.displayName}' διαγράφηκε με επιτυχία.`);
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['settings']));
        });
      }
    });
  }

  public override actionHandler(): void {
    this._router.navigate(['', { outlets: { rightpane: 'create-message-sender' } }]);
  }
}
