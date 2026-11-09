import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const SETTINGS_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./settings.component').then(m => m.SettingsComponent) }
];

export const SETTINGS_PANE_ROUTES: Routes = [
  paneRoute('create-message-sender', () => import('./email/edit/email-senders-edit.component').then(m => m.EmailSendersEditComponent)),
  paneRoute('edit-message-sender/:messageSenderId', () => import('./email/edit/email-senders-edit.component').then(m => m.EmailSendersEditComponent)),
  paneRoute('edit-media-setting/:mediaSettingKey', () => import('./media/edit/media-setting-edit.component').then(m => m.MediaSettingEditComponent))
];
