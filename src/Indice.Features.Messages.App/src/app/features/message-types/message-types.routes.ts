import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const MESSAGE_TYPE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./message-types.component').then(m => m.MessageTypesComponent) }
];

export const MESSAGE_TYPE_PANE_ROUTES: Routes = [
  paneRoute('create-message-type', () => import('./edit/message-type-edit.component').then(m => m.MessageTypeEditComponent)),
  paneRoute('edit-message-type/:messageTypeId', () => import('./edit/message-type-edit.component').then(m => m.MessageTypeEditComponent))
];
