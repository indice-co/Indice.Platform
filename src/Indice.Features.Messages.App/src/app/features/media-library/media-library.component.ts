import { Component, OnInit, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { map, tap } from 'rxjs';
import {
  LucideCornerLeftUp, LucideDatabaseZap, LucideFolderOpen, LucideFolderPlus, LucideHouse, LucideLayoutGrid, LucideList, LucideRefreshCw, LucideUpload
} from '@lucide/angular';
import { FolderContent, MediaApiClient, MediaFile, MediaFolder } from 'src/app/core/services/media-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { FolderViewComponent } from './item-views/folder-view/folder-view.component';
import { ListViewComponent } from './item-views/list-view/list-view.component';
import { MEDIA_SCOPE, MediaLibraryStore } from './media-library-store.service';
import { TreeBreadcrumbComponent } from './tree-breadcrumb/tree-breadcrumb.component';

/** Media library file manager: folder tree, path breadcrumb, grid / list view of the current folder's content. */
@Component({
  selector: 'app-media-library',
  imports: [
    RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, TreeBreadcrumbComponent, FolderViewComponent,
    ListViewComponent, LucideCornerLeftUp, LucideDatabaseZap, LucideFolderOpen, LucideFolderPlus, LucideHouse, LucideLayoutGrid, LucideList,
    LucideRefreshCw, LucideUpload
  ],
  template: `
    <app-page-header [title]="'MediaLibrary.Title' | translate" [subtitle]="'MediaLibrary.Subtitle' | translate">
      <div actions class="flex flex-wrap gap-2">
        <a class="btn btn-sm" [routerLink]="pane.link('create-folder', createFolderParams())">
          <svg lucideFolderPlus class="size-4"></svg>{{ 'MediaLibrary.AddFolderTitle' | translate }}
        </a>
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link('upload-file', uploadParams())">
          <svg lucideUpload class="size-4"></svg>{{ 'MediaLibrary.UploadFiles' | translate }}
        </a>
      </div>
    </app-page-header>

    <div class="grid gap-4 lg:grid-cols-[16rem_minmax(0,1fr)]">
      <aside class="panel hidden self-start p-2 lg:block">
        <p class="px-3 pt-2 pb-1 text-xs font-semibold tracking-wide text-base-content/60 uppercase">{{ 'MediaLibrary.FoldersSection' | translate }}</p>
        <app-tree-breadcrumb [structure]="store.structure()" [activeFolderId]="folderId()" [loading]="store.structureLoading()" />
      </aside>

      <section class="panel min-w-0">
        <div class="flex flex-wrap items-center gap-x-3 gap-y-2 border-b border-base-300 px-4 py-3">
          <nav class="breadcrumbs min-w-0 flex-1 py-0 text-sm" [attr.aria-label]="'MediaLibrary.FolderLabel' | translate">
            <ul>
              <li>
                <a class="inline-flex items-center gap-1.5" routerLink="/media">
                  <svg lucideHouse class="size-4"></svg>{{ 'MediaLibrary.Title' | translate }}
                </a>
              </li>
              @for (folder of path(); track folder.id; let last = $last) {
                <li>
                  @if (last) {
                    <span class="inline-flex items-center gap-1.5 font-semibold"><svg lucideFolderOpen class="size-4 text-warning"></svg>{{ folder.name }}</span>
                  } @else {
                    <a [routerLink]="store.folderLink(folder.id)">{{ folder.name }}</a>
                  }
                </li>
              }
            </ul>
          </nav>
          <span class="text-sm text-base-content/60 tabular">{{ list.count() }} {{ 'MediaLibrary.ResultsSuffix' | translate }}</span>
          <div class="flex items-center gap-1">
            <span class="tooltip" [attr.data-tip]="'MediaLibrary.RefreshTitle' | translate">
              <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="refresh()" [attr.aria-label]="'MediaLibrary.RefreshTitle' | translate">
                <svg lucideRefreshCw class="size-4" [class.animate-spin]="list.loading()"></svg>
              </button>
            </span>
            @if (!folderId()) {
              <span class="tooltip" [attr.data-tip]="'MediaLibrary.DiscoverStructureTitle' | translate">
                <button type="button" class="btn btn-ghost btn-sm btn-square" [disabled]="discovering()" (click)="discover()"
                  [attr.aria-label]="'MediaLibrary.DiscoverStructureTitle' | translate">
                  @if (discovering()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideDatabaseZap class="size-4"></svg> }
                </button>
              </span>
            }
            <div class="join ml-1">
              <button type="button" class="join-item btn btn-sm" [class.btn-active]="store.view() === 'grid'" [attr.aria-pressed]="store.view() === 'grid'"
                [title]="'MediaLibrary.ViewFolderTitle' | translate" [attr.aria-label]="'MediaLibrary.ViewFolderTitle' | translate" (click)="store.setView('grid')">
                <svg lucideLayoutGrid class="size-4"></svg>
              </button>
              <button type="button" class="join-item btn btn-sm" [class.btn-active]="store.view() === 'list'" [attr.aria-pressed]="store.view() === 'list'"
                [title]="'MediaLibrary.ViewListTitle' | translate" [attr.aria-label]="'MediaLibrary.ViewListTitle' | translate" (click)="store.setView('list')">
                <svg lucideList class="size-4"></svg>
              </button>
            </div>
          </div>
        </div>

        @if (!content() && list.loading()) {
          <div class="grid grid-cols-2 gap-3 p-4 sm:grid-cols-3 lg:grid-cols-4 2xl:grid-cols-6">
            @for (card of [1, 2, 3, 4, 5, 6]; track card) { <div class="skeleton aspect-[4/3] w-full"></div> }
          </div>
        } @else if (isEmpty()) {
          <app-empty-state [title]="'MediaLibrary.EmptyFolderTitle' | translate" [message]="'MediaLibrary.EmptyFolderMessage' | translate">
            <svg icon lucideFolderOpen class="size-5"></svg>
            <div actions class="flex flex-wrap justify-center gap-2">
              @if (content()?.id) {
                <a class="btn btn-ghost btn-sm" [routerLink]="store.folderLink(content()?.parentId)">
                  <svg lucideCornerLeftUp class="size-4"></svg>{{ 'MediaLibrary.ParentFolderTitle' | translate }}
                </a>
              }
              <a class="btn btn-sm" [routerLink]="pane.link('create-folder', createFolderParams())">
                <svg lucideFolderPlus class="size-4"></svg>{{ 'MediaLibrary.AddFolderTitle' | translate }}
              </a>
              <a class="btn btn-primary btn-sm" [routerLink]="pane.link('upload-file', uploadParams())">
                <svg lucideUpload class="size-4"></svg>{{ 'MediaLibrary.UploadFiles' | translate }}
              </a>
            </div>
          </app-empty-state>
        } @else if (content(); as current) {
          <div class="transition-opacity" [class.opacity-60]="list.loading()">
            @if (store.view() === 'grid') {
              <app-folder-view class="p-4" [content]="current" />
            } @else {
              <app-list-view [content]="current" />
            }
          </div>
        }

        @if (list.count() > 0) {
          <app-pagination class="border-t border-base-300 px-4 py-3" [page]="list.page()" [pageSize]="list.pageSize()" [count]="list.count()"
            (pageChange)="list.setPage($event)" (pageSizeChange)="list.setPageSize($event)" />
        }
      </section>
    </div>
  `
})
export class MediaLibraryComponent implements OnInit {
  private readonly _api = inject(MediaApiClient);
  protected readonly store = inject(MediaLibraryStore);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `media/:folderId` route; absent at the library root. */
  public readonly folderId = input<string>();

  protected readonly content = signal<FolderContent | undefined>(undefined);
  protected readonly discovering = signal(false);
  private _loadedFolderId: string | undefined;

  protected readonly list = new ListState<MediaFolder | MediaFile>({
    pageSize: 20,
    load: query => {
      const folderId = this.folderId() || undefined;
      this._loadedFolderId = folderId;
      return this._api.getFolderContent(folderId, query.page, query.pageSize).pipe(
        tap(content => this.content.set(content)),
        map(content => ({ count: content.totalCount ?? 0, items: [...(content.folders ?? []), ...(content.files ?? [])] }))
      );
    }
  });

  protected readonly isEmpty = computed(() => !this.list.loading() && !this.content()?.folders?.length && !this.content()?.files?.length);
  protected readonly createFolderParams = computed(() => this.folderId() ? { parentId: this.folderId()! } : undefined);
  protected readonly uploadParams = computed(() => this.folderId() ? { folderId: this.folderId()! } : undefined);

  /** Folders from the root to the current one; falls back to the loaded folder while the tree is not available. */
  protected readonly path = computed(() => {
    const folderId = this.folderId();
    const path = this.store.pathTo(folderId);
    if (path.length || !folderId) {
      return path;
    }
    const content = this.content();
    return content?.id === folderId ? [new MediaFolder({ id: content.id, name: content.name ?? '' })] : [];
  });

  constructor() {
    inject(RefreshBus).on(MEDIA_SCOPE).pipe(takeUntilDestroyed()).subscribe(() => this.refresh());
    // The component is reused when navigating between folders: reload when the folder changes.
    effect(() => {
      const folderId = this.folderId() || undefined;
      untracked(() => {
        if (this.list.started && folderId !== this._loadedFolderId) {
          this.list.refresh();
        }
      });
    });
    // A deletion can leave the current page empty: go back to the first page.
    effect(() => {
      if (!this.list.loading() && this.list.page() > 1 && this.list.items()?.length === 0) {
        untracked(() => this.list.setPage(1));
      }
    });
  }

  public ngOnInit(): void {
    this.list.start();
    this.store.loadStructure();
  }

  protected refresh(): void {
    this.list.refresh();
    this.store.loadStructure();
  }

  protected discover(): void {
    this.discovering.set(true);
    this._api.discover().subscribe({
      next: () => {
        this.discovering.set(false);
        this.store.notifyChanged();
      },
      error: () => this.discovering.set(false)
    });
  }
}
