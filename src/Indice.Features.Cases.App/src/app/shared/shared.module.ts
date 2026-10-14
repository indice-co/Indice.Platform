import { CommonModule } from '@angular/common';
import { provideHttpClient, withInterceptorsFromDi, withXhr } from '@angular/common/http';
import { ApprovalButtonsComponent } from './components/approval-buttons/approval-buttons.component';
import { PageIllustrationComponent } from './components/page-illustration/page-illustration.component';
import { RadioButtonsListComponent } from './components/radio-buttons-list/radio-buttons-list.component';
import { SelectCaseTypeComponent } from './components/select-case-type/select-case-type.component';
import { SearchContactComponent } from './components/search-contact/search-contact.component';
import { IndiceComponentsModule } from '@indice/ng-components';
import { CaseDetailInfoComponent } from './components/case-detail-info/case-detail-info.component';
import { CaseCustomActionComponent } from './components/case-custom-action/case-custom-action.component';
import { CanvasTileComponent } from './components/canvas-tile/canvas-tile.component';
import { DisplayCaseTypesComponent } from './components/select-case-type/display-case-types/display-case-types.component';
import { DeleteQueryModalComponent } from './components/delete-query-modal/delete-query-modal.component';
import { TranslateModule } from '@ngx-translate/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgModule } from '@angular/core';
import { QuillModule } from 'ngx-quill';
import { NgxMaskDirective, NgxMaskPipe, provideNgxMask } from 'ngx-mask';
import { BeautifyBooleanPipe, ValueFromPathPipe, ToReadableDatePipe } from './pipes.services';
import { JsonFormsTailwindModule } from './jsonforms/jsonforms-tailwind.module';

@NgModule({ declarations: [
        // components
        ApprovalButtonsComponent,
        PageIllustrationComponent,
        RadioButtonsListComponent,
        SelectCaseTypeComponent,
        SearchContactComponent,
        CaseDetailInfoComponent,
        CaseCustomActionComponent,
        CanvasTileComponent,
        DisplayCaseTypesComponent,
        DeleteQueryModalComponent,
        // pipes
        BeautifyBooleanPipe,
        ValueFromPathPipe,
        ToReadableDatePipe
    ],
    exports: [
        // components
        ApprovalButtonsComponent,
        PageIllustrationComponent,
        RadioButtonsListComponent,
        SelectCaseTypeComponent,
        SearchContactComponent,
        CaseDetailInfoComponent,
        CaseCustomActionComponent,
        CanvasTileComponent,
        // jsonforms
        JsonFormsTailwindModule,
        // pipes
        BeautifyBooleanPipe,
        TranslateModule,
        ValueFromPathPipe,
        ToReadableDatePipe
    ], imports: [CommonModule,
        FormsModule,
        ReactiveFormsModule,
        JsonFormsTailwindModule,
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
        NgxMaskPipe], providers: [
        provideNgxMask(),
        provideHttpClient(withXhr(), withInterceptorsFromDi())
    ] })
export class SharedModule { }
