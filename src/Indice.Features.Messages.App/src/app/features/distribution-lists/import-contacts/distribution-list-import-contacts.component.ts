import { AfterViewInit, Component, ElementRef, OnInit, Inject, ViewChild } from "@angular/core";
import { BulkCreateDistributionListContactsRequest, FileParameter, MessagesApiClient } from "src/app/core/services/messages-api.service";
import { Router } from "@angular/router";
import { AbstractControl, UntypedFormControl, UntypedFormGroup } from "@angular/forms";
import { IAttachment } from "src/app/shared/components/file-upload/file-upload.component";
import { ToasterService, ToastType } from "@indice/ng-components";
import { AppTranslatedToaster } from "../../../shared/services/app-translated-toaster";

@Component({
    selector: 'app-distribution-list-import-contacts',
    templateUrl: './distribution-list-import-contacts.component.html',
    standalone: false
})
export class DistributionListImportContactsComponent implements OnInit, AfterViewInit {
  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

  private _distributionListId: string = '';

  public get attachment(): AbstractControl { return this.form.get('attachment')!; }
  public form!: UntypedFormGroup;

  public submitInProgress = false;
  public model = new BulkCreateDistributionListContactsRequest();

  constructor(
      private _api: MessagesApiClient,
      private _router: Router,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster
    ) {}

  public ngOnInit(): void {
    this._distributionListId = this._router.url.split('/')[2];
    this._initForm();
  }

  public ngAfterViewInit(): void {}

  public onSubmit(): void {
    const fileAttachment: FileParameter = this.attachment.value as FileParameter;
    this.submitInProgress = true;
    this._api
      .bulkImportContactsToDistributionList(this._distributionListId, fileAttachment)
      .subscribe({
        error: (err) => {
          console.error('Failed to import contacts in distribution list.', err);
          this._toaster.show(
            ToastType.Error,
            'DistributionLists.EditImportContactsErrorTitle',
            'DistributionLists.EditImportContactsErrorMessage'
          );
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists', this._distributionListId, 'distribution-list-contacts']));
        },
        next: () => {
          this._toaster.show(
            ToastType.Success,
            'DistributionLists.EditImportContactsSuccessTitle',
            'DistributionLists.ImportContactsSuccessMessage'
          );
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists', this._distributionListId, 'distribution-list-contacts']));
        }
      });
  }

  public onFileChange(file: IAttachment | undefined) {
    if (!file) {
      this.attachment.setValue(null)
      return;
    }
    this.attachment.setValue(<FileParameter>{
      fileName: file.title,
      data: file.data
    });
  }

  private _initForm(): void {
      this.form = new UntypedFormGroup({
          attachment: new UntypedFormControl(false)
      });
  }
}
