import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { FileParameter } from 'src/app/core/services/media-api.service';
import { FileUploadComponent, IAttachment } from 'src/app/shared/components/file-upload/file-upload.component';
import { MediaLibraryStore } from '../media-library-store.service';

@Component({
  selector: 'app-document-upload',
  templateUrl: './document-upload.component.html'
})
export class DocumentUploadComponent implements OnInit {
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    @ViewChild('fileUploadComponent') public fileUploadComponent!: FileUploadComponent;

    public isLoading = false;
    public file: IAttachment | undefined;
    public files: IAttachment[] = [];

    private _folderId: string | undefined;

    constructor(private _mediaStore: MediaLibraryStore, private _toaster: ToasterService, private _router: Router, private _activatedRoute: ActivatedRoute) { }

    ngOnInit(): void {
        this._folderId = this._router.url.split('/')[2]?.split('(')[0];
    }

    public onFileChange(files: IAttachment[]): void {
      this.files = files;
    }


    public onSubmit(): void {
        if (this.files.length > 0) {
            for (let file of this.files) {
            let fileParameter = <FileParameter>{
                fileName: file.title,
                data: file.data
            }
            this._mediaStore.uploadFile(this._folderId, [fileParameter])
                .subscribe(() => {
                this._toaster.show(ToastType.Success, 'Επιτυχής ενημέρωση', `Το επισυναπτόμενο αρχείο ενημερώθηκε με επιτυχία.`);
                this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._folderId ? this._router.navigate(['media', this._folderId]) : this._router.navigate(['media']));
            }, (error) => {
                this._toaster.show(ToastType.Error, 'Σφάλμα ενημέρωσης', `Υπήρξε κάποιο πρόβλημα με την ενημέρωση των στοιχείων. Παρακαλώ δοκιμάστε αργότερα.`);
                this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._folderId ? this._router.navigate(['media', this._folderId]) : this._router.navigate(['media']));
            });
            }
        }
    }
}
