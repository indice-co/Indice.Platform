import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MediaFile, MediaFolder, FolderContent } from 'src/app/core/services/media-api.service';
import { ModalService, ToasterService, ToastType } from '@indice/ng-components';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { MediaLibraryStore } from '../../media-library-store.service';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-folder-view',
  templateUrl: './folder-view.component.html'
})
export class FolderViewComponent implements OnInit {

  public page: number = 1;
  public size: number = 20;

  private _folderContent?: FolderContent;

  @Input() set folderContent(value: FolderContent | undefined) {
    this._folderContent = value;
  }

  public get folderContent(): FolderContent | undefined {
    return this._folderContent;
  }

  @Output() itemDeleted: EventEmitter<void> = new EventEmitter<void>();
  @Output() pageChanged: EventEmitter<number> = new EventEmitter<number>();
  @Output() pageSizeChanged: EventEmitter<number> = new EventEmitter<number>();

  constructor(
    private _router: Router,
    private _translate: TranslateService,
    private _route: ActivatedRoute,
    private _mediaStore: MediaLibraryStore,
    private _modalService: ModalService,
    private _toaster: ToasterService,
    private _fileUtilitiesService: FileUtilitiesService
  ) { }

  ngOnInit(): void {
    this._route.queryParams.subscribe((params) => {
      this.page = params.page ? +params.page : this.page;
      this.size = params.pageSize ? +params.pageSize : this.size;
    });
  }

  public getImageUrl(file: MediaFile) {
    return this._fileUtilitiesService.getCoverImageUrl(file);
  }
  public deleteFolder(folder: MediaFolder) {
    const modal = this._modalService.show(BasicModalComponent, {
      animated: true,
      initialState: {
          title: this._translate.instant('folder-view.delete'),
          message: `'${this._translate.instant('folder-view.delete-folder-warning')}' '${folder?.name}';`,
          data: folder
      },
      keyboard: true
    });
    modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
            this._mediaStore.deleteFolder(response.result.data.id).subscribe(() => {
                this._toaster.show(ToastType.Success, this._translate.instant('folder-view.success-delete'), `'${this._translate.instant('folder-view.success-delete-folder-message')}' '${response.result.data.name}'`);
                this.itemDeleted.emit();
            });
        }
    });
  }
  public deleteFile(file: MediaFile) {
    const modal = this._modalService.show(BasicModalComponent, {
      animated: true,
      initialState: {
          title: this._translate.instant('folder-view.delete'),
          message: `'${this._translate.instant('folder-view.delete-file-warning')}' '${file?.name}';`,
          data: file
      },
      keyboard: true
    });
    modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
            this._mediaStore.deleteFile(response.result.data.id).subscribe(() => {
                this._toaster.show(ToastType.Success, this._translate.instant('folder-view.success-delete'), `'${this._translate.instant('folder-view.success-delete-file-message')}' '${response.result.data.name}'`);
                this.itemDeleted.emit();
            });
        }
    });
  }
  public editFile(file: MediaFile) {
    this._router.navigate(['media', file.folderId ? file.folderId : 'root', file.id ]);
  }
  public editFolder(folder: MediaFolder) {
    this._router.navigate(['', { outlets: { rightpane: ['edit-folder', folder.id] } }]);
  }
  public goToFolder(id: string | undefined) {
    id ? this._router.navigate(['media', id]) : this._router.navigate(['media'])
  }
  public loadContent(page?: number, size?: number) {
    this.page = page ?? 1;
    this.size = size ?? 20;
    this._router.navigate([], {
      relativeTo: this._route,
      queryParams: {
        page: this.page,
        pageSize: this.size
      },
      queryParamsHandling: 'merge'
    });
  }
  public copyToClipboard(file: MediaFile) {
    this._fileUtilitiesService.copyPermaLinkToClipboard(file);
    this._toaster.show(ToastType.Success, this._translate.instant('folder-view.copy-link'), `'${this._translate.instant('folder-view.success-copy-link')}' '${file.name}'`);
  }
}
