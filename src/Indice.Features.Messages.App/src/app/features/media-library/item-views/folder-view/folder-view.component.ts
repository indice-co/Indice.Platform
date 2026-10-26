import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideCornerLeftUp, LucideFolder } from '@lucide/angular';
import { FolderContent } from 'src/app/core/services/media-api.service';
import { MediaLibraryStore } from '../../media-library-store.service';
import { MediaFileMenuComponent, MediaFileThumbComponent, MediaFolderMenuComponent } from '../media-item-parts';

/** Grid ("file manager") view of a folder's content: folder cards followed by file cards with thumbnails. */
@Component({
  selector: 'app-folder-view',
  imports: [RouterLink, TranslatePipe, MediaFileMenuComponent, MediaFileThumbComponent, MediaFolderMenuComponent, LucideCornerLeftUp, LucideFolder],
  host: { class: 'block space-y-6' },
  template: `
    @if (content().id || content().folders?.length) {
      <section>
        <h3 class="mb-2 text-xs font-semibold tracking-wide text-base-content/60 uppercase">{{ 'MediaLibrary.FoldersSection' | translate }}</h3>
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4">
          @if (content().id) {
            <a class="flex items-center gap-3 rounded-box border border-dashed border-base-300 p-3 text-base-content/70 transition-colors hover:border-primary hover:bg-primary/5 hover:text-base-content"
              [routerLink]="store.folderLink(content().parentId)" [title]="'MediaLibrary.ParentFolderTitle' | translate">
              <span class="grid size-10 shrink-0 place-items-center rounded-field bg-base-200"><svg lucideCornerLeftUp class="size-5"></svg></span>
              <span class="font-medium">{{ 'MediaLibrary.ParentFolderTitle' | translate }}</span>
            </a>
          }
          @for (folder of content().folders; track folder.id) {
            <div class="group relative flex items-center rounded-box focus-within:z-30 border border-base-300 bg-base-100 transition hover:border-primary/50 hover:shadow-sm">
              <a class="flex min-w-0 flex-1 items-center gap-3 p-3 pr-10" [routerLink]="store.folderLink(folder.id)" [title]="folder.name">
                <span class="grid size-10 shrink-0 place-items-center rounded-field bg-warning/15 text-warning"><svg lucideFolder class="size-5"></svg></span>
                <span class="min-w-0">
                  <span class="block truncate font-medium">{{ folder.name || ('MediaLibrary.EmptyValueIndicator' | translate) }}</span>
                  <span class="block text-xs text-base-content/60 tabular">
                    {{ 'MediaLibrary.FolderCounts' | translate: { folders: folder.subFoldersCount ?? 0, files: folder.filesCount ?? 0 } }}
                  </span>
                </span>
              </a>
              <app-media-folder-menu class="absolute top-1/2 right-2 -translate-y-1/2" [folder]="folder" />
            </div>
          }
        </div>
      </section>
    }
    @if (content().files?.length) {
      <section>
        <h3 class="mb-2 text-xs font-semibold tracking-wide text-base-content/60 uppercase">{{ 'MediaLibrary.FilesSection' | translate }}</h3>
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 2xl:grid-cols-6">
          @for (file of content().files; track file.id) {
            <div class="group relative rounded-box focus-within:z-30 border border-base-300 bg-base-100 transition hover:border-primary/50 hover:shadow-sm">
              <a class="block" [routerLink]="store.fileLink(file)" [title]="file.name">
                <app-media-file-thumb class="aspect-[4/3] rounded-t-box" [file]="file" />
                <div class="px-3 py-2">
                  <p class="truncate pr-6 text-sm font-medium">{{ file.name }}</p>
                  <p class="truncate text-xs text-base-content/60">
                    <span class="uppercase">{{ file.fileExtension?.replace('.', '') }}</span>
                    @if (file.contentSize) { · {{ file.contentSize }} }
                  </p>
                </div>
              </a>
              <app-media-file-menu class="absolute right-1.5 bottom-2" [file]="file" />
            </div>
          }
        </div>
      </section>
    }
  `
})
export class FolderViewComponent {
  protected readonly store = inject(MediaLibraryStore);
  public readonly content = input.required<FolderContent>();
}
