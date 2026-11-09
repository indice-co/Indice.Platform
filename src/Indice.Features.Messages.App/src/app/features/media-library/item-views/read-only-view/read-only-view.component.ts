import { Component, OnInit, inject, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideCopy, LucideCornerLeftUp, LucideExternalLink, LucideFolder, LucideFolderOpen, LucideHouse, LucideRefreshCw } from '@lucide/angular';
import { FolderContent, MediaApiClient, MediaFile } from 'src/app/core/services/media-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { MediaLibraryStore } from '../../media-library-store.service';
import { MediaFileThumbComponent } from '../media-item-parts';

/**
 * Read-only media library browser used to pick a file (e.g. from the campaign content editor). Navigates folders
 * internally (not via the URL); the picked file is available as `selectedFile` and emitted (its perma link) by `fileSelected`.
 */
@Component({
  selector: 'app-read-only-view',
  imports: [
    TranslatePipe, EmptyStateComponent, PaginationComponent, MediaFileThumbComponent, LucideCopy, LucideCornerLeftUp, LucideExternalLink,
    LucideFolder, LucideFolderOpen, LucideHouse, LucideRefreshCw
  ],
  host: { class: 'block space-y-3' },
  template: `
    <div class="flex items-center gap-2">
      <div class="flex min-w-0 flex-1 items-center gap-1.5 text-sm">
        <button type="button" class="btn btn-ghost btn-xs" [disabled]="!folderId()" (click)="goToFolder(undefined)">
          <svg lucideHouse class="size-4"></svg>{{ 'MediaLibrary.Title' | translate }}
        </button>
        @if (content()?.id) {
          <span class="text-base-content/40">/</span>
          <span class="inline-flex min-w-0 items-center gap-1.5 font-semibold">
            <svg lucideFolderOpen class="size-4 shrink-0 text-warning"></svg><span class="truncate">{{ content()?.name }}</span>
          </span>
        }
      </div>
      <span class="tooltip tooltip-left" [attr.data-tip]="'MediaLibrary.RefreshTitle' | translate">
        <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="loadContent(page(), size())" [attr.aria-label]="'MediaLibrary.RefreshTitle' | translate">
          <svg lucideRefreshCw class="size-4" [class.animate-spin]="loading()"></svg>
        </button>
      </span>
    </div>

    @if (!content() && loading()) {
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
        @for (card of [1, 2, 3, 4]; track card) { <div class="skeleton aspect-square w-full"></div> }
      </div>
    } @else if (content(); as current) {
      @if (!current.folders?.length && !current.files?.length) {
        <app-empty-state [title]="'MediaLibrary.EmptyFolderTitle' | translate">
          <svg icon lucideFolderOpen class="size-5"></svg>
          @if (current.id) {
            <button actions type="button" class="btn btn-ghost btn-sm" (click)="goToFolder(current.parentId)">
              <svg lucideCornerLeftUp class="size-4"></svg>{{ 'MediaLibrary.ParentFolderTitle' | translate }}
            </button>
          }
        </app-empty-state>
      } @else {
        <div class="grid grid-cols-2 gap-3 transition-opacity sm:grid-cols-3 md:grid-cols-4" [class.opacity-60]="loading()">
          @if (current.id) {
            <button type="button" class="flex aspect-square flex-col items-center justify-center gap-2 rounded-box border border-dashed border-base-300 text-sm text-base-content/70 hover:border-primary hover:bg-primary/5"
              (click)="goToFolder(current.parentId)">
              <svg lucideCornerLeftUp class="size-6"></svg>{{ 'MediaLibrary.ParentFolderTitle' | translate }}
            </button>
          }
          @for (folder of current.folders; track folder.id) {
            <button type="button" class="flex aspect-square flex-col items-center justify-center gap-2 rounded-box border border-base-300 bg-base-100 p-3 hover:border-primary/50 hover:shadow-sm"
              [title]="folder.name" (click)="goToFolder(folder.id)">
              <span class="grid size-12 place-items-center rounded-field bg-warning/15 text-warning"><svg lucideFolder class="size-6"></svg></span>
              <span class="w-full truncate text-sm font-medium">{{ folder.name }}</span>
              <span class="text-xs text-base-content/60 tabular">
                {{ 'MediaLibrary.FolderCounts' | translate: { folders: folder.subFoldersCount ?? 0, files: folder.filesCount ?? 0 } }}
              </span>
            </button>
          }
          @for (file of current.files; track file.id) {
            <div class="group relative overflow-hidden rounded-box border-2 bg-base-100 transition"
              [class]="selectedFile?.id === file.id ? 'border-primary shadow-md' : 'border-base-300 hover:border-primary/50'">
              <button type="button" class="block w-full text-left" [attr.aria-pressed]="selectedFile?.id === file.id" (click)="selectFile(file)">
                <app-media-file-thumb class="aspect-[4/3]" [file]="file" iconClass="size-8" />
                <span class="block truncate px-2.5 py-2 text-sm font-medium" [title]="file.name">{{ file.name }}</span>
              </button>
              <div class="absolute top-1.5 right-1.5 flex gap-1">
                <button type="button" class="btn btn-xs btn-square" [title]="'MediaLibrary.CopyLinkTitle' | translate"
                  [attr.aria-label]="'MediaLibrary.CopyLinkTitle' | translate" (click)="store.copyLink(file)">
                  <svg lucideCopy class="size-3.5"></svg>
                </button>
                <button type="button" class="btn btn-xs btn-square" [title]="'MediaLibrary.OpenInNewTabTitle' | translate"
                  [attr.aria-label]="'MediaLibrary.OpenInNewTabTitle' | translate" (click)="store.open(file)">
                  <svg lucideExternalLink class="size-3.5"></svg>
                </button>
              </div>
            </div>
          }
        </div>
      }
      @if ((current.totalCount ?? 0) > 0) {
        <app-pagination [page]="page()" [pageSize]="size()" [count]="current.totalCount ?? 0"
          (pageChange)="loadContent($event, size())" (pageSizeChange)="loadContent(1, $event)" />
      }
    }
  `
})
export class ReadOnlyViewComponent implements OnInit {
  private readonly _api = inject(MediaApiClient);
  protected readonly store = inject(MediaLibraryStore);

  protected readonly folderId = signal<string | undefined>(undefined);
  protected readonly page = signal(1);
  protected readonly size = signal(20);
  protected readonly content = signal<FolderContent | undefined>(undefined);
  protected readonly loading = signal(false);
  private readonly _selectedFile = signal<MediaFile | undefined>(undefined);

  /** Emits the perma link of the picked file. */
  public readonly fileSelected = output<string>();

  /** The picked file; hosts may clear it by setting `undefined`. */
  public get selectedFile(): MediaFile | undefined {
    return this._selectedFile();
  }
  public set selectedFile(file: MediaFile | undefined) {
    this._selectedFile.set(file);
  }

  public ngOnInit(): void {
    this.loadContent();
  }

  public loadContent(page = 1, size = 20): void {
    this.page.set(page);
    this.size.set(size);
    this.loading.set(true);
    this._api.getFolderContent(this.folderId(), page, size).subscribe({
      next: content => {
        this.content.set(content);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  public goToFolder(id: string | undefined): void {
    this.folderId.set(id);
    this.loadContent(1, this.size());
  }

  public selectFile(file: MediaFile): void {
    this.selectedFile = file;
    this.fileSelected.emit(file.permaLink);
  }
}
