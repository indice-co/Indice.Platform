import { JsonSchemaFormModule } from "@ajsf-extended/core";
import { CommonModule, DatePipe } from "@angular/common";
import { HttpClientModule } from "@angular/common/http";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { BrowserModule } from "@angular/platform-browser";
import { RouterModule } from "@angular/router";
import { IndiceComponentsModule } from "@indice/ng-components";
import { SharedModule } from "src/app/shared/shared.module";
import { CasesBase } from "./cases.base.component";
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
import { CasesComponent } from "./cases/cases.component";
import { CasesTypeMenuItemsComponent } from "./case-type-menu-item/case-type-menu-item.component";

@NgModule({
  declarations: [
    CasesBase,
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
    CasesComponent,
    CasesTypeMenuItemsComponent
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
    CasesBase,
    CaseAssignmentComponent,
    CaseUnassignmentComponent,
    CaseTimelineComponent,
    CaseDetailPageComponent
  ],
  providers: [DatePipe]
})
export class CasesBaseModule { }