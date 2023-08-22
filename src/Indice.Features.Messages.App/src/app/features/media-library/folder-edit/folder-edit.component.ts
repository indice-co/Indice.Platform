import { ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MenuOption, ToasterService, ToastType } from '@indice/ng-components';
import { Folder, UpdateFolderRequest } from 'src/app/core/services/media-api.service';
import { MediaLibraryStore } from '../media-library-store.service';
import { tap } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-folder-edit',
  templateUrl: './folder-edit.component.html'
})
export class FolderEditComponent implements OnInit {

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  
  public folders: MenuOption[] = [new MenuOption('Παρακαλώ επιλέξτε...', null)];
  public parentFolderId: MenuOption | null = null;

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _mediaStore: MediaLibraryStore,
        private _router: Router,
        private _activatedRoute: ActivatedRoute,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    public submitInProgress = false;
    public model = new Folder();
    private _folderId: string | undefined;
    private _loadFoldersSubscription: Subscription | undefined;

    public ngOnInit(): void { 
      this._folderId = this._activatedRoute.snapshot.params['folderId'];
      if (this._folderId) {
        this._mediaStore.getFolderDetails(this._folderId)
          .subscribe((folderDetails) => {
            this.model = folderDetails;
            this._loadFolders();
          });
      }

    }

    private _loadFolders(): void {        
      this._loadFoldersSubscription = this._mediaStore
          .listFolders()
          .pipe(tap((folders: Folder[]) => {
              let selectedFolder = this.model.parentId ? folders.find(f => f.id == this.model.parentId) : null;
              this.parentFolderId = selectedFolder ? new MenuOption(selectedFolder.name!, selectedFolder?.id) : null;
              if (folders) {
                  this.folders.push(...folders.filter(f => f.id !== this._folderId).map(s => {
                      return new MenuOption(s.name!, s.id, undefined)
                  }));
              }
          }))
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
            .subscribe({
                next: () => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Ο φάκελος με όνομα '${this.model.name}' ενημερώθηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this.model.parentId ? this._router.navigate(['media', this.model.parentId]) : this._router.navigate(['media']));
                }
            });
    }


}
