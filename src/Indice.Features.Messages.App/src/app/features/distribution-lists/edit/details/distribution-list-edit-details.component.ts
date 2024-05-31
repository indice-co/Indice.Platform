import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalService, ToasterService, ToastType } from '@indice/ng-components';

import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { DistributionList, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { DistributionListEditStore } from '../distribution-list-edit-store.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-distribution-list-details-edit',
    templateUrl: './distribution-list-edit-details.component.html'
})
export class DistributionListDetailsEditComponent implements OnInit {
    private _distributionListId: string | undefined;

    constructor(
        private _modalService: ModalService,
        private _api: MessagesApiClient,
        private _translate: TranslateService,
        private _distributionListStore: DistributionListEditStore,
        private _router: Router,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _activatedRoute: ActivatedRoute
    ) { }

    public list: DistributionList | undefined;

    public ngOnInit(): void {
        this._distributionListId = this._activatedRoute.parent?.snapshot.params['distributionListId'];
        if (this._distributionListId) {
            this._distributionListStore.getDistributionList(this._distributionListId!).subscribe((list: DistributionList) => {
                this.list = list;
            });
        }
    }

    public deleteTemplate(): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: this._translate.instant('distribution-list-edit.details.delete'),
                message: `'${this._translate.instant('distribution-list-edit.details.delete-warning')}' '${this.list?.name}';`,
                data: this.list
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteDistributionList(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, this._translate.instant('distribution-list-edit.details.success-delete'), `'${this._translate.instant('distribution-list-edit.details.success-delete-message')}' '${response.result.data.name}'`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
                });
            }
        });
    }

    public openEditPane(action: string): void {
        this._router.navigate(['', { outlets: { rightpane: ['edit-distribution-list'] } }], { queryParams: { action: action } });
    }
}
