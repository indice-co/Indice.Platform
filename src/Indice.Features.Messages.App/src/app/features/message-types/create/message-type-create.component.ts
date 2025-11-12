import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { ToastType } from '@indice/ng-components';
import { catchError, EMPTY } from 'rxjs';
import { CreateMessageTypeRequest, MessagesApiClient, MessageType, MessageTypeClassification } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';

@Component({
    selector: 'app-message-type-create',
    templateUrl: './message-type-create.component.html',
    standalone: false
})
export class MessageTypeCreateComponent implements OnInit, AfterViewInit {
  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _api: MessagesApiClient,
    private _router: Router,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster
  ) { }

  public submitInProgress = false;
  public model = new CreateMessageTypeRequest({ name: '', alias: undefined, classification: MessageTypeClassification.System });
  public classifications = Object.values(MessageTypeClassification);

  public ngOnInit(): void { }

  public ngAfterViewInit(): void {
    this._changeDetector.detectChanges();
  }

  public onSubmit(): void {
    this.submitInProgress = true;
    this._api
      .createMessageType(this.model)
      .pipe(
        catchError((error: any) => {
          this.submitInProgress = false;
          return EMPTY;
        }))
      .subscribe({
        next: (messageType: MessageType) => {
          this.submitInProgress = false;
          this._toaster.show(ToastType.Success, 'MessageTypes.CreateSuccessTitle', 'MessageTypes.CreateSuccessMessage', undefined, { name: messageType.name });
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['message-types']));
        }
      });
  }
}
