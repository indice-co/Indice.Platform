import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HttpClientModule } from "@angular/common/http";
import { BrowserModule } from "@angular/platform-browser";
import { RouterModule } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { IndiceComponentsModule } from "@indice/ng-components";
import { SharedModule } from "src/app/shared/shared.module";
import { JsonSchemaFormModule } from "@ajsf-extended/core";
import { CaseAssignmentComponent } from "./case-detail-page/case-assignment/case-assignment.component";
import { CaseTimelineComponent } from "./case-detail-page/case-timeline/case-timeline.component";
import { CaseUnassignmentComponent } from "./case-detail-page/case-unassignment/case-unassignment.component";
import { CaseDetailPageComponent } from './case-detail-page/case-detail-page.component';
import { CaseFormComponent } from './case-detail-page/case-form/case-form.component';
import { CaseCreatePageComponent } from './case-create-page/case-create-page.component';
import { CaseDiscardDraftComponent } from './case-detail-page/case-discard-draft/case-discard-draft.component';
import { CasePrintPdfComponent } from './case-detail-page/case-print-pdf/case-print-pdf.component';
import { CaseWarningModalComponent } from "../../shared/components/case-warning-modal/case-warning-modal.component";
import { QueriesModalComponent } from "src/app/shared/components/query-modal/query-modal.component";
import { QueriesPageComponent } from "./queries-page/queries-page.component";
import { GeneralCasesComponent } from "./general-cases/general-cases.component";
import { CaseTypeSpecificCasesComponent } from "./case-type-specific-cases/case-type-specific-cases.component";
import { RelatedCasesComponent } from "./case-detail-page/related-cases/related-cases.component";
import { ToReadableDatePipe, ValueFromPathPipe } from "src/app/shared/pipes.services";

@NgModule({
  declarations: [
    CaseAssignmentComponent,
    CaseUnassignmentComponent,
    CaseTimelineComponent,
    CaseDetailPageComponent,
    CaseFormComponent,
    CaseCreatePageComponent,
    QueriesPageComponent,
    CaseDiscardDraftComponent,
    CasePrintPdfComponent,
    CaseWarningModalComponent,
    QueriesModalComponent,
    GeneralCasesComponent,
    CaseTypeSpecificCasesComponent,
    RelatedCasesComponent
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
    CaseAssignmentComponent,
    CaseUnassignmentComponent,
    CaseTimelineComponent,
    CaseDetailPageComponent
  ],
  providers: [
    ValueFromPathPipe,
    ToReadableDatePipe
  ]
})
export class CasesModule { }
