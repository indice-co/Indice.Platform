import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { FolderContent, FolderTreeStructure, MediaApiClient } from 'src/app/core/services/media-api.service';
import { IAttachment } from 'src/app/shared/components/file-upload/file-upload.component';
import { MediaLibraryStore } from './media-library-store.service';
import { mergeMap, tap, map, switchMap, takeUntil, filter, startWith, skipUntil } from 'rxjs/operators';
import { combineLatest } from 'rxjs';

@Component({
  selector: 'app-media-library',
  templateUrl: './media-library.component.html'
})
export class MediaLibraryComponent implements OnInit {

  public structure?: FolderTreeStructure;
  public folderContent?: FolderContent;
  public useBreadcrumb: boolean = true;
  public file: IAttachment | undefined;
  public currentFolderId: string | undefined;

  public page = 1;
  public size = 20;
  public view: 'folder' | 'list' = 'folder';

  constructor(
    private _mediaStore: MediaLibraryStore, 
    private _media: MediaApiClient, 
    private _activatedRoute: ActivatedRoute, 
    private _router: Router
  ) { }

  ngOnInit(): void {
    this.view = localStorage.getItem('view') == 'list' ? 'list' : 'folder';
    this._activatedRoute.params.pipe(mergeMap((params) => {
      this.currentFolderId = params.folderId;
      return this._activatedRoute.queryParams.pipe(filter((x: any) => this._activatedRoute.snapshot.params.folderId == this.currentFolderId ? this._activatedRoute.snapshot.queryParams?.page ? x.page != undefined : true : false), mergeMap((queryParams) => {
        this.page = queryParams?.page ?? 1;
        this.size = queryParams?.pageSize ?? 20;
        return this._mediaStore.getFolderContent(this.currentFolderId, this.page, this.size)
          .pipe(tap((content) => {
            this.folderContent = content;
            this.loadStructure();
          }));
      }));
    })).subscribe();
  }

  public onFileChange(file: IAttachment | undefined) {
    this.file = file;
  }
  public createFolder() {
    this._router.navigate(['', { outlets: { rightpane: 'create-folder' } }]);
  }
  public uploadFile() {
    this._router.navigate(['', { outlets: { rightpane: 'upload-file' } }]);
  }
  public loadContent() {
    this._media.getFolderContent(this.currentFolderId, this.page, this.size)
        .subscribe((content) => {
          this.folderContent = content;
        });
  }
  public loadStructure() {
    this._mediaStore.getFolderStructure()
      .subscribe((structure) => {
        if (structure != this.structure) {
          this.structure = structure;
        }
      });
  }
  public selectView(view: 'folder' | 'list') {
    this.view = view;
    localStorage.setItem('view', view);
  }
  public onItemDeleted() {
    if (this.page !== 1 || this.size !== 20) {
      this.page = 1;
      this.size = 20;
      this._router.navigate([], {
        relativeTo: this._activatedRoute,
        queryParams: {
          page: this.page,
          pageSize: this.size
        },
        queryParamsHandling: 'merge'
      });
    }
    else {
      this.loadContent();
      this.loadStructure();
    }
  }
  public onPageChanged(page: number) {
    this.page = page;
    this._mediaStore.getFolderContent(this.currentFolderId, this.page, this.size)
          .pipe(tap((content) => {
            this.folderContent = content;
            this.loadStructure();
          }));
  }
  public onPageSizeChanged(size: number) {
    this.size = size;
    this._mediaStore.getFolderContent(this.currentFolderId, this.page, this.size)
          .pipe(tap((content) => {
            this.folderContent = content;
            this.loadStructure();
          }));
  }
}
