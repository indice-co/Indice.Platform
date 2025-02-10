import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { catchError, EMPTY, Subscription } from 'rxjs';
import { MessagesApiClient, MessageType, UpdateMessageTypeRequest } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-message-type-edit',
  templateUrl: './message-type-edit.component.html'
})
export class MessageTypeEditComponent implements OnInit, AfterViewInit, OnDestroy {
  private _getTypeSubscription!: Subscription;
  private _updateTypeSubscription!: Subscription;
  private _messageTypeId: string = '';

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _api: MessagesApiClient,
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    @Inject(ToasterService) private _toaster: ToasterService
  ) { }

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  public submitInProgress = false;
  public model = new UpdateMessageTypeRequest({ name: '', alias: undefined });

  public ngOnInit(): void {
    this._messageTypeId = this._activatedRoute.snapshot.params['messageTypeId'];
    this._getTypeSubscription = this._api
      .getMessageTypeById(this._messageTypeId)
      .subscribe((messageType: MessageType) => {
        this.model.name = messageType.name;
        this.model.alias = messageType.alias;
      });
  }

  public ngAfterViewInit(): void {
    this._changeDetector.detectChanges();
  }

  public ngOnDestroy(): void {
    this._getTypeSubscription?.unsubscribe();
    this._updateTypeSubscription?.unsubscribe();
  }

  public onSubmit(): void {
    this.submitInProgress = true;
    this._updateTypeSubscription = this._api
      .updateMessageType(this._messageTypeId, this.model)
      .pipe(
        catchError((error: any) => {
          this.submitInProgress = false;
          // Optionally, re-throw the error or return a default value
          return EMPTY;
        })
      )
      .subscribe({
        next: () => {
          this.submitInProgress = false;
          this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Ο τύπος με όνομα '${this.model.name}' αποθηκεύτηκε με επιτυχία.`);
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['message-types']));
        }
      });
  }
}
