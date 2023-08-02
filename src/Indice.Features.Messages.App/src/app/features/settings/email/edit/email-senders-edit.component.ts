import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { MessagesApiClient, UpdateMessageSenderRequest, MessageSender } from 'src/app/core/services/messages-api.service';
import { SettingsStore } from '../../settings-store.service';

@Component({
  selector: 'app-email-senders-edit',
  templateUrl: './email-senders-edit.component.html'
})
export class EmailSendersEditComponent implements OnInit, AfterViewInit {
  private _messageSenderId: string = '';

  constructor(
      private _changeDetector: ChangeDetectorRef,
      private _api: MessagesApiClient,
      private _router: Router,
      private _activatedRoute: ActivatedRoute,
      private _settingsStore: SettingsStore,
      @Inject(ToasterService) private _toaster: ToasterService
  ) { }

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  public submitInProgress = false;
  public model = new UpdateMessageSenderRequest({ 
    sender: '',
    displayName: ''
 });

  public ngOnInit(): void {
      this._messageSenderId = this._activatedRoute.snapshot.params['messageSenderId'];
      this._api
          .getMessageSenderById(this._messageSenderId)
          .subscribe((messageSender: MessageSender) => {
            this.model.displayName = messageSender.displayName;
            this.model.sender = messageSender.sender;
          });
  }

  public ngAfterViewInit(): void {
      this._changeDetector.detectChanges();
  }

  public onSubmit(): void {
      this.submitInProgress = true;
      this._settingsStore
          .updateMessageSender(this._messageSenderId, this.model)
          .subscribe({
              next: () => {
                  this.submitInProgress = false;
                  this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Ο αποστολέας με διεύθυνση '${this.model.sender}' αποθηκεύτηκε με επιτυχία.`);
                  this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['settings']));
              }
          });
  }
}
