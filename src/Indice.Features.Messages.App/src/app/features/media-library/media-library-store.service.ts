import { Injectable } from '@angular/core';

import { AsyncSubject, Observable, of } from 'rxjs';
import { map, mergeMap, tap } from 'rxjs/operators';
import { MediaApiClient, FolderContent, CreateFolderRequest, FileParameter, MediaFile, MediaFolder, UpdateFileMetadataRequest, UpdateFolderRequest, FolderTreeStructure, CreateFolderResponse } from 'src/app/core/services/media-api.service';

@Injectable({
    providedIn: 'root'
})
export class MediaLibraryStore {
    private _folderStructure: AsyncSubject<FolderTreeStructure> | undefined;
    private _folderContent: AsyncSubject<FolderContent> | undefined;
    private _folderIdChanged = false;
    private _currentFolderId: string | undefined;
    private _selectedFile: AsyncSubject<MediaFile> | undefined;
    private _fileIdChanged = false;
    private _currentFileId: string | undefined;
    private _folders: AsyncSubject<MediaFolder[]> | undefined;
    private _page: number | undefined;
    private _size: number | undefined;

    constructor(
        private _api: MediaApiClient
    ) { }

    public getFolderContent(folderId: string | undefined, page: number, size: number): Observable<FolderContent> {
        this._folderIdChanged = this._currentFolderId !== folderId;
        let paginationChanged = this._page !== page || this._size !== size;
        this._page = page;
        this._size = size;
        this._currentFolderId = folderId;
        if (!this._folderContent || this._folderIdChanged || paginationChanged) {
            this._folderContent = new AsyncSubject<FolderContent>();
            this._api
                .getFolderContent(folderId, page, size)
                .subscribe((folderContent: FolderContent) => {
                    this._folderContent?.next(folderContent);
                    this._folderContent?.complete();
                });
        }
        return this._folderContent;
    }

    public getFolderStructure(): Observable<FolderTreeStructure> {
        if (!this._folderStructure) {
            this._folderStructure = new AsyncSubject<FolderTreeStructure>();
            this._api
                .getFolderStructure()
                .subscribe((folderContent: FolderTreeStructure) => {
                    this._folderStructure?.next(folderContent);
                    this._folderStructure?.complete();
                });
        }
        return this._folderStructure;
    }

    public getFolderDetails(folderId: string): Observable<MediaFolder> {
        if (this._folderContent) {
            return this._folderContent
                .pipe(mergeMap((content) => {
                    if (this._currentFolderId === folderId) {
                        return of(content);
                    }
                    let folderDetails = content.folders?.find(f => f.id == folderId);
                    if (folderDetails) {
                        return of(folderDetails);
                    }
                    return this._api.getFolderById(folderId)
                }))
        }
        return this._api.getFolderById(folderId);
    }

     public createFolder(request: CreateFolderRequest): Observable<CreateFolderResponse> {
        return this._api
            .createFolder(request)
            .pipe(
                tap(_ => this._folderContent = undefined),
                tap(_ => this._folders = undefined),
                tap(_ => this._folderStructure = undefined)
            );
    }

    public updateFolder(folderId: string, request: UpdateFolderRequest): Observable<void> {
        return this._api
            .updateFolder(folderId, request)
            .pipe(
                tap(_ => this._folderContent = undefined),
                tap(_ => this._folders = undefined),
                tap(_ => this._folderStructure = undefined)
            );
    }

    public deleteFolder(id: string) {
        return this._api
            .deleteFolder(id)
            .pipe(
                tap(_ => this._folderContent = undefined),
                tap(_ => this._folders = undefined),
                tap(_ => this._folderStructure = undefined)
            );
    }

    public uploadFile(folderId?: string | undefined, file?: FileParameter) {
        return this._api
            .uploadFile(folderId, file)
            .pipe(
                tap(_ => this._folderContent = undefined),
                tap(_ => this._folderStructure = undefined)
            );
    }

    public deleteFile(id: string) {
        return this._api
            .deleteFile(id)
            .pipe(
                tap(_ => this._folderContent = undefined),
                tap(_ => this._selectedFile = undefined),
                tap(_ => this._folderStructure = undefined)
            );
    }

    public getFileDetails(id: string): Observable<MediaFile> {
        this._fileIdChanged = this._currentFileId !== id;
        this._currentFileId = id;
        if (!this._selectedFile || this._fileIdChanged) {
            this._selectedFile = new AsyncSubject<MediaFile>();
            this._api
                .getFileDetails(id)
                .subscribe((mediaFile: MediaFile) => {
                    this._selectedFile?.next(mediaFile);
                    this._selectedFile?.complete();
                });
        }
        return this._selectedFile;
    }

    public updateFileMetadata(id: string, request: UpdateFileMetadataRequest) {
        return this._api.updateFileMetadata(id, request)
            .pipe(
                tap(_ => this._folderContent = undefined),
                tap(_ => this._selectedFile = undefined)
            );
    }

    public listFolders() {
        if (!this._folders) {
            this._folders = new AsyncSubject<MediaFolder[]>();
            this._api
                .listFolders()
                .subscribe((folders: MediaFolder[]) => {
                    this._folders?.next(folders);
                    this._folders?.complete();
                });
        }
        return this._folders;
    }
}
