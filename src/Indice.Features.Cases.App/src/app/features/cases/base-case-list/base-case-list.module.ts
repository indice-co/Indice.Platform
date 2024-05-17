import { NgModule } from "@angular/core";
import { CommonModule, DatePipe } from "@angular/common";
import { HttpClientModule } from "@angular/common/http";
import { BrowserModule } from "@angular/platform-browser";
import { RouterModule, RouteReuseStrategy } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { IndiceComponentsModule } from "@indice/ng-components";
import { SharedModule } from "src/app/shared/shared.module";
import { JsonSchemaFormModule } from "@ajsf-extended/core";
import { CustomRouteReuseStrategy } from '../../../shared/custom-route-reuse-strategy'; // Import the custom route reuse strategy
import { BaseCaseListComponent } from "./base-case-list.component";
import { CaseAssignmentComponent } from "../case-detail-page/case-assignment/case-assignment.component";
import { CaseTimelineComponent } from "../case-detail-page/case-timeline/case-timeline.component";
import { CaseUnassignmentComponent } from "../case-detail-page/case-unassignment/case-unassignment.component";
import { CaseDetailPageComponent } from '../case-detail-page/case-detail-page.component';
import { CaseFormComponent } from '../case-detail-page/case-form/case-form.component';
import { CaseCreatePageComponent } from '../case-create-page/case-create-page.component';
import { CaseDiscardDraftComponent } from '../case-detail-page/case-discard-draft/case-discard-draft.component';
import { CasePrintPdfComponent } from '../case-detail-page/case-print-pdf/case-print-pdf.component';
import { CaseWarningModalComponent } from "../../../shared/components/case-warning-modal/case-warning-modal.component";
import { QueriesModalComponent } from "src/app/shared/components/query-modal/query-modal.component";
import { QueriesPageComponent } from "../queries-page/queries-page.component";
import { GeneralCaseListComponent } from "../general-case-list/general-case-list.component";
import { CaseTypeCaseListComponent } from "../case-type-case-list/case-type-case-list.component";

@NgModule({
  declarations: [
    BaseCaseListComponent,
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
    GeneralCaseListComponent,
    CaseTypeCaseListComponent
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
    BaseCaseListComponent,
    CaseAssignmentComponent,
    CaseUnassignmentComponent,
    CaseTimelineComponent,
    CaseDetailPageComponent
  ],
  providers: [
    DatePipe,
    { provide: RouteReuseStrategy, useClass: CustomRouteReuseStrategy }
  ]
})
export class BaseCaseListModule { }
