import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ClaimTypesComponent } from './claim-types.component';
import { ClaimTypeAddComponent } from './add/claim-type-add.component';
import { ClaimTypeEditComponent } from './edit/claim-type-edit.component';
import { ClaimTypeResolverService } from './edit/claim-type-resolver.service';

const routes: Routes = [
  { path: '', component: ClaimTypesComponent },
  { path: 'add', component: ClaimTypeAddComponent },
  { path: ':id/edit', component: ClaimTypeEditComponent, resolve: { claimType: ClaimTypeResolverService } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
  providers: [ClaimTypeResolverService]
})
export class ClaimTypesRoutingModule { }
