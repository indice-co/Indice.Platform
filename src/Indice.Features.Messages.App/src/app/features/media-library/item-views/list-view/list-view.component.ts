import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalService, ToastType, ToasterService } from '@indice/ng-components';
import { MediaFile, MediaFolder, FolderContent } from 'src/app/core/services/media-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { MediaLibraryStore } from '../../media-library-store.service';

@Component({
  selector: 'app-list-view',
  templateUrl: './list-view.component.html'
})
export class ListViewComponent implements OnInit {

  private _folderContent?: FolderContent;

  @Input() set folderContent(value: FolderContent | undefined) {
    this._folderContent = value;
    this.items = [];
    if (value?.id) {
      this.items.push({
        id: value.parentId,
        name: '...',
        type: ItemType.Button
      })
    }
    if (value?.folders) {
      value.folders.forEach(f => {
        let folder = <any>f;
        folder.type = ItemType.Folder;
        this.items.push(folder);
      })
    }
    if (value?.files) {
      value.files.forEach(f => {
        let file = <any>f;
        file.type = ItemType.File;
        this.items.push(file);
      })
    }
  }

  public get folderContent(): FolderContent | undefined {
    return this._folderContent;
  }

  @Output() itemDeleted: EventEmitter<void> = new EventEmitter<void>();
  @Output() pageChanged: EventEmitter<number> = new EventEmitter<number>();
  @Output() pageSizeChanged: EventEmitter<number> = new EventEmitter<number>();
  public items: any;
  public ItemType = ItemType;

  public page: number = 1;
  public size: number = 20;
  
  constructor(
    private _router: Router, 
    private _route: ActivatedRoute,
    private _mediaStore: MediaLibraryStore,
    private _modalService: ModalService,
    private _toaster: ToasterService
  ) { }

  ngOnInit(): void {
    this._route.queryParams.subscribe((params) => {
      this.page = params.page ? +params.page : 1;
      this.size = params.pageSize ? +params.pageSize : 20;
    });
  }
  public deleteFolder(folder: MediaFolder) {
    const modal = this._modalService.show(BasicModalComponent, {
      animated: true,
      initialState: {
          title: 'list-view.delete',
          message: `'list-view.delete-folder-warning' '${folder?.name}';`,
          data: folder
      },
      keyboard: true
    });
    modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
            this._mediaStore.deleteFolder(response.result.data.id).subscribe(() => {
                this._toaster.show(ToastType.Success, 'list-view.success-delete', `'list-view.success-delete-folder-message' '${response.result.data.name}'`);
                this.itemDeleted.emit();
            });
        }
    });
  }
  public deleteFile(file: MediaFile) {
    const modal = this._modalService.show(BasicModalComponent, {
      animated: true,
      initialState: {
          title: 'list-view.delete',
          message: `'list-view.delete-file-warning' '${file?.name}';`,
          data: file
      },
      keyboard: true
    });
    modal.onHidden?.subscribe((response: any) => {
        if (response.result?.answer) {
            this._mediaStore.deleteFile(response.result.data.id).subscribe(() => {
                this._toaster.show(ToastType.Success, 'list-view.success-delete', `'list-view.success-delete-file-message' '${response.result.data.name}'`);
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
}

export enum ItemType {
  Button,
  Folder,
  File
}