import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const SETTING_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./settings.component').then(m => m.SettingsComponent) },
  { path: 'add', redirectTo: '/settings(rightpane:setting-add)' }
];

export const SETTING_PANE_ROUTES: Routes = [
  paneRoute('setting-add', () => import('./setting-edit.component').then(m => m.SettingEditComponent)),
  paneRoute('setting/:key', () => import('./setting-edit.component').then(m => m.SettingEditComponent))
];
