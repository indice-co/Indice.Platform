import { Injectable, inject, signal } from '@angular/core';
import { Observable, filter, map, switchMap, tap } from 'rxjs';
import { FolderTree, FolderTreeStructure, MediaApiClient, MediaFile, MediaFolder } from 'src/app/core/services/media-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';

/** RefreshBus scope notified whenever folders or files of the media library change. */
export const MEDIA_SCOPE = 'media';

export type MediaView = 'grid' | 'list';

const VIEW_STORAGE_KEY = 'media-library.view';

/**
 * Shared state and actions of the media library: the folder tree, the preferred view and the common item actions
 * (delete with confirmation, copy link, open, download). Mutations notify {@link MEDIA_SCOPE} on the RefreshBus.
 */
@Injectable({ providedIn: 'root' })
export class MediaLibraryStore {
  private readonly _api = inject(MediaApiClient);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  private readonly _files = inject(FileUtilitiesService);

  public readonly structure = signal<FolderTreeStructure | undefined>(undefined);
  public readonly structureLoading = signal(false);
  public readonly view = signal<MediaView>(readView());

  public setView(view: MediaView): void {
    this.view.set(view);
    try {
      localStorage.setItem(VIEW_STORAGE_KEY, view);
    } catch { /* storage unavailable */ }
  }

  public loadStructure(): void {
    this.structureLoading.set(true);
    this._api.getFolderStructure().subscribe({
      next: structure => {
        this.structure.set(structure);
        this.structureLoading.set(false);
      },
      error: () => this.structureLoading.set(false)
    });
  }

  /** Folders from the root down to (and including) `folderId`, according to the loaded tree. */
  public pathTo(folderId: string | undefined): MediaFolder[] {
    if (!folderId) {
      return [];
    }
    const search = (nodes: FolderTree[] | undefined, trail: MediaFolder[]): MediaFolder[] | undefined => {
      for (const tree of nodes ?? []) {
        if (!tree.node) {
          continue;
        }
        const next = [...trail, tree.node];
        if (tree.node.id === folderId) {
          return next;
        }
        const found = search(tree.children, next);
        if (found) {
          return found;
        }
      }
      return undefined;
    };
    return search(this.structure()?.items, []) ?? [];
  }

  /** Router link of a folder page (the library root when `folderId` is empty). */
  public folderLink(folderId: string | undefined): string[] {
    return folderId ? ['/media', folderId] : ['/media'];
  }

  /** Router link of a file details page. */
  public fileLink(file: MediaFile): string[] {
    return ['/media', file.folderId || 'root', file.id];
  }

  public notifyChanged(): void {
    this._refresh.notify(MEDIA_SCOPE);
  }

  public deleteFolder(folder: MediaFolder): Observable<void> {
    return this._dialog
      .confirm({ title: 'MediaLibrary.DeleteFolderTitle', message: 'MediaLibrary.DeleteFolderConfirmMessage', messageParams: { name: folder.name }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._api.deleteFolder(folder.id!)),
        tap(() => {
          this._toaster.show(ToastType.Success, 'MediaLibrary.DeleteFolderSuccessTitle', 'MediaLibrary.DeleteFolderSuccessMessage', undefined, { name: folder.name });
          this.notifyChanged();
        })
      );
  }

  public deleteFile(file: MediaFile): Observable<void> {
    return this._dialog
      .confirm({ title: 'MediaLibrary.DeleteFileTitle', message: 'MediaLibrary.DeleteFileConfirmMessage', messageParams: { name: file.name }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._api.deleteFile(file.id)),
        map(() => undefined),
        tap(() => {
          this._toaster.show(ToastType.Success, 'MediaLibrary.DeleteFileSuccessTitle', 'MediaLibrary.DeleteFileSuccessMessage', undefined, { name: file.name });
          this.notifyChanged();
        })
      );
  }

  public copyLink(file: MediaFile): void {
    this._files.copyPathToClipboard(file.permaLink).then(
      () => this._toaster.show(ToastType.Success, 'MediaLibrary.CopyLinkTitle', 'MediaLibrary.CopyLinkSuccessMessage', undefined, { name: file.name }),
      () => this._toaster.show(ToastType.Error, 'MediaLibrary.CopyLinkErrorTitle', 'MediaLibrary.CopyLinkErrorMessage')
    );
  }

  public copyPath(file: MediaFile): void {
    this._files.copyPathToClipboard(file.path).then(
      () => this._toaster.show(ToastType.Success, 'MediaLibrary.CopyPathTitle', 'MediaLibrary.CopyPathSuccessMessage', undefined, { path: file.path }),
      () => this._toaster.show(ToastType.Error, 'MediaLibrary.CopyPathErrorTitle', 'MediaLibrary.CopyPathErrorMessage')
    );
  }

  public open(file: MediaFile): void {
    this._files.openFileInNewTab(file);
  }

  public download(file: MediaFile): void {
    this._files.downloadFile(file).catch(() => this._files.openFileInNewTab(file));
  }
}

function readView(): MediaView {
  try {
    return localStorage.getItem(VIEW_STORAGE_KEY) === 'list' ? 'list' : 'grid';
  } catch {
    return 'grid';
  }
}
