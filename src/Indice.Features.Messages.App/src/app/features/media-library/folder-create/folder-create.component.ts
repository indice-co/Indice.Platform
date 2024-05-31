import { ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { CreateFolderRequest } from 'src/app/core/services/media-api.service';
import { MediaLibraryStore } from '../media-library-store.service';
import { TranslateService } from '@ngx-translate/core';

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
        private _translate: TranslateService,
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
                next: (response: string) => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, this._translate.instant('folder-create.success-save'), `'${this._translate.instant('folder-create.success-save-message')}' '${this.model.name}'`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this.model.parentId ? this._router.navigate(['media', this.model.parentId]) : this._router.navigate(['media']));
                }
            });
    }

}
