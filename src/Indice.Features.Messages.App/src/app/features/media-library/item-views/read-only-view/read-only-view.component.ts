import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { tap } from 'rxjs/operators';
import { MediaFile, FolderContent, MediaApiClient } from 'src/app/core/services/media-api.service';
import { ToastType, ToasterService } from '@indice/ng-components';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';

@Component({
  selector: 'app-read-only-view',
  templateUrl: './read-only-view.component.html'
})
export class ReadOnlyViewComponent implements OnInit {

  private _currentFolderId: string | undefined = undefined; 

  public page: number = 1;
  public size: number = 20;
  public selectedFile?: MediaFile;
  public folderContent?: FolderContent;

  @Output() fileSelected: EventEmitter<string> = new EventEmitter<string>();

  constructor(
    private _mediaClient: MediaApiClient, 
    private _toaster: ToasterService,
    private _fileUtilitiesService: FileUtilitiesService
  ) { }

  ngOnInit(): void {
    this.loadContent();
  }

  public getImageUrl(file: MediaFile) {
    return this._fileUtilitiesService.getCoverImageUrl(file);
  }

  public loadContent(page?: number, size?: number) {
    this.page = page ?? 1;
    this.size = size ?? 20;
    this._mediaClient.getFolderContent(this._currentFolderId, this.page, this.size)
      .pipe(tap((content) => {
        this.folderContent = content;
      })).subscribe();
  }

  public goToFolder(id: string | undefined) {
    this._currentFolderId = id;
    this.loadContent(1, 20);
  }

  public selectFile(file: MediaFile) {
    this.selectedFile = file;
    this.fileSelected.emit(this.selectedFile.permaLink);
  }

  public copyToClipboard(file: MediaFile): void {
    this._fileUtilitiesService.copyPathToClipboard(file.permaLink)
      .then(() => {
        this._toaster.show(ToastType.Success, 'Αντιγραφή συνδέσμου', `Ο σύνδεσμος του αρχείου '${file.name}' αντιγράφηκε με επιτυχία.`);
      })
      .catch((err) => {
        this._toaster.show(ToastType.Error, 'Αποτυχία αντιγραφής', 'Ο σύνδεσμος του αρχείου δεν μπόρεσε να αντιγραφεί στο πρόχειρο.');
      });
  }

  public openInNewTab(file: MediaFile) {
    this._fileUtilitiesService.openFileInNewTab(file)
  }
}
