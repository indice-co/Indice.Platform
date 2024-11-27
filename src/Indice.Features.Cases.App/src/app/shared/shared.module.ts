import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { ApprovalButtonsComponent } from './components/approval-buttons/approval-buttons.component';
import { PageIllustrationComponent } from './components/page-illustration/page-illustration.component';
import { RadioButtonsListComponent } from './components/radio-buttons-list/radio-buttons-list.component';
import { TailwindFrameworkComponent } from './ajsf/json-schema-frameworks/tailwind-framework/tailwind-framework.component';
import { JsonSchemaFormModule } from '@ajsf-extended/core';
import { SubmitWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/submit-widget/submit-widget.component';
import { SelectCaseTypeComponent } from './components/select-case-type/select-case-type.component';
import { SearchCustomerComponent } from './components/search-customer/search-customer.component';
import { IndiceComponentsModule } from '@indice/ng-components';
import { SelectWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/select-widget/select-widget.component';
import { CurrencyWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/currency-widget/currency-widget.component';
import { DateWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/date-widget/date-widget.component';
import { LookupWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/lookup-widget/lookup-widget.component';
import { InputWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/input-widget/input-widget.component';
import { TextAreaWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/text-area-widget/text-area-widget.component';
import { FileWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/file-widget/file-widget.component';
import { CaseDetailInfoComponent } from './components/case-detail-info/case-detail-info.component';
import { CaseCustomActionComponent } from './components/case-custom-action/case-custom-action.component';
import { LookupSelectorWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/lookup-selector-widget/lookup-selector-widget.component';
import { CanvasTileComponent } from './components/canvas-tile/canvas-tile.component';
import { DisplayCaseTypesComponent } from './components/select-case-type/display-case-types/display-case-types.component';
import { WysiwygWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/wysiwyg-widget/wysiwyg-widget.component';
import { DeleteQueryModalComponent } from './components/delete-query-modal/delete-query-modal.component';
import { TranslateModule } from '@ngx-translate/core';
import { HrefWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/href-widget/href-widget.component';
import { LabelOnlyWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/label-only-widget/label-only-widget.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgModule } from '@angular/core';
import { QuillModule } from 'ngx-quill';
import { NgxMaskDirective, NgxMaskPipe, provideNgxMask } from 'ngx-mask';
import { BeautifyBooleanPipe, ValueFromPathPipe, ToReadableDatePipe } from './pipes.services';

@NgModule({
  declarations: [
    // components
    ApprovalButtonsComponent,
    PageIllustrationComponent,
    RadioButtonsListComponent,
    SelectCaseTypeComponent,
    SearchCustomerComponent,
    CaseDetailInfoComponent,
    CaseCustomActionComponent,
    CanvasTileComponent,
    DisplayCaseTypesComponent,
    DeleteQueryModalComponent,
    // ajsf
    FileWidgetComponent,
    TailwindFrameworkComponent,
    SelectWidgetComponent,
    SubmitWidgetComponent,
    CurrencyWidgetComponent,
    DateWidgetComponent,
    LookupWidgetComponent,
    InputWidgetComponent,
    TextAreaWidgetComponent,
    LookupSelectorWidgetComponent,
    WysiwygWidgetComponent,
    HrefWidgetComponent,
    LabelOnlyWidgetComponent,
    // pipes
    BeautifyBooleanPipe,
    ValueFromPathPipe,
    ToReadableDatePipe
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    JsonSchemaFormModule,
    IndiceComponentsModule,
    QuillModule.forRoot({
      modules: {
        toolbar: [
          ['bold', 'italic', 'underline'],
          [{ 'align': [] }],
          [{ 'list': 'ordered' }, { 'list': 'bullet' }],
          [{ 'indent': '-1' }, { 'indent': '+1' }],
          ['clean'],
        ]
      }
    }),
    TranslateModule,
    NgxMaskDirective,
    NgxMaskPipe
  ],
  exports: [
    // components
    ApprovalButtonsComponent,
    PageIllustrationComponent,
    RadioButtonsListComponent,
    SelectCaseTypeComponent,
    SearchCustomerComponent,
    CaseDetailInfoComponent,
    CaseCustomActionComponent,
    CanvasTileComponent,
    // ajsf
    TailwindFrameworkComponent,
    // pipes
    BeautifyBooleanPipe,
    TranslateModule,
    ValueFromPathPipe,
    ToReadableDatePipe
  ],
  providers: [
    provideNgxMask()
  ]
})
export class SharedModule { }
