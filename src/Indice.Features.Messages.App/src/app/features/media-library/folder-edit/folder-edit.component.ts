import { ChangeDetectorRef, Component, ElementRef, Inject, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MenuOption, ToastType } from '@indice/ng-components';
import { MediaFolder, UpdateFolderRequest } from 'src/app/core/services/media-api.service';
import { MediaLibraryStore } from '../media-library-store.service';
import { takeUntil, tap } from 'rxjs/operators';
import { Subscription, Subject, combineLatest } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';

@Component({
  selector: 'app-folder-edit',
  templateUrl: './folder-edit.component.html'
})
export class FolderEditComponent implements OnInit, OnDestroy {

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;

  public folders: MenuOption[] = [];
  public parentFolderId: MenuOption | null = null;

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _mediaStore: MediaLibraryStore,
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    private _toaster: AppTranslatedToaster,
    private _translate: TranslateService
  ) { }

  public submitInProgress = false;
  public model = new MediaFolder();
  private _folderId: string | undefined;
  private _loadFoldersSubscription: Subscription | undefined;
  private _destroy$ = new Subject<void>();

  public ngOnInit(): void {
    this._folderId = this._activatedRoute.snapshot.params['folderId'];
    combineLatest([
      this._translate.get('MediaLibrary.SelectPlaceholder')
    ]).pipe(takeUntil(this._destroy$)).subscribe(([selectPlaceholder]) => {
      this.folders = [new MenuOption(selectPlaceholder || 'MediaLibrary.SelectPlaceholder', null)];
      if (this._folderId) {
        this._mediaStore.getFolderDetails(this._folderId)
          .pipe(takeUntil(this._destroy$))
          .subscribe((folderDetails) => {
            this.model = folderDetails;
            this._loadFolders();
          });
      }
    });
  }

  public ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
    this._loadFoldersSubscription?.unsubscribe();
  }

  private _loadFolders(): void {
    this._loadFoldersSubscription = this._mediaStore
      .listFolders()
      .pipe(
        tap((folders: MediaFolder[]) => {
          let selectedFolder = this.model.parentId ? folders.find(f => f.id == this.model.parentId) : null;
          this.parentFolderId = selectedFolder ? new MenuOption(selectedFolder.name!, selectedFolder?.id) : null;
          if (folders) {
            this.folders.push(...folders.filter(f => f.id !== this._folderId).map(s => {
              return new MenuOption(s.name!, s.id, undefined)
            }));
          }
        }),
        takeUntil(this._destroy$)
      )
      .subscribe();
  }

  public parentFolderSelectionChanged(selectedOption: MenuOption): void {
    if (selectedOption.value) {
      this.parentFolderId = selectedOption;
      this.model.parentId = this.parentFolderId.value;
    } else {
      this.parentFolderId = null;
      this.model.parentId = undefined;
    }
  }

  public ngAfterViewInit(): void {
    this._changeDetector.detectChanges();
  }

  public onSubmit(): void {
    this.submitInProgress = true;
    let request = new UpdateFolderRequest({
      name: this.model.name,
      description: this.model.description,
      parentId: this.model.parentId
    })
    this._mediaStore
      .updateFolder(this._folderId!, request)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: () => {
          this.submitInProgress = false;
          this._toaster.show(ToastType.Success, 'MediaLibrary.UpdateFolderSuccessTitle', 'MediaLibrary.UpdateFolderSuccessMessage', undefined, { name: this.model.name }); // localized toast
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this.model.parentId ? this._router.navigate(['media', this.model.parentId]) : this._router.navigate(['media']));
        }
      });
  }
}
