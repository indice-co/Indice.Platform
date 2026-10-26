import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { map, of, switchMap } from 'rxjs';
import {
  LucideArrowLeft, LucideCopy, LucideDownload, LucideDynamicIcon, LucideExternalLink, LucideFolder, LucideLink, LucidePencil, LucideTrash
} from '@lucide/angular';
import { MediaApiClient, MediaFile } from 'src/app/core/services/media-api.service';
import { FileUtilitiesService } from 'src/app/shared/services/file-utilities.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { MEDIA_SCOPE, MediaLibraryStore } from '../media-library-store.service';

/** Details page of a media file (`media/:folderId/:documentId`, `folderId` is `root` for files at the library root). */
@Component({
  selector: 'app-document-edit',
  imports: [
    DatePipe, RouterLink, TranslatePipe, PageHeaderComponent, LucideArrowLeft, LucideCopy, LucideDownload, LucideDynamicIcon, LucideExternalLink,
    LucideFolder, LucideLink, LucidePencil, LucideTrash
  ],
  template: `
    <app-page-header [title]="file()?.name || ('MediaLibrary.EditFileTitle' | translate)" [subtitle]="file()?.description">
      <a leading class="btn btn-ghost btn-xs -ml-2 mb-2" [routerLink]="backLink()">
        <svg lucideArrowLeft class="size-4"></svg>{{ folderName() || ('MediaLibrary.Title' | translate) }}
      </a>
      @if (file(); as current) {
        <div class="mt-2 flex flex-wrap items-center gap-2">
          @if (current.fileExtension) { <span class="badge badge-soft badge-primary uppercase">{{ current.fileExtension.replace('.', '') }}</span> }
          @if (current.contentSize) { <span class="badge badge-ghost tabular">{{ current.contentSize }}</span> }
        </div>
      }
      <div actions class="flex flex-wrap gap-2">
        @if (file(); as current) {
          <button type="button" class="btn btn-sm" (click)="store.open(current)">
            <svg lucideExternalLink class="size-4"></svg>{{ 'MediaLibrary.Preview' | translate }}
          </button>
          <button type="button" class="btn btn-sm" [disabled]="downloading()" (click)="download(current)">
            @if (downloading()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideDownload class="size-4"></svg> }
            {{ 'MediaLibrary.DownloadFile' | translate }}
          </button>
          <button type="button" class="btn btn-sm" (click)="store.copyLink(current)">
            <svg lucideLink class="size-4"></svg>{{ 'MediaLibrary.CopyLinkTitle' | translate }}
          </button>
          <a class="btn btn-primary btn-sm" [routerLink]="pane.link(['edit-file', current.id])">
            <svg lucidePencil class="size-4"></svg>{{ 'MediaLibrary.EditFileTitle' | translate }}
          </a>
          <span class="tooltip tooltip-left" [attr.data-tip]="'MediaLibrary.DeleteFileTitle' | translate">
            <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="remove(current)" [attr.aria-label]="'MediaLibrary.DeleteFileTitle' | translate">
              <svg lucideTrash class="size-4"></svg>
            </button>
          </span>
        }
      </div>
    </app-page-header>

    @if (!file()) {
      <div class="grid gap-4 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
        <div class="skeleton h-80 w-full"></div>
        <div class="skeleton h-80 w-full"></div>
      </div>
    } @else {
      @let current = file()!;
      <div class="grid gap-4 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
        <section class="panel overflow-hidden">
          <div class="grid min-h-72 place-items-center bg-base-200 p-6">
            @if (previewUrl() && !previewFailed()) {
              <img class="max-h-[32rem] max-w-full rounded-box object-contain shadow-lg" [src]="previewUrl()" [alt]="current.description || current.name"
                (load)="onImageLoad($event)" (error)="previewFailed.set(true)" />
            } @else {
              <div class="flex flex-col items-center gap-3 text-center text-base-content/60">
                <svg [lucideIcon]="icon()" class="size-16"></svg>
                <p class="text-sm">{{ 'MediaLibrary.NoPreview' | translate }}</p>
                <button type="button" class="btn btn-sm" (click)="store.open(current)">
                  <svg lucideExternalLink class="size-4"></svg>{{ 'MediaLibrary.OpenInNewTabTitle' | translate }}
                </button>
              </div>
            }
          </div>
          @if (dimensions(); as size) {
            <p class="border-t border-base-300 px-5 py-2.5 text-center text-sm text-base-content/70 tabular">
              {{ 'MediaLibrary.ImageDimensionsPrefix' | translate }} {{ size.width }} × {{ size.height }} {{ 'MediaLibrary.PixelsSuffix' | translate }}
            </p>
          }
        </section>

        <section class="panel p-5 sm:p-6">
          <h2 class="font-semibold">{{ 'MediaLibrary.BasicInformationHeader' | translate }}</h2>
          <p class="mt-0.5 text-sm text-base-content/60">{{ 'MediaLibrary.BasicInformationDescription' | translate }}</p>
          <dl class="mt-5 grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
            <div class="sm:col-span-2">
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.TitleLabel' | translate }}</dt>
              <dd class="font-medium break-words">{{ current.name || ('MediaLibrary.EmptyValueIndicator' | translate) }}</dd>
            </div>
            <div class="sm:col-span-2">
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.DescriptionLabel' | translate }}</dt>
              <dd class="font-medium break-words">{{ current.description || ('MediaLibrary.EmptyValueIndicator' | translate) }}</dd>
            </div>
            <div class="sm:col-span-2">
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.FolderLabel' | translate }}</dt>
              <dd class="font-medium">
                <a class="link link-hover inline-flex items-center gap-1.5" [routerLink]="backLink()">
                  <svg lucideFolder class="size-4 text-warning"></svg>{{ folderName() || ('MediaLibrary.Title' | translate) }}
                </a>
              </dd>
            </div>
            <div class="sm:col-span-2">
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.LocationLabel' | translate }}</dt>
              <dd class="mt-0.5 flex items-center gap-2">
                <code class="min-w-0 flex-1 truncate rounded bg-base-200 px-2 py-1 text-xs" [title]="current.path">{{ current.path || ('MediaLibrary.EmptyValueIndicator' | translate) }}</code>
                <span class="tooltip tooltip-left" [attr.data-tip]="'MediaLibrary.CopyPathTitle' | translate">
                  <button type="button" class="btn btn-ghost btn-xs btn-square" (click)="store.copyPath(current)" [attr.aria-label]="'MediaLibrary.CopyPathTitle' | translate">
                    <svg lucideCopy class="size-4"></svg>
                  </button>
                </span>
              </dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.TypeLabel' | translate }}</dt>
              <dd class="font-medium break-words">{{ current.contentType || ('MediaLibrary.EmptyValueIndicator' | translate) }}</dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.SizeLabel' | translate }}</dt>
              <dd class="font-medium tabular">{{ current.contentSize || ('MediaLibrary.EmptyValueIndicator' | translate) }}</dd>
            </div>
          </dl>
          <dl class="mt-6 grid grid-cols-1 gap-x-6 gap-y-4 border-t border-base-300 pt-5 text-sm sm:grid-cols-2">
            <div>
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.CreatedByLabel' | translate }}</dt>
              <dd class="font-medium break-words">{{ current.createdBy || ('MediaLibrary.EmptyValueIndicator' | translate) }}</dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.CreatedAtLabel' | translate }}</dt>
              <dd class="font-medium">{{ current.createdAt ? (current.createdAt | date: 'medium') : ('MediaLibrary.EmptyValueIndicator' | translate) }}</dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.UpdatedByLabel' | translate }}</dt>
              <dd class="font-medium break-words">{{ current.updatedBy || ('MediaLibrary.EmptyValueIndicator' | translate) }}</dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'MediaLibrary.UpdatedAtLabel' | translate }}</dt>
              <dd class="font-medium">{{ current.updatedAt ? (current.updatedAt | date: 'medium') : ('MediaLibrary.EmptyValueIndicator' | translate) }}</dd>
            </div>
          </dl>
        </section>
      </div>
    }
  `
})
export class DocumentEditComponent implements OnInit {
  private readonly _api = inject(MediaApiClient);
  private readonly _router = inject(Router);
  private readonly _files = inject(FileUtilitiesService);
  protected readonly store = inject(MediaLibraryStore);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the route: the file's folder id, or `root`. */
  public readonly folderId = input<string>();
  /** Bound from the route. */
  public readonly documentId = input.required<string>();

  protected readonly file = signal<MediaFile | undefined>(undefined);
  protected readonly folderName = signal<string | undefined>(undefined);
  protected readonly dimensions = signal<{ width: number; height: number } | undefined>(undefined);
  protected readonly previewFailed = signal(false);
  protected readonly downloading = signal(false);

  protected readonly previewUrl = computed(() => {
    const file = this.file();
    return file && this._files.isImage(file) ? file.permaLink : undefined;
  });
  protected readonly icon = computed(() => this._files.getFileIcon(this.file()));
  protected readonly backLink = computed(() => {
    const folderId = this.file()?.folderId ?? this.folderId();
    return this.store.folderLink(folderId && folderId !== 'root' ? folderId : undefined);
  });

  constructor() {
    inject(RefreshBus).on(MEDIA_SCOPE).pipe(takeUntilDestroyed()).subscribe(() => this.load());
  }

  public ngOnInit(): void {
    this.load();
  }

  protected onImageLoad(event: Event): void {
    const image = event.target as HTMLImageElement;
    this.dimensions.set({ width: image.naturalWidth, height: image.naturalHeight });
  }

  protected download(file: MediaFile): void {
    this.downloading.set(true);
    this._files.downloadFile(file)
      .catch(() => this._files.openFileInNewTab(file))
      .finally(() => this.downloading.set(false));
  }

  protected remove(file: MediaFile): void {
    const back = this.backLink();
    this.store.deleteFile(file).subscribe(() => this._router.navigate(back));
  }

  private load(): void {
    this._api.getFileDetails(this.documentId())
      .pipe(
        switchMap(file => file.folderId
          ? this._api.getFolderById(file.folderId).pipe(map(folder => ({ file, folderName: folder.name as string | undefined })))
          : of({ file, folderName: undefined as string | undefined }))
      )
      .subscribe(({ file, folderName }) => {
        this.file.set(file);
        this.folderName.set(folderName);
        this.previewFailed.set(false);
      });
  }
}
