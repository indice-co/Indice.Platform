import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ClientsComponent } from './clients.component';
import { ClientAddComponent } from './add/client-add.component';

const routes: Routes = [
    { path: '', component: ClientsComponent },
    { path: 'add', component: ClientAddComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ClientsRoutingModule { }
