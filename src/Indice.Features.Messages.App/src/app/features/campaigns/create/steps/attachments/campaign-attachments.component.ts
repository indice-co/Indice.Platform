import { Component, OnInit } from '@angular/core';
import { AbstractControl, UntypedFormControl, UntypedFormGroup } from '@angular/forms';
import { FileParameter } from 'src/app/core/services/messages-api.service';
import { IAttachment } from 'src/app/shared/components/file-upload/file-upload.component';

@Component({
  selector: 'app-campaign-attachments',
  templateUrl: './campaign-attachments.component.html'
})
export class CampaignAttachmentsComponent implements OnInit {
  constructor() { }

  // Form Controls
  public get attachment(): AbstractControl { return this.form.get('attachment')!; }
  // Properties
  public form!: UntypedFormGroup;

  public ngOnInit(): void {
      this._initForm();
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
