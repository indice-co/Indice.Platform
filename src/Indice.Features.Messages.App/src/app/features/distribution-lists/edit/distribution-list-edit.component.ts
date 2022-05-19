import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { Subscription } from 'rxjs';
import { DistributionList, MessagesApiClient, UpdateDistributionListRequest } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-distribution-list-edit',
    templateUrl: './distribution-list-edit.component.html'
})
export class DistributionListEditComponent implements OnInit, AfterViewInit, OnDestroy {
    private _getListSubscription!: Subscription;
    private _updateListSubscription!: Subscription;
    private _distributionListId: string = '';

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _router: Router,
        private _activatedRoute: ActivatedRoute,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    public submitInProgress = false;
    public model = new UpdateDistributionListRequest({ name: '' });

    public ngOnInit(): void {
        this._distributionListId = this._activatedRoute.snapshot.params['distributionListId'];
        this._getListSubscription = this._api
            .getDistributionListById(this._distributionListId)
            .subscribe((list: DistributionList) => this.model.name = list.name);
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public ngOnDestroy(): void {
        this._getListSubscription?.unsubscribe();
        this._updateListSubscription?.unsubscribe();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        this._updateListSubscription = this._api
            .updateDistributionList(this._distributionListId, this.model)
            .subscribe({
                next: () => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η λίστα με όνομα '${this.model.name}' αποθηκεύτηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
                }
            });
    }
}
