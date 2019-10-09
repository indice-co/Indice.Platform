import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { SharedModule } from 'src/app/shared/shared.module';
import { ClaimTypesComponent } from './claim-types.component';
import { ClaimTypesRoutingModule } from './claim-types-routing.module';
import { ClaimTypeAddComponent } from './add/claim-type-add.component';
import { ClaimTypeEditComponent } from './edit/claim-type-edit.component';

@NgModule({
    declarations: [
        ClaimTypesComponent,
        ClaimTypeAddComponent,
        ClaimTypeEditComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        ClaimTypesRoutingModule,
        SharedModule,
        SweetAlert2Module
    ]
})
export class ClaimTypesModule { }
