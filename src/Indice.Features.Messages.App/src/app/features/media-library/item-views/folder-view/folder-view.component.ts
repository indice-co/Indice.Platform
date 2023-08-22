import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FileDetails, Folder, FolderContent } from 'src/app/core/services/media-api.service';
import { settings } from 'src/app/core/models/settings';
import { ModalService, ToasterService, ToastType } from '@indice/ng-components';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { MediaLibraryStore } from '../../media-library-store.service';

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
    private _route: ActivatedRoute,
    private _mediaStore: MediaLibraryStore,
    private _modalService: ModalService,
    private _toaster: ToasterService
  ) { }

  ngOnInit(): void {
    this._route.queryParams.subscribe((params) => {
      this.page = params.page ? +params.page : this.page;
      this.size = params.pageSize ? +params.pageSize : this.size;
    });
  }

  public getImageUrl(file: FileDetails) {
    switch (file.fileExtension) {
      case '.csv': 
        return '../../../../assets/images/csv-icon.png';
      case '.docx': 
        return '../../../../assets/images/word-icon.png';
      case '.xlsx': 
        return '../../../../assets/images/excel-icon.png';
      case '.pdf': 
        return '../../../../assets/images/pdf-icon.png';
      case '.pptx': 
        return '../../../../assets/images/pptx-icon.png';
      default:
        return `${settings.api_url}${file.permaLink}`;
    }
    
  }
  public deleteFolder(folder: Folder) {
    const modal = this._modalService.show(BasicModalComponent, {
      animated: true,
      initialState: {
          title: 'Διαγραφή',
          message: `Είστε σίγουρος ότι θέλετε να διαγράψετε τον φάκελο '${folder?.name}';`,
          data: folder
      },
      keyboard: true
    });
    modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
            this._mediaStore.deleteFolder(response.result.data.id).subscribe(() => {
                this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Ο φάκελος με τίτλο '${response.result.data.name}' διαγράφηκε με επιτυχία.`);
                this.itemDeleted.emit();
            });
        }
    });
  }
  public deleteFile(file: FileDetails) {
    const modal = this._modalService.show(BasicModalComponent, {
      animated: true,
      initialState: {
          title: 'Διαγραφή',
          message: `Είστε σίγουρος ότι θέλετε να διαγράψετε το αρχείο '${file?.name}';`,
          data: file
      },
      keyboard: true
    });
    modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
            this._mediaStore.deleteFile(response.result.data.id).subscribe(() => {
                this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Το αρχείο με τίτλο '${response.result.data.name}' διαγράφηκε με επιτυχία.`);
                this.itemDeleted.emit();
            });
        }
    });
  }
  public editFile(file: FileDetails) {
    this._router.navigate(['media', file.folderId ? file.folderId : 'root', file.id ]);
  }
  public editFolder(folder: Folder) {
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
}
