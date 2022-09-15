import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { Subscription } from 'rxjs';
import { DistributionList } from 'src/app/core/services/messages-api.service';
import { DistributionListEditStore } from '../../distribution-list-edit-store.service';
import { settings } from 'src/app/core/models/settings';

@Component({
    selector: 'app-distribution-list-details-edit-rightpane',
    templateUrl: './distribution-list-edit-details-rightpane.component.html'
})
export class DistributionListDetailsEditRightpaneComponent implements OnInit, AfterViewInit, OnDestroy {
    private _updateDistributionListSubscription: Subscription | undefined;
    private _distributionListId = '';

    constructor(
        private _distributionListStore: DistributionListEditStore,
        private _router: Router,
        private _activatedRoute: ActivatedRoute,
        private _changeDetector: ChangeDetectorRef,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    @ViewChild('editNameTemplate', { static: true }) public editNameTemplate!: TemplateRef<any>;
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    public submitInProgress = false;
    public templateOutlet!: TemplateRef<any>;
    public model = new DistributionList();

    public ngOnInit(): void {
        this._distributionListId = this._router.url.split('/')[2];
        this._activatedRoute.queryParams.subscribe((queryParams: Params) => {
            this._selectTemplate(queryParams.action || 'editName');
        });
    }

    public ngAfterViewInit(): void {
        this._distributionListStore
            .getDistributionList(this._distributionListId)
            .subscribe((distributionList: DistributionList) => this.model = distributionList);
        this._changeDetector.detectChanges();
    }

    public ngOnDestroy(): void {
        this._updateDistributionListSubscription?.unsubscribe();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        this._updateDistributionListSubscription = this._distributionListStore
            .updateDistributionList(this._distributionListId, this.model)
            .subscribe({
                next: () => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Το πρότυπο με όνομα '${this.model.name}' αποθηκεύτηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists', this._distributionListId]));
                }
            });
    }

    private _selectTemplate(action: string): void {
        switch (action) {
            case 'editName':
                this.templateOutlet = this.editNameTemplate;
                break;
        }
    }
}
