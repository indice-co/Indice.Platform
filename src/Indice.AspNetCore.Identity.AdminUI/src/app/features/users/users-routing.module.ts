import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { UsersComponent } from './users.component';
import { UserAddComponent } from './add/user-add.component';
import { UserEditComponent } from './edit/user-edit.component';
import { UserDetailsComponent } from './edit/details/user-details.component';
import { UserRolesComponent } from './edit/roles/user-roles.component';
import { UserAdditionalDetailsComponent } from './edit/additional-details/user-additional-details.component';
import { AdditionalDetailEditComponent } from './edit/additional-details/edit/additional-detail-edit.component';
import { UserApplicationsComponent } from './edit/applications/user-applications.component';

const routes: Routes = [
  { path: '', component: UsersComponent },
  { path: 'add', component: UserAddComponent },
  {
    path: ':id', component: UserEditComponent, children: [
      { path: '', redirectTo: 'details', pathMatch: 'full' },
      { path: 'details', component: UserDetailsComponent },
      { path: 'roles', component: UserRolesComponent },
      { path: 'additional-details', component: UserAdditionalDetailsComponent },
      { path: 'additional-details/:id/edit', component: AdditionalDetailEditComponent },
      { path: 'applications', component: UserApplicationsComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UsersRoutingModule { }
