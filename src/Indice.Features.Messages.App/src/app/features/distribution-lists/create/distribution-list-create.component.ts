import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';
import { CreateDistributionListRequest, MessagesApiClient, MessageType } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-distribution-list-create',
    templateUrl: './distribution-list-create.component.html'
})
export class DistributionListCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _translate: TranslateService,
        private _router: Router,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public submitInProgress = false;
    public model = new CreateDistributionListRequest({ name: '' });

    public ngOnInit(): void { }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        this._api
            .createDistributionList(this.model)
            .subscribe({
                next: (messageType: MessageType) => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, this._translate.instant('distribution-list-create.success-save'), `'${this._translate.instant('distribution-list-create.success-save-message')}' '${messageType.name}' `);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
                }
            });
    }
}
