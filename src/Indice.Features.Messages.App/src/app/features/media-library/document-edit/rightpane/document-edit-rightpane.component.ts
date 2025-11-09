import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';

import { MenuOption, ToastType } from '@indice/ng-components';
import { Subscription, Subject, combineLatest } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';
import { MediaFile, MediaFolder, UpdateFileMetadataRequest } from 'src/app/core/services/media-api.service';

import { MediaLibraryStore } from '../../media-library-store.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster'; // localized toaster
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-document-edit-rightpane',
  templateUrl: './document-edit-rightpane.component.html'
})
export class DocumentEditRightpaneComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('editNameTemplate', { static: true }) public editNameTemplate!: TemplateRef<any>;
  @ViewChild('editDescriptionTemplate', { static: true }) public editDescriptionTemplate!: TemplateRef<any>;
  @ViewChild('editFolderTemplate', { static: true }) public editFolderTemplate!: TemplateRef<any>;
  private _updateFileSubscription: Subscription | undefined;
  private _loadFoldersSubscription: Subscription | undefined;
  private _documentId = '';
  private _destroy$ = new Subject<void>();

  constructor(
    private _mediaStore: MediaLibraryStore,
    private _router: Router,
    private _activatedRoute: ActivatedRoute,
    private _changeDetector: ChangeDetectorRef,
    private _toaster: AppTranslatedToaster, // replaced ToasterService
    private _translate: TranslateService
  ) { }

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  public submitInProgress = false;
  public templateOutlet!: TemplateRef<any>;
  public model = new MediaFile();
  public folders: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
  public selectedFolderId: MenuOption | null = null;

  public ngOnInit(): void {
    this._documentId = this._router.url.split('/')[3]?.split('(')[0];
      this._activatedRoute.queryParams.subscribe((queryParams: Params) => {
      this._selectTemplate(queryParams.action || 'editName');
    });
    combineLatest([
      this._translate.get('MediaLibrary.SelectPlaceholder')
    ]).pipe(takeUntil(this._destroy$)).subscribe(([placeholder]) => {
      this.folders = [new MenuOption(placeholder || 'MediaLibrary.SelectPlaceholder', null)];
    });
  }

  public onSubmit(): void {
    this.submitInProgress = true;
    let request = new UpdateFileMetadataRequest();
    request.name = this.model.name;
    request.description = this.model.description;
    request.folderId = this.model.folderId;
    this._updateFileSubscription = this._mediaStore
      .updateFileMetadata(this._documentId, request)
      .pipe(takeUntil(this._destroy$))
      .subscribe({
        next: () => {
          this.submitInProgress = false;
          this._toaster.show(ToastType.Success, 'MediaLibrary.UpdateFileSuccessTitle', 'MediaLibrary.UpdateFileSuccessMessage',undefined, { name: this.model.name }); // localized single line toast
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['media', this.model.folderId ?? 'root', this._documentId]));
        }
      });
  }

  public ngAfterViewInit(): void {
    this._mediaStore.getFileDetails(this._documentId)
      .subscribe((fileDetails) => {
        this.model = fileDetails;
        if (this.templateOutlet == this.editFolderTemplate) {
          this._loadFolders();
        }
      })
    this._changeDetector.detectChanges();
  }

  public folderSelectionChanged(selectedOption: MenuOption): void {
    if (selectedOption.value) {
      this.selectedFolderId = selectedOption;
      this.model.folderId = this.selectedFolderId.value;
    } else {
      this.selectedFolderId = null;
      this.model.folderId = '';
    }
  }

  private _selectTemplate(action: string): void {
    switch (action) {
      case 'editName':
        this.templateOutlet = this.editNameTemplate;
        break;
      case 'editDescription':
        this.templateOutlet = this.editDescriptionTemplate;
        break;
      case 'editParentFolder':
        this.templateOutlet = this.editFolderTemplate;
        break;
    }
  }

  private _loadFolders(): void {
    this._loadFoldersSubscription = this._mediaStore
      .listFolders()
      .pipe(
        tap((folders: MediaFolder[]) => {
          let selectedFolder = this.model.folderId ? folders.find(f => f.id == this.model.folderId) : null;
          this.selectedFolderId = selectedFolder ? new MenuOption(selectedFolder.name!, selectedFolder?.id) : null;
          if (folders) {
            this.folders.push(...folders.map(s => new MenuOption(s.name!, s.id, undefined)));
          }
        }),
        takeUntil(this._destroy$)
      )
      .subscribe();
  }

  public ngOnDestroy(): void {
    this._updateFileSubscription?.unsubscribe();
    this._loadFoldersSubscription?.unsubscribe();
    this._destroy$.next();
    this._destroy$.complete();
  }
}
