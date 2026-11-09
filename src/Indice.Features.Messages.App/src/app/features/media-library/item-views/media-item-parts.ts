import { Component, computed, inject, input, linkedSignal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import {
  LucideCopy, LucideDownload, LucideDynamicIcon, LucideEllipsisVertical, LucideExternalLink, LucideFileArchive, LucideFileSpreadsheet,
  LucideFileText, LucideFolderOpen, LucideInfo, LucidePencil, LucidePresentation, LucideTrash
} from '@lucide/angular';
import { MediaFile, MediaFolder } from 'src/app/core/services/media-api.service';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { MediaLibraryStore } from '../media-library-store.service';

/** Closes an open daisyUI (focus based) dropdown. */
export function closeDropdown(): void {
  (document.activeElement as HTMLElement | null)?.blur();
}

/** Image thumbnail of a media file, falling back to a file type icon for documents or images that fail to load. */
@Component({
  selector: 'app-media-file-thumb',
  imports: [LucideDynamicIcon],
  host: { class: 'grid place-items-center overflow-hidden bg-base-200' },
  template: `
    @if (url() && !broken()) {
      <img class="size-full" [class]="fit() === 'cover' ? 'object-cover' : 'object-contain'" [src]="url()" [alt]="file().name" loading="lazy"
        (error)="broken.set(true)" />
    } @else {
      <svg [lucideIcon]="icon()" [class]="iconClass() + ' ' + tone()" [attr.aria-hidden]="true"></svg>
    }
  `
})
export class MediaFileThumbComponent {
  private readonly _files = inject(FileUtilitiesService);

  public readonly file = input.required<MediaFile>();
  /** Thumbnail size requested from the server; omit for the original image. */
  public readonly size = input<number>();
  public readonly fit = input<'cover' | 'contain'>('cover');
  public readonly iconClass = input('size-10');

  protected readonly url = computed(() => this._files.getCoverImageUrl(this.file(), this.size()));
  protected readonly icon = computed(() => this._files.getFileIcon(this.file()));
  protected readonly broken = linkedSignal({ source: this.file, computation: () => false });
  protected readonly tone = computed(() => {
    const icon = this.icon();
    if (icon === LucideFileText) {
      return (this.file().fileExtension ?? '').toLowerCase().endsWith('pdf') ? 'text-error' : 'text-info';
    }
    if (icon === LucideFileSpreadsheet) {
      return 'text-success';
    }
    if (icon === LucidePresentation) {
      return 'text-warning';
    }
    if (icon === LucideFileArchive) {
      return 'text-secondary';
    }
    return 'text-base-content/40';
  });
}

/** Context menu (daisyUI dropdown) with the actions of a folder. */
@Component({
  selector: 'app-media-folder-menu',
  imports: [RouterLink, TranslatePipe, LucideEllipsisVertical, LucideFolderOpen, LucidePencil, LucideTrash],
  template: `
    <div class="dropdown dropdown-end">
      <button type="button" tabindex="0" class="btn btn-ghost btn-xs btn-square" [attr.aria-label]="'MediaLibrary.ActionsColumn' | translate"
        (click)="$event.stopPropagation()">
        <svg lucideEllipsisVertical class="size-4"></svg>
      </button>
      <ul tabindex="-1" class="dropdown-content menu menu-sm z-20 mt-1 w-48 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
        <li><a [routerLink]="store.folderLink(folder().id)" (click)="close()"><svg lucideFolderOpen class="size-4"></svg>{{ 'MediaLibrary.OpenFolder' | translate }}</a></li>
        <li><a [routerLink]="pane.link(['edit-folder', folder().id!])" (click)="close()"><svg lucidePencil class="size-4"></svg>{{ 'MediaLibrary.EditFolderTitle' | translate }}</a></li>
        <li class="text-error"><button type="button" (click)="delete()"><svg lucideTrash class="size-4"></svg>{{ 'MediaLibrary.DeleteFolderTitle' | translate }}</button></li>
      </ul>
    </div>
  `
})
export class MediaFolderMenuComponent {
  protected readonly store = inject(MediaLibraryStore);
  protected readonly pane = inject(SidePaneService);
  public readonly folder = input.required<MediaFolder>();

  protected close(): void {
    closeDropdown();
  }

  protected delete(): void {
    closeDropdown();
    this.store.deleteFolder(this.folder()).subscribe();
  }
}

/** Context menu (daisyUI dropdown) with the actions of a file. */
@Component({
  selector: 'app-media-file-menu',
  imports: [RouterLink, TranslatePipe, LucideCopy, LucideDownload, LucideEllipsisVertical, LucideExternalLink, LucideInfo, LucidePencil, LucideTrash],
  template: `
    <div class="dropdown dropdown-end">
      <button type="button" tabindex="0" class="btn btn-ghost btn-xs btn-square" [attr.aria-label]="'MediaLibrary.ActionsColumn' | translate"
        (click)="$event.stopPropagation()">
        <svg lucideEllipsisVertical class="size-4"></svg>
      </button>
      <ul tabindex="-1" class="dropdown-content menu menu-sm z-20 mt-1 w-52 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
        <li><a [routerLink]="store.fileLink(file())" (click)="close()"><svg lucideInfo class="size-4"></svg>{{ 'MediaLibrary.FileDetails' | translate }}</a></li>
        <li><a [routerLink]="pane.link(['edit-file', file().id])" (click)="close()"><svg lucidePencil class="size-4"></svg>{{ 'MediaLibrary.EditFileTitle' | translate }}</a></li>
        <li><button type="button" (click)="run('copy')"><svg lucideCopy class="size-4"></svg>{{ 'MediaLibrary.CopyLinkTitle' | translate }}</button></li>
        <li><button type="button" (click)="run('open')"><svg lucideExternalLink class="size-4"></svg>{{ 'MediaLibrary.OpenInNewTabTitle' | translate }}</button></li>
        <li><button type="button" (click)="run('download')"><svg lucideDownload class="size-4"></svg>{{ 'MediaLibrary.DownloadFile' | translate }}</button></li>
        <li class="text-error"><button type="button" (click)="run('delete')"><svg lucideTrash class="size-4"></svg>{{ 'MediaLibrary.DeleteFileTitle' | translate }}</button></li>
      </ul>
    </div>
  `
})
export class MediaFileMenuComponent {
  protected readonly store = inject(MediaLibraryStore);
  protected readonly pane = inject(SidePaneService);
  public readonly file = input.required<MediaFile>();

  protected close(): void {
    closeDropdown();
  }

  protected run(action: 'copy' | 'open' | 'download' | 'delete'): void {
    closeDropdown();
    const file = this.file();
    switch (action) {
      case 'copy': this.store.copyLink(file); break;
      case 'open': this.store.open(file); break;
      case 'download': this.store.download(file); break;
      case 'delete': this.store.deleteFile(file).subscribe(); break;
    }
  }
}
