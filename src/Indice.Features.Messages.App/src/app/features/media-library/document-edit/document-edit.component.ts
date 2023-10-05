import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { HeaderMetaItem, ViewLayoutComponent } from '@indice/ng-components';
import { MediaFile, MediaFolder } from 'src/app/core/services/media-api.service';
import { MediaLibraryStore } from '../media-library-store.service';
import { map, mergeMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { settings } from 'src/app/core/models/settings';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';

@Component({
  selector: 'app-document-edit',
  templateUrl: './document-edit.component.html'
})
export class DocumentEditComponent implements OnInit {
  @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;
    private _documentId?: string;

    constructor(
        private _activatedRoute: ActivatedRoute,
        private _mediaStore: MediaLibraryStore,
        private _router: Router,
        private _changeDetector: ChangeDetectorRef,
        private _fileUtilitiesService: FileUtilitiesService
    ) { }

    public submitInProgress = false;
    public file: MediaFile | undefined;
    public parentFolderName: string | undefined;
    public metaItems: HeaderMetaItem[] = [];

    public ngOnInit(): void {
        this._documentId = this._activatedRoute.snapshot.params['documentId'];
        if (this._documentId) {
            this._mediaStore.getFileDetails(this._documentId!)
              .pipe(mergeMap((file: MediaFile) => {
                  this.file = file;
                  this._layout.title = `Αρχείο - ${file.name}`;
                  return this.file.folderId ? this._mediaStore.getFolderDetails(this.file.folderId).pipe(map((folder: MediaFolder) => folder?.name)) : of(undefined)
              }))
              .subscribe((folderName: string | undefined) => {
                this.parentFolderName = this.file?.folderId ? folderName : "-";
              });
        }
    }

    public openEditPane(action: string): void {
      this._router.navigate(['', { outlets: { rightpane: ['edit-file'] } }], { queryParams: { action: action } });
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }

    public preview(): void {
      window.open(this.file?.permaLink, '_blank');
    }

    public async download() {
      await this._fileUtilitiesService.downloadFile(this.file);
    }
}
