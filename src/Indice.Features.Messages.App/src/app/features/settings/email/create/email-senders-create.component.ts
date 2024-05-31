import { ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { MessagesApiClient, CreateMessageSenderRequest, MessageSender } from 'src/app/core/services/messages-api.service';
import { SettingsStore } from '../../settings-store.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-email-senders-create',
  templateUrl: './email-senders-create.component.html'
})
export class EmailSendersCreateComponent implements OnInit {
  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

  constructor(
      private _changeDetector: ChangeDetectorRef,
      private _api: MessagesApiClient,
      private _router: Router,
      private _translate: TranslateService,
      private _settingsStore: SettingsStore,
      @Inject(ToasterService) private _toaster: ToasterService
  ) { }

  public submitInProgress = false;
  public model = new CreateMessageSenderRequest({ 
    sender: '',
    displayName: '' 
  });

  public ngOnInit(): void { }

  public ngAfterViewInit(): void {
      this._changeDetector.detectChanges();
  }

  public onSubmit(): void {
      this.submitInProgress = true;
      this._settingsStore
          .createMessageSender(this.model)
          .subscribe({
              next: (messageSender: MessageSender) => {
                  this.submitInProgress = false;
                  this._toaster.show(ToastType.Success, this._translate.instant('settings.email.success-save'), `'${this._translate.instant('settings.email.success-save-message')}' '${messageSender.displayName}'`);
                  this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['settings']));
              }
          });
  }
}
