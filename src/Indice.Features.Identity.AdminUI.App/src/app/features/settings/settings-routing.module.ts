import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { SettingsComponent } from './settings.component';
import { SettingEditComponent } from './edit/setting-edit.component';
import { SettingResolverService } from './edit/setting-resolver.service';
import { SettingAddComponent } from './add/setting-add.component';

const routes: Routes = [
    { path: '', component: SettingsComponent },
    { path: 'add', component: SettingAddComponent },
    { path: ':id/edit', component: SettingEditComponent, resolve: { setting: SettingResolverService } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
  providers: [SettingResolverService]
})
export class SettingsRoutingModule { }
