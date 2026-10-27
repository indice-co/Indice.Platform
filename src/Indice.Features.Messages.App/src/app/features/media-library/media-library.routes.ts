import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const MEDIA_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./media-library.component').then(m => m.MediaLibraryComponent) },
  { path: 'root', redirectTo: '', pathMatch: 'full' },
  {
    path: ':folderId', data: { breadcrumb: { title: 'Breadcrumb.Folder' } },
    loadComponent: () => import('./media-library.component').then(m => m.MediaLibraryComponent)
  },
  {
    path: ':folderId/:documentId', data: { breadcrumb: { title: 'Breadcrumb.Edit' } },
    loadComponent: () => import('./document-edit/document-edit.component').then(m => m.DocumentEditComponent)
  }
];

const folderPane = () => import('./folder-edit/folder-edit.component').then(m => m.FolderEditComponent);

export const MEDIA_PANE_ROUTES: Routes = [
  /** Matrix param `parentId`: the folder the new folder is created in. */
  paneRoute('create-folder', folderPane),
  paneRoute('edit-folder/:folderId', folderPane),
  /** Matrix param `folderId`: the folder the files are uploaded to. */
  paneRoute('upload-file', () => import('./document-upload/document-upload.component').then(m => m.DocumentUploadComponent)),
  paneRoute('edit-file/:documentId', () => import('./document-edit/rightpane/document-edit-rightpane.component').then(m => m.DocumentEditRightpaneComponent))
];
