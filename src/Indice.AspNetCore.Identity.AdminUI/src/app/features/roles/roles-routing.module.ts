import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { RolesComponent } from './roles.component';
import { RoleAddComponent } from './add/role-add.component';
import { RoleEditComponent } from './edit/role-edit.component';
import { RoleResolverService } from './edit/role-resolver.service';

const routes: Routes = [
    { path: '', component: RolesComponent },
    { path: 'add', component: RoleAddComponent },
    { path: ':id/edit', component: RoleEditComponent, resolve: { role: RoleResolverService } }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
    providers: [RoleResolverService]
})
export class RolesRoutingModule { }
