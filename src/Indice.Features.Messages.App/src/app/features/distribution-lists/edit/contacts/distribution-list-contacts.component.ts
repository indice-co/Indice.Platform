import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, ToastType, ViewAction } from '@indice/ng-components';
import { combineLatest, Observable, Subject, Subscription } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { Contact, ContactResultSet, DistributionList, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { FileResponse } from 'src/app/core/services/messages-api.service';
import { AppLanguagesService } from '../../../../shared/services/app-languages.service';
import { AppTranslatedToaster } from '../../../../shared/services/app-translated-toaster';

@Component({
    selector: 'app-distribution-list-contacts',
    templateUrl: './distribution-list-contacts.component.html'
})
export class DistributionListContactsComponent extends BaseListComponent<Contact> implements OnInit, OnDestroy {
    private _distributionListId: string = '';
    private _getListSubscription!: Subscription;
    private _exportViewActionKey = 'export-contacts';
    private _importViewActionKey = 'import-contacts';
    private _destroy$ = new Subject<void>();

    constructor(
        route: ActivatedRoute,
        private _router: Router,
        private _api: MessagesApiClient,
        private _modalService: ModalService,
        private _activatedRoute: ActivatedRoute,
        private _lang: AppLanguagesService,
        @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster

    ) {
        super(route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 20;
        this.sort = 'updatedAt';
        this.sortdir = 'desc';
        this.search = '';
        this.sortOptions = [
            new MenuOption('Όνομα', 'firstName'),
            new MenuOption('Επίθετο', 'lastName'),
            new MenuOption('Email', 'email'),
            new MenuOption('Τηλέφωνο', 'phone'),
            new MenuOption('Δημιουργήθηκε', 'updatedAt'),
            new MenuOption('Κωδικός', 'recipientId'),
            new MenuOption('Resolved', 'resolved'),
            new MenuOption('Last Resolution', 'lastResolutionDate'),
        ];
    }

    public newItemLink: string | null = 'create-contact';
    public full = true;
    public distributionList = new DistributionList({ name: '' });

    public override ngOnInit(): void {
        this._distributionListId = this._activatedRoute.parent?.snapshot.params['distributionListId'];
        super.ngOnInit();
        this._getListSubscription = this._api.getDistributionListById(this._distributionListId).subscribe((list: DistributionList) => {
            this.distributionList = list;
        });
       this.itemTranslation();
    }

  public itemTranslation() {
    const firstName$ = this._lang.translateKey('DistributionLists.SortFirstNameOption');
    const lastName$ = this._lang.translateKey('DistributionLists.SortLastNameOption');
    const email$ = this._lang.translateKey('DistributionLists.SortEmailOption');
    const phone$ = this._lang.translateKey('DistributionLists.SortPhoneOption');
    const updatedAt$ = this._lang.translateKey('DistributionLists.SortUpdatedAtOption');
    const contactCode$ = this._lang.translateKey('DistributionLists.SortContactCodeOption');
    const resolved$ = this._lang.translateKey('DistributionLists.SortResolvedOption');
    const lastResolution$ = this._lang.translateKey('DistributionLists.SortLastResolutionDateOption');

    combineLatest([
      firstName$, lastName$, email$, phone$, updatedAt$, contactCode$, resolved$, lastResolution$])
      .pipe(takeUntil(this._destroy$))
      .subscribe(([firstName, lastName, email, phone, updatedAt, contactCode, resolved, lastResolution]) => {
        this.sortOptions = [
          new MenuOption(firstName || 'DistributionLists.SortFirstNameOption', 'firstName'),
          new MenuOption(lastName || 'DistributionLists.SortLastNameOption', 'lastName'),
          new MenuOption(email || 'DistributionLists.SortEmailOption', 'email'),
          new MenuOption(phone || 'DistributionLists.SortPhoneOption', 'phone'),
          new MenuOption(updatedAt || 'DistributionLists.SortUpdatedAtOption', 'updatedAt'),
          new MenuOption(contactCode || 'DistributionLists.SortContactCodeOption', 'recipientId'),
          new MenuOption(resolved || 'DistributionLists.SortResolvedOption', 'resolved'),
          new MenuOption(lastResolution || 'DistributionLists.SortLastResolutionDateOption', 'lastResolutionDate')
        ];
      });
    // add custom ViewActions for importing/exporting contacts
    this.actions.push(
      new ViewAction(this._exportViewActionKey, this._exportViewActionKey, '', 'ms-Icon ms-Icon--Download ', 'DistributionLists.ExportContactsTooltip', ''),
      new ViewAction(this._importViewActionKey, this._importViewActionKey, '', 'ms-Icon ms-Icon--Upload ', 'DistributionLists.ImportContactsTooltip', '')
    );
    const exportTooltip$ = this._lang.translateKey('DistributionLists.ExportContactsTooltip');
    const importTooltip$ = this._lang.translateKey('DistributionLists.ImportContactsTooltip');
    combineLatest([exportTooltip$, importTooltip$]).pipe(takeUntil(this._destroy$)).subscribe(([exportView, importView]) => {
      var exportTooltip = this.actions.filter(o => o.type == this._exportViewActionKey)
      exportTooltip[0]!.tooltip = exportView;
      var importTooltip = this.actions.filter(o => o.type == this._importViewActionKey)
      importTooltip[0]!.tooltip = importView;
    })

  }

    public loadItems(): Observable<IResultSet<Contact> | null | undefined> {
        return this._api
            .getDistributionListContacts(this._distributionListId, this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
            .pipe(map((result: ContactResultSet) => (result as IResultSet<Contact>)));
    }

    public deleteConfirmation(contact: Contact): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: 'Διαγραφή',
                message: `Είστε σίγουρος ότι θέλετε να διαγράψετε την επαφή '${contact.fullName || contact.email}' από τη λίστα '${this.distributionList.name}';`,
                data: contact
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                const contact = response.result.data;
                this._api.removeContactFromDistributionList(this._distributionListId, contact.id).subscribe(() => {
                  this._toaster.show(ToastType.Success, 'DistributionLists.DeleteContactSuccessTitle', `DistributionLists.DeleteContactSuccessMessage`, undefined, { name: contact.fullName || contact.email });
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists', this._distributionListId, 'distribution-list-contacts']));
                });
            }
        });
    }

    public override ngOnDestroy(): void {
        this._getListSubscription?.unsubscribe();
    }

    public override actionHandler(action: ViewAction): void {
        if (action.icon === Icons.Refresh) {
            this.search = '';
            this.refresh();
        }
        else if (action.key === this._importViewActionKey) {
            this._router.navigate(['', { outlets: { rightpane: ['import-contacts'] } }]);
        }
        else if (action.key === this._exportViewActionKey) {
            this.exportContactsFromDistributionList(this._distributionListId);
        }
    }

    private exportContactsFromDistributionList(distributionListId: string): void {
        this._api
            .bulkExportContactsFromDistributionList(distributionListId)
            .subscribe({
                error: (err) => {
                    console.error('Distribution list contacts export failed.', err);
                },
                next: (response: FileResponse) => {
                    const url = window.URL.createObjectURL(response.data);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = response.fileName as string;
                    a.click();
                    window.URL.revokeObjectURL(url);
                }
            });
    }
}
