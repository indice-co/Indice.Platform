import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, RouterViewAction, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Campaign, CampaignResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
    selector: 'app-campaigns',
    templateUrl: './campaigns.component.html'
})
export class CampaignsComponent extends BaseListComponent<Campaign> implements OnInit {
    constructor(
        route: ActivatedRoute,
        private _router: Router,
        private _api: MessagesApiClient,
        private _modalService: ModalService,
        @Inject(ToasterService) private _toaster: ToasterService
    ) {
        super(route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'createdAt';
        this.sortdir = 'desc';
        this.search = '';
        this.sortOptions = [
            new MenuOption('Ημ/νια Δημιουργίας', 'createdAt'),
            new MenuOption('Τίτλος', 'title'),
            new MenuOption('Ενεργή Από', 'activePeriod.from')
        ];
    }

    public newItemLink: string | null = null;
    public full = true;

    public ngOnInit(): void {
        super.ngOnInit();
        this.actions.push(new RouterViewAction(Icons.Add, 'campaigns/add', null, null));
    }

    public deleteConfirmation(campaign: Campaign): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: `Είστε σίγουρος ότι θέλετε να διαγράψετε τo campaign '${campaign.title}';`,
                data: campaign
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteCampaign(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Το campaign με τίτλο '${response.result.data.title}' διαγράφηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['campaigns']));
                });
            }
        });
    }

    public loadItems(): Observable<IResultSet<Campaign> | null | undefined> {
        return this._api
            .getCampaigns(undefined, undefined, this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
            .pipe(map((result: CampaignResultSet) => (result as IResultSet<Campaign>)));
    }

    public actionHandler(action: ViewAction): void {
        if (action.icon === Icons.Refresh) {
            this.search = '';
            this.refresh();
        }
    }
}
