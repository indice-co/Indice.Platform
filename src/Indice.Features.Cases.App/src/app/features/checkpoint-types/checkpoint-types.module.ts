import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { IndiceComponentsModule } from '@indice/ng-components';
import { SharedModule } from 'src/app/shared/shared.module';
import { CheckpointTypesComponent } from './checkpoint-types.component';
import { CheckpointTypeEditComponent } from './checkpoint-type-edit/checkpoint-type-edit.component';
import { CheckpointTypeDeleteModalComponent } from './checkpoint-type-delete-modal/checkpoint-type-delete-modal.component';

@NgModule({
    declarations: [
        CheckpointTypesComponent,
        CheckpointTypeEditComponent,
        CheckpointTypeDeleteModalComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        SharedModule,
        IndiceComponentsModule
    ],
    exports: [CheckpointTypesComponent]
})
export class CheckpointTypesModule { }
