import { DatePipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideCopy, LucideCornerLeftUp, LucideFolder, LucidePencil, LucideTrash } from '@lucide/angular';
import { FolderContent, MediaFile, MediaFolder } from 'src/app/core/services/media-api.service';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { MediaLibraryStore } from '../../media-library-store.service';
import { MediaFileThumbComponent } from '../media-item-parts';

/** Table view of a folder's content. */
@Component({
  selector: 'app-list-view',
  imports: [DatePipe, RouterLink, TranslatePipe, MediaFileThumbComponent, LucideCopy, LucideCornerLeftUp, LucideFolder, LucidePencil, LucideTrash],
  host: { class: 'block overflow-x-auto' },
  template: `
    <table class="table">
      <thead>
        <tr>
          <th>{{ 'MediaLibrary.NameColumn' | translate }}</th>
          <th class="hidden md:table-cell">{{ 'MediaLibrary.TypeColumn' | translate }}</th>
          <th class="hidden sm:table-cell">{{ 'MediaLibrary.SizeLabel' | translate }}</th>
          <th class="hidden lg:table-cell">{{ 'MediaLibrary.CreatedByColumn' | translate }}</th>
          <th class="hidden lg:table-cell">{{ 'MediaLibrary.CreatedAtColumn' | translate }}</th>
          <th class="w-28 text-right"><span class="sr-only">{{ 'MediaLibrary.ActionsColumn' | translate }}</span></th>
        </tr>
      </thead>
      <tbody>
        @if (content().id) {
          <tr class="hover:bg-base-200/60">
            <td colspan="6">
              <a class="flex items-center gap-3 text-base-content/70 hover:text-base-content" [routerLink]="store.folderLink(content().parentId)">
                <span class="grid size-8 place-items-center rounded-field bg-base-200"><svg lucideCornerLeftUp class="size-4"></svg></span>
                {{ 'MediaLibrary.ParentFolderTitle' | translate }}
              </a>
            </td>
          </tr>
        }
        @for (folder of content().folders; track folder.id) {
          <tr class="hover:bg-base-200/60">
            <td>
              <a class="flex items-center gap-3 font-semibold hover:text-primary" [routerLink]="store.folderLink(folder.id)">
                <span class="grid size-8 shrink-0 place-items-center rounded-field bg-warning/15 text-warning"><svg lucideFolder class="size-4"></svg></span>
                <span class="max-w-xs truncate">{{ folder.name || ('MediaLibrary.EmptyValueIndicator' | translate) }}</span>
              </a>
            </td>
            <td class="hidden text-base-content/70 md:table-cell">{{ 'MediaLibrary.FolderType' | translate }}</td>
            <td class="hidden text-base-content/70 sm:table-cell tabular">
              {{ 'MediaLibrary.FolderCounts' | translate: { folders: folder.subFoldersCount ?? 0, files: folder.filesCount ?? 0 } }}
            </td>
            <td class="hidden lg:table-cell">{{ folder.createdBy || ('MediaLibrary.EmptyValueIndicator' | translate) }}</td>
            <td class="hidden whitespace-nowrap lg:table-cell">{{ folder.createdAt | date: 'medium' }}</td>
            <td class="text-right whitespace-nowrap">
              <span class="tooltip" [attr.data-tip]="'MediaLibrary.EditFolderTitle' | translate">
                <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['edit-folder', folder.id!])" [attr.aria-label]="'MediaLibrary.EditFolderTitle' | translate">
                  <svg lucidePencil class="size-4"></svg>
                </a>
              </span>
              <span class="tooltip tooltip-left" [attr.data-tip]="'MediaLibrary.DeleteFolderTitle' | translate">
                <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="deleteFolder(folder)" [attr.aria-label]="'MediaLibrary.DeleteFolderTitle' | translate">
                  <svg lucideTrash class="size-4"></svg>
                </button>
              </span>
            </td>
          </tr>
        }
        @for (file of content().files; track file.id) {
          <tr class="hover:bg-base-200/60">
            <td>
              <a class="flex items-center gap-3 font-medium hover:text-primary" [routerLink]="store.fileLink(file)">
                <app-media-file-thumb class="size-8 shrink-0 rounded-field" [file]="file" iconClass="size-4" />
                <span class="max-w-xs truncate">{{ file.name || ('MediaLibrary.EmptyValueIndicator' | translate) }}</span>
              </a>
            </td>
            <td class="hidden text-base-content/70 md:table-cell"><span class="block max-w-[12rem] truncate">{{ file.contentType || ('MediaLibrary.EmptyValueIndicator' | translate) }}</span></td>
            <td class="hidden whitespace-nowrap text-base-content/70 sm:table-cell tabular">{{ file.contentSize || ('MediaLibrary.EmptyValueIndicator' | translate) }}</td>
            <td class="hidden lg:table-cell">{{ file.createdBy || ('MediaLibrary.EmptyValueIndicator' | translate) }}</td>
            <td class="hidden whitespace-nowrap lg:table-cell">{{ file.createdAt | date: 'medium' }}</td>
            <td class="text-right whitespace-nowrap">
              <span class="tooltip" [attr.data-tip]="'MediaLibrary.CopyLinkTitle' | translate">
                <button type="button" class="btn btn-ghost btn-xs btn-square" (click)="store.copyLink(file)" [attr.aria-label]="'MediaLibrary.CopyLinkTitle' | translate">
                  <svg lucideCopy class="size-4"></svg>
                </button>
              </span>
              <span class="tooltip" [attr.data-tip]="'MediaLibrary.EditFileTitle' | translate">
                <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['edit-file', file.id])" [attr.aria-label]="'MediaLibrary.EditFileTitle' | translate">
                  <svg lucidePencil class="size-4"></svg>
                </a>
              </span>
              <span class="tooltip tooltip-left" [attr.data-tip]="'MediaLibrary.DeleteFileTitle' | translate">
                <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="deleteFile(file)" [attr.aria-label]="'MediaLibrary.DeleteFileTitle' | translate">
                  <svg lucideTrash class="size-4"></svg>
                </button>
              </span>
            </td>
          </tr>
        }
      </tbody>
    </table>
  `
})
export class ListViewComponent {
  protected readonly store = inject(MediaLibraryStore);
  protected readonly pane = inject(SidePaneService);
  public readonly content = input.required<FolderContent>();

  protected deleteFolder(folder: MediaFolder): void {
    this.store.deleteFolder(folder).subscribe();
  }

  protected deleteFile(file: MediaFile): void {
    this.store.deleteFile(file).subscribe();
  }
}
