import { JsonSchemaFormModule } from "@ajsf-extended/core";
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
import { CaseDiscardDraftComponent } from './case-detail-page/case-discard-draft/case-discard-draft.component';
import { CasePrintPdfComponent } from './case-detail-page/case-print-pdf/case-print-pdf.component';
import { CaseWarningModalComponent } from "../../shared/components/case-warning-modal/case-warning-modal.component";
import { FiltersModalComponent } from "src/app/shared/components/filter-modal/filter-modal.component";
import { MyFiltersPageComponent } from "./my-filters-page/my-filters-page.component";

@NgModule({
    declarations: [
        CasesComponent,
        CaseAssignmentComponent,
        CaseUnassignmentComponent,
        CaseTimelineComponent,
        CaseDetailPageComponent,
        CaseFormComponent,
        CaseCreatePageComponent,
        MyFiltersPageComponent,
        CaseDiscardDraftComponent,
        CasePrintPdfComponent,
        CaseWarningModalComponent,
        FiltersModalComponent
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