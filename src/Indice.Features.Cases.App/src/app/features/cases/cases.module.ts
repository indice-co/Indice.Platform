import { JsonSchemaFormModule } from "@ajsf/core";
import { CommonModule } from "@angular/common";
import { HttpClientModule } from "@angular/common/http";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { BrowserModule } from "@angular/platform-browser";
import { RouterModule } from "@angular/router";
import { IndiceComponentsModule } from "@indice/ng-components";
import { SharedModule } from "src/app/shared/shared.module";
import { CasesComponent } from "./cases.component";
import { CaseAssignmentComponent } from "./case-detail-page/case-assignment/case-assignment.component";
import { CaseTimelineComponent } from "./case-detail-page/case-timeline/case-timeline.component";
import { CaseUnassignmentComponent } from "./case-detail-page/case-unassignment/case-unassignment.component";
import { CaseDetailPageComponent } from './case-detail-page/case-detail-page.component';
import { CaseFormComponent } from './case-detail-page/case-form/case-form.component';
import { CaseCreatePageComponent } from './case-create-page/case-create-page.component';

@NgModule({
    declarations: [
        CasesComponent,
        CaseAssignmentComponent,
        CaseUnassignmentComponent,
        CaseTimelineComponent,
        CaseDetailPageComponent,
        CaseFormComponent,
        CaseCreatePageComponent
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
    exports: [
        CasesComponent,
        CaseAssignmentComponent,
        CaseUnassignmentComponent,
        CaseTimelineComponent,
        CaseDetailPageComponent
    ]
})
export class CasesModule { }