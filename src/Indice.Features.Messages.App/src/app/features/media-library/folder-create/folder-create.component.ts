import { ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { CreateFolderRequest, CreateFolderResponse } from 'src/app/core/services/media-api.service';
import { MediaLibraryStore } from '../media-library-store.service';

@Component({
  selector: 'app-folder-create',
  templateUrl: './folder-create.component.html'
})
export class FolderCreateComponent implements OnInit {

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _mediaStore: MediaLibraryStore,
        private _router: Router,
        private _activatedRoute: ActivatedRoute,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public submitInProgress = false;
    public model = new CreateFolderRequest({ name: '' });
    private _folderId: string | undefined;

    public ngOnInit(): void { 
      this._folderId = this._router.url.split('/')[2]?.split('(')[0];
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        this.model.parentId = this._folderId;
        this._mediaStore
            .createFolder(this.model)
            .subscribe({
                next: (response: CreateFolderResponse) => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Ο φάκελος με όνομα '${this.model.name}' δημιουργήθηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this.model.parentId ? this._router.navigate(['media', this.model.parentId]) : this._router.navigate(['media']));
                }
            });
    }

}
