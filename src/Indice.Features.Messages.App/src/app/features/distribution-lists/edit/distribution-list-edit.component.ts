import { AfterViewChecked, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { HeaderMetaItem, ViewLayoutComponent } from '@indice/ng-components';
import { DistributionList } from 'src/app/core/services/messages-api.service';
import { DistributionListEditStore } from './distribution-list-edit-store.service';

@Component({
    selector: 'app-distribution-list',
    templateUrl: './distribution-list-edit.component.html'
})
export class DistributionListEditComponent implements OnInit, AfterViewChecked {
    @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;
    private _distributionListId?: string;

    constructor(
        private _activatedRoute: ActivatedRoute,
        private _changeDetector: ChangeDetectorRef,
        private _distributionListStore: DistributionListEditStore
    ) { }

    public submitInProgress = false;
    public distributionList: DistributionList | undefined;
    public metaItems: HeaderMetaItem[] = [];

    public ngOnInit(): void {
        this._distributionListId = this._activatedRoute.snapshot.params['distributionListId'];
        if (this._distributionListId) {
            this._distributionListStore.getDistributionList(this._distributionListId!).subscribe((distributionList: DistributionList) => {
                this.distributionList = distributionList;
                this._layout.title = `Λίστα διανομής - ${distributionList.name}`;
            });
        }
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }
}
