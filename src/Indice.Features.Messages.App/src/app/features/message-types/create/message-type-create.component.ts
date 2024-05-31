import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';
import { CreateMessageTypeRequest, MessagesApiClient, MessageType } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-message-type-create',
    templateUrl: './message-type-create.component.html'
})
export class MessageTypeCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _router: Router,
        private _translate: TranslateService,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public submitInProgress = false;
    public model = new CreateMessageTypeRequest({ name: '' });

    public ngOnInit(): void { }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        this._api
            .createMessageType(this.model)
            .subscribe({
                next: (messageType: MessageType) => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, this._translate.instant('message-type.success-save'), `'${this._translate.instant('message-type.success-save-message')}' '${messageType.name}'`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['message-types']));
                }
            });
    }
}
