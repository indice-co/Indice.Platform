import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { DistributionList, DistributionListResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
    selector: 'app-distribution-lists',
    templateUrl: './distribution-lists.component.html'
})
export class DistributionListsComponent extends BaseListComponent<DistributionList> implements OnInit, OnDestroy {
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
        this.sort = 'name';
        this.sortdir = 'asc';
        this.search = '';
    }

    public newItemLink: string | null = 'create-distribution-list';
    public full = true;

    private _isSystemGeneratedFilter = false;

    public override ngOnInit(): void {
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
        this.sortOptions = [new MenuOption(this._translate.instant('distribution-list-edit.name'), 'name')];
    }

    public loadItems(): Observable<IResultSet<DistributionList> | null | undefined> {
        return this._api
            .getDistributionLists(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, this._isSystemGeneratedFilter)
            .pipe(map((result: DistributionListResultSet) => (result as IResultSet<DistributionList>)));
    }

    public deleteConfirmation(list: DistributionList): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: this._translate.instant('distribution-list-edit.delete'),
                message: `'${this._translate.instant('distribution-list-edit.delete-warning')}' ${list.name};`,
                data: list
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteDistributionList(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, this._translate.instant('distribution-list-edit.success-delete'), `'${this._translate.instant('distribution-list-edit.success-delete-message')}' '${response.result.data.name}'`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
                });
            }
        });
    }

    public override actionHandler(action: ViewAction): void {
        if (action.icon === Icons.Refresh) {
            this.search = '';
            this.refresh();
        }
    }
}
