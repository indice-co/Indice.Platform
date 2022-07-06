import { CasesAttachmentLink } from './../../../../core/services/cases-api.service';
import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { ToasterService, ToastType } from '@indice/ng-components';
import { forkJoin, Observable } from 'rxjs';
import { CaseDetails, CasesApiService, EditCaseRequest } from 'src/app/core/services/cases-api.service';
import { UploadFileWidgetService } from 'src/app/core/services/file-upload.service';
import { JSFFileWidgetComponent } from 'src/app/shared/ajsf/jsf-file-widget.component';
import { TailwindSubmitWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/submit-widget/submit-widget.component';
import { TailwindFrameworkComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/tailwind-framework.component';
import { Router } from '@angular/router';
import { SelectWidgetComponent } from 'src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/select-widget/select-widget.component';

@Component({
  selector: 'app-case-form',
  templateUrl: './case-form.component.html',
  styleUrls: ['./case-form.component.scss']
})
export class CaseFormComponent implements OnChanges, OnInit {
  private latestModel: any;
  private latestIsValid: boolean = false;
  // form is editable?
  @Input() formEditable: boolean | undefined;
  @Input() case: CaseDetails | undefined;
  // inform parent that data should be refreshed
  @Output() updateDataEvent = new EventEmitter<void>();
  // the Compound Input Object for json-schema-form
  public formObject: any;
  // Add custom widget
  public widgets = {
    "file": JSFFileWidgetComponent,
    "submit": TailwindSubmitWidgetComponent,
    select: SelectWidgetComponent
  };
  // Add custom framework
  public framework = {
    framework: TailwindFrameworkComponent
  };
  public jsonFormOptions: any = {
    addSubmit: false, // Don't show a submit button if layout does not have one
    setSchemaDefaults: true, // Always use schema defaults for empty fields
    draft: false
  };
  private copiedLayout: any;

  constructor(
    private _toaster: ToasterService,
    private _api: CasesApiService,
    private uploadFileWidgetService: UploadFileWidgetService,
    private router: Router
  ) { }

  ngOnChanges(changes: SimpleChanges) {
    if (!this.case) { // no case -> no point to continue execution
      return;
    }
    if (changes.hasOwnProperty('case')) {
      // deep copy layout
      this.copiedLayout = JSON.parse(this.case.caseType?.layout!);
    }
    // extract layout from case type
    let layout = JSON.parse(this.case.caseType?.layout!);
    // transform layout only when formEditable input is changed
    if (changes.hasOwnProperty('formEditable')) {
      this.transformLayout(layout, !this.formEditable, this.case.draft);
    }
    this.jsonFormOptions.draft = this.case.draft;
    this.jsonFormOptions.addSubmit = this.case.draft || this.formEditable;
    this.formObject = {
      schema: JSON.parse(this.case.caseType?.dataSchema!),
      layout: layout,
      data: JSON.parse(this.case.data!)
    };
  }

  ngOnInit(): void { }

  public onSubmit(event: any): void {
    if (this.case?.draft) {
      // we have to send user's document(s) to the server
      const callsDict: { [key: string]: Observable<CasesAttachmentLink> } = {};
      // what is the key of the dictionary here? the dataPointer (e.g. "/homeAddress/attachmentId") that was added in the file-upload widget
      for (const key in this.uploadFileWidgetService.files) {
        if (this.uploadFileWidgetService.files.hasOwnProperty(key)) {
          const fileParam = { data: this.uploadFileWidgetService.files[key], fileName: this.uploadFileWidgetService.files[key].name };
          callsDict[key] = this._api.uploadAdminCaseAttachment(this.case.id!, undefined, fileParam);
        }
      }
      forkJoin(callsDict).subscribe(
        // we can match the received attachment guids to the respective fields with the help of the keys/dataPointers!
        (response: { [key: string]: CasesAttachmentLink }) => {
          let stringifiedData = JSON.stringify(event);
          for (const key in response) {
            if (response.hasOwnProperty(key)) {
              // we simply replace the dataPointer with the guid that we received from the server
              stringifiedData = stringifiedData.replace(key, response[key].id!);
            }
          }
          // finally, submit the case
          this._api.submitAdminCase(this.case?.id!, stringifiedData).subscribe(
            () => {
              this._toaster.show(ToastType.Success, 'Επιτυχής Επεξεργασία', `Η αίτηση επεξεργάστηκε επιτυχώς.`, 5000);
              this.router.navigate(['/cases']);
            },
            (error) => {
              // TODO: handle error
              // console.log('error in submit: ' + error);
            }
          );
        },
        (error) => {
          // TODO: handle error
          // console.log('error in addAttachment fork join: ' + error);
        }
      );
    } else {
      const editCaseRequest = new EditCaseRequest({ data: JSON.stringify(event) });
      this._api.editCase(this.case?.id!, undefined, editCaseRequest)
        .subscribe(_ => {
          this.updateDataEvent.emit();
          this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Οι αλλαγές σας αποθηκεύτηκαν με επιτυχία.`, 5000);
        }, _ => {
          this._toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `Δεν κατέστη εφικτό να αποθηκευτούν οι αλλαγές σας.`, 5000);
        });
    }
  }

  public onChanges(event: any) {
    this.latestModel = event;
  }
  public isValid(event: any) {
    this.latestIsValid = event;
  }

  onSubmitExternal(): void {
    if (this.latestIsValid) {
      this.onSubmit(this.latestModel);
    }
  }

  onCancel(): void {
    this.updateDataEvent.emit();
  }

  private transformLayout(layout: any, viewOnlyMode: boolean = true, draft?: boolean): void {
    // first check for draft, and override json layout
    if (draft) {
      // case is draft, we need all form properties available and ignore layout restrictions (eg readonly, or html class disable)
      this.removeReadonlyProperties(layout);
      return;
    }
    // case is not draft, check for editability
    if (!viewOnlyMode) {
      // form is editable      
      layout = this.copiedLayout;
      this.jsonFormOptions.addSubmit = true;
    } else {
      this.addReadonlyProperties(layout);
    }
  }

  private removeReadonlyProperties(layout: any): void {
    const activateElement = (element: any) => {
      delete element.readonly;
      if (element.htmlClass && element.htmlClass.indexOf('disableCheckbox') > -1) {
        element.htmlClass = element.htmlClass.replace('disableCheckbox', '');
      }
    }
    layout.forEach((element: any) => {
      activateElement(element);
      if (element.hasOwnProperty('items')) { // ajsf sections have items!
        element.items.forEach((item: any) => {
          activateElement(item);
          if (item.hasOwnProperty('items')) { // ajsf flex containers have items!
            item.items.forEach((i: any) => {
              activateElement(i);
            });
          }
        });
      }
    });
  }

  private addReadonlyProperties(layout: any): void {
    // form is view-only -> add readonly property to all objects of layout object!
    layout.forEach((element: any) => {
      element.readonly = "true";
      if (element.hasOwnProperty('items')) { // ajsf sections have items!
        element.items.forEach((item: any) => {
          item.readonly = "true";
          if (item.hasOwnProperty('items')) { // ajsf flex containers have items!
            item.items.forEach((i: any) => {
              i.readonly = "true";
            });
          }
        });
      }
    });
  }

}
