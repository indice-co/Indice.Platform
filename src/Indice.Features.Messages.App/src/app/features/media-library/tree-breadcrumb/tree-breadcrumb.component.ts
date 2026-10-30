import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideFolder, LucideFolderOpen, LucideHouse } from '@lucide/angular';
import { FolderTreeStructure } from 'src/app/core/services/media-api.service';
import { MediaLibraryStore } from '../media-library-store.service';

/** Folder tree navigation of the media library (daisyUI menu). Branches leading to the active folder start expanded. */
@Component({
  selector: 'app-tree-breadcrumb',
  imports: [NgTemplateOutlet, RouterLink, TranslatePipe, LucideFolder, LucideFolderOpen, LucideHouse],
  host: { class: 'block' },
  template: `
    @if (!structure() && loading()) {
      <div class="space-y-2 p-2">
        @for (row of [1, 2, 3, 4]; track row) { <div class="skeleton h-6 w-full"></div> }
      </div>
    } @else {
      <ul class="menu menu-sm w-full">
        <li>
          <a [routerLink]="store.folderLink(undefined)" [class.menu-active]="!activeFolderId()">
            <svg lucideHouse class="size-4"></svg>
            <span class="flex-1 truncate">{{ 'MediaLibrary.Title' | translate }}</span>
            <span class="badge badge-ghost badge-sm tabular">{{ structure()?.rootFilesCount ?? structure()?.filesCount ?? 0 }}</span>
          </a>
        </li>
        <ng-container *ngTemplateOutlet="branch; context: { $implicit: structure()?.items }" />
      </ul>
    }

    <ng-template #branch let-nodes>
      @for (tree of nodes; track tree.node?.id) {
        <li>
          @if (tree.children?.length) {
            <details [open]="expanded().has(tree.node?.id)">
              <summary [class.menu-active]="tree.node?.id === activeFolderId()">
                <a class="flex min-w-0 flex-1 items-center gap-2" [routerLink]="store.folderLink(tree.node?.id)">
                  <ng-container *ngTemplateOutlet="label; context: { $implicit: tree }" />
                </a>
              </summary>
              <ul><ng-container *ngTemplateOutlet="branch; context: { $implicit: tree.children }" /></ul>
            </details>
          } @else {
            <a [routerLink]="store.folderLink(tree.node?.id)" [class.menu-active]="tree.node?.id === activeFolderId()">
              <ng-container *ngTemplateOutlet="label; context: { $implicit: tree }" />
            </a>
          }
        </li>
      }
    </ng-template>

    <ng-template #label let-tree>
      @if (tree.node?.id === activeFolderId()) { <svg lucideFolderOpen class="size-4 shrink-0 text-warning"></svg> }
      @else { <svg lucideFolder class="size-4 shrink-0 text-warning"></svg> }
      <span class="min-w-0 flex-1 truncate" [title]="tree.node?.name">{{ tree.node?.name || ('MediaLibrary.EmptyValueIndicator' | translate) }}</span>
      <span class="badge badge-ghost badge-sm tabular">{{ tree.node?.filesCount ?? 0 }}</span>
    </ng-template>
  `
})
export class TreeBreadcrumbComponent {
  protected readonly store = inject(MediaLibraryStore);

  public readonly structure = input<FolderTreeStructure>();
  public readonly activeFolderId = input<string>();
  public readonly loading = input(false);

  /** Ids of the active folder and its ancestors. */
  protected readonly expanded = computed(() => {
    return new Set(this.store.pathTo(this.activeFolderId()).map(folder => folder.id));
  });
}
