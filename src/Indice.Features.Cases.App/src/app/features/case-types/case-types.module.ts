import { JsonSchemaFormModule } from "@ajsf-extended/core";
import { CommonModule } from "@angular/common";
import { HttpClientModule } from "@angular/common/http";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { BrowserModule } from "@angular/platform-browser";
import { RouterModule } from "@angular/router";
import { IndiceComponentsModule } from "@indice/ng-components";
import { SharedModule } from "src/app/shared/shared.module";
import { CaseTypeCreateComponent } from "./case-type-create/case-type-create.component";
import { CaseTypeEditComponent } from "./case-type-edit/case-type-edit.component";
import { CaseTypesComponent } from "./case-types.component";
import { CaseTypeUpdateService } from "./case-type-update.service";
import { CaseTypeDeleteModalComponent } from './case-type-delete-modal/case-type-delete-modal.component';

@NgModule({
    declarations: [
        CaseTypesComponent,
        CaseTypeCreateComponent,
        CaseTypeEditComponent,
        CaseTypeDeleteModalComponent
    ],
    imports: [
        BrowserModule,
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,
        SharedModule,
        JsonSchemaFormModule,
        IndiceComponentsModule
    ],
    providers: [
        CaseTypeUpdateService
    ]
})
export class CaseTypesModule { }
