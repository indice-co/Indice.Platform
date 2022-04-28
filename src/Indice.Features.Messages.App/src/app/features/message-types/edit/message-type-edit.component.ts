import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { Subscription } from 'rxjs';
import { map, mergeMap } from 'rxjs/operators';
import { MessagesApiClient, MessageType, UpsertMessageTypeRequest, ValidationProblemDetails } from 'src/app/core/services/messages-api.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';

@Component({
    selector: 'app-message-type-edit',
    templateUrl: './message-type-edit.component.html'
})
export class MessageTypeEditComponent implements OnInit, AfterViewInit, OnDestroy {
    private _getTypeSubscription!: Subscription;

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _router: Router,
        private _activatedRoute: ActivatedRoute,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _utilities: UtilitiesService
    ) { }

    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    public submitInProgress = false;
    public model = new UpsertMessageTypeRequest({ name: '' });

    public ngOnInit(): void {
        this._getTypeSubscription = this._api
            .getMessageTypeById(this._activatedRoute.snapshot.params['messageTypeId'])
            .subscribe((messageType: MessageType) => this.model.name = messageType.name);
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }


    public ngOnDestroy(): void {
        this._getTypeSubscription?.unsubscribe();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        this._api
            .createMessageType(this.model)
            .subscribe({
                next: (messageType: MessageType) => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Ο τύπος με όνομα '${messageType.name}' δημιουργήθηκε με επιτυχία.`);
                    // This is to force reload message types page when a new campaign is successfully saved.
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['message-types']));
                },
                error: (problemDetails: ValidationProblemDetails) => {
                    this._toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `${this._utilities.getValidationProblemDetails(problemDetails)}`, 6000);
                }
            });
    }
}
