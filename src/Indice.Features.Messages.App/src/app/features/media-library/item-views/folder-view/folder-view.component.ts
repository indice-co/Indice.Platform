import { Component, EventEmitter, Input, OnInit, OnDestroy, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MediaFile, MediaFolder, FolderContent } from 'src/app/core/services/media-api.service';
import { ModalService, ToastType } from '@indice/ng-components';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { MediaLibraryStore } from '../../media-library-store.service';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';
import { AppTranslatedToaster } from '../../../../shared/services/app-translated-toaster';
import { TranslateService } from '@ngx-translate/core';
import { Subject, combineLatest } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-folder-view',
  templateUrl: './folder-view.component.html'
})
export class FolderViewComponent implements OnInit, OnDestroy {

  public page: number = 1;
  public size: number = 20;

  private _folderContent?: FolderContent;
  private _destroy$ = new Subject<void>();

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
    private _route: ActivatedRoute,
    private _mediaStore: MediaLibraryStore,
    private _modalService: ModalService,
    private _toaster: AppTranslatedToaster,
    private _fileUtilitiesService: FileUtilitiesService,
    private _translate: TranslateService
  ) { }

  ngOnInit(): void {
    this._route.queryParams.subscribe((params) => {
      this.page = params.page ? +params.page : this.page;
      this.size = params.pageSize ? +params.pageSize : this.size;
    });
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public getImageUrl(file: MediaFile, size?: number) {
    return this._fileUtilitiesService.getCoverImageUrl(file, size);
  }

  public deleteFolder(folder: MediaFolder) {
    combineLatest([
      this._translate.get('MediaLibrary.Delete'),
      this._translate.get('MediaLibrary.DeleteFolderConfirmMessage', { name: folder?.name })
    ]).pipe(takeUntil(this._destroy$)).subscribe(([title, message]) => {
      const modal = this._modalService.show(BasicModalComponent, {
        animated: true,
        initialState: {
          title: title || 'MediaLibrary.Delete',
          message: message || 'MediaLibrary.DeleteFolderConfirmMessage',
          data: folder
        },
        keyboard: true
      });
      modal.onHidden?.pipe(takeUntil(this._destroy$)).subscribe((response: any) => {
        if (response.result?.answer) {
          this._mediaStore.deleteFolder(response.result.data.id).subscribe(() => {
            this._toaster.show(ToastType.Success, 'MediaLibrary.DeleteFolderSuccessTitle', 'MediaLibrary.DeleteFolderSuccessMessage', undefined, { name: response.result.data.name }); // toaster single line
            this.itemDeleted.emit();
          });
        }
      });
    });
  }

  public deleteFile(file: MediaFile) {
    combineLatest([
      this._translate.get('MediaLibrary.Delete'),
      this._translate.get('MediaLibrary.DeleteFileConfirmMessage', { name: file?.name })
    ]).pipe(takeUntil(this._destroy$)).subscribe(([title, message]) => {
      const modal = this._modalService.show(BasicModalComponent, {
        animated: true,
        initialState: {
          title: title || 'MediaLibrary.Delete',
          message: message || 'MediaLibrary.DeleteFileConfirmMessage',
          data: file
        },
        keyboard: true
      });
      modal.onHidden?.pipe(takeUntil(this._destroy$)).subscribe((response: any) => {
        if (response.result?.answer) {
          this._mediaStore.deleteFile(response.result.data.id).subscribe(() => {
            this._toaster.show(ToastType.Success, 'MediaLibrary.DeleteFileSuccessTitle', 'MediaLibrary.DeleteFileSuccessMessage', undefined, { name: response.result.data.name }); // toaster single line
            this.itemDeleted.emit();
          });
        }
      });
    });
  }

  public editFile(file: MediaFile) {
    this._router.navigate(['media', file.folderId ? file.folderId : 'root', file.id]);
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

  public copyToClipboard(file: MediaFile): void {
    this._fileUtilitiesService.copyPathToClipboard(file.permaLink)
      .then(() => {
        this._toaster.show(ToastType.Success, 'MediaLibrary.CopyLinkTitle', 'MediaLibrary.CopyLinkSuccessMessage', undefined,{ name: file.name }); // toaster single line
      })
      .catch(() => {
        this._toaster.show(ToastType.Error, 'MediaLibrary.CopyLinkErrorTitle', 'MediaLibrary.CopyLinkErrorMessage'); // toaster single line
      });
  }
}
