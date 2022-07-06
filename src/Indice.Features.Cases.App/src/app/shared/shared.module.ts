import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ApprovalButtonsComponent } from './components/approval-buttons/approval-buttons.component';
import { PageIllustrationComponent } from './components/page-illustration/page-illustration.component';
import { RadioButtonsListComponent } from './components/radio-buttons-list/radio-buttons-list.component';
import { ToggleButtonComponent } from './components/toggle-button/toggle-button.component';
import { BeautifyBooleanPipe, RemovePrefixPipe } from './pipes.services';
import { JSFFileWidgetComponent } from './ajsf/jsf-file-widget.component';
import { TailwindFrameworkComponent } from './ajsf/json-schema-frameworks/tailwind-framework/tailwind-framework.component';
import { JsonSchemaFormModule } from '@ajsf/core';
import { TailwindSubmitWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/submit-widget/submit-widget.component';
import { SelectCaseTypeComponent } from './components/select-case-type/select-case-type.component';
import { SearchCustomerComponent } from './components/search-customer/search-customer.component';
import { IndiceComponentsModule } from '@indice/ng-components';
import { SelectWidgetComponent } from './ajsf/json-schema-frameworks/tailwind-framework/select-widget/select-widget.component';



@NgModule({
  declarations: [
    // components
    ApprovalButtonsComponent,
    PageIllustrationComponent,
    RadioButtonsListComponent,
    ToggleButtonComponent,
    SelectCaseTypeComponent,
    SearchCustomerComponent,
    // ajsf
    JSFFileWidgetComponent,
    TailwindFrameworkComponent,
    SelectWidgetComponent,
    TailwindSubmitWidgetComponent,
    // pipes
    BeautifyBooleanPipe,
    RemovePrefixPipe
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule,
    JsonSchemaFormModule,
    IndiceComponentsModule
  ],
  exports: [
    // components
    ApprovalButtonsComponent,
    PageIllustrationComponent,
    RadioButtonsListComponent,
    ToggleButtonComponent,
    SelectCaseTypeComponent,
    SearchCustomerComponent,
    // ajsf
    JSFFileWidgetComponent,
    TailwindSubmitWidgetComponent,
    TailwindFrameworkComponent,
    // pipes
    BeautifyBooleanPipe,
    RemovePrefixPipe
  ]
})
export class SharedModule { }
