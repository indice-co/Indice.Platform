import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ClientsComponent } from './clients.component';
import { ClientAddComponent } from './add/client-add.component';
import { ClientEditComponent } from './edit/client-edit.component';
import { ClientDetailsComponent } from './edit/details/client-details.component';
import { ClientUrlsComponent } from './edit/urls/client-urls.component';

const routes: Routes = [
    { path: '', component: ClientsComponent },
    { path: 'add', component: ClientAddComponent },
    {
        path: ':id', component: ClientEditComponent, children: [
          { path: '', redirectTo: 'details', pathMatch: 'full' },
          { path: 'details', component: ClientDetailsComponent },
          { path: 'urls', component: ClientUrlsComponent }
        ]
      }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ClientsRoutingModule { }
