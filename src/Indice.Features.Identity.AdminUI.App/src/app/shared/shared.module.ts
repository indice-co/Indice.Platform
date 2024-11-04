import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import { NgbDatepickerModule, NgbDateParserFormatter, NgbDatepickerConfig, NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { ListViewComponent } from './components/list-view/list-view.component';
import { RouterLinkMatchDirective } from './directives/router-link-match.directive';
import { DynamicInputComponent } from './components/dynamic-input/dynamic-input.component';
import { NgbDateCustomParserFormatter } from './services/custom-parser-formatter.service';
import { TransferListsComponent } from './components/transfer-lists/transfer-lists.component';
import { ValidationSummaryComponent } from './components/validation-summary/validation-summary.component';
import { WizardStepDirective } from './components/step-base/wizard-step.directive';
import { FormReadonlyDirective } from './directives/form-readonly.directive';
import { TranslateInputComponent } from './components/translate-input/translate-input.component';
import { CKEditorModule } from '@ckeditor/ckeditor5-angular';
import { ImgUserPictureDirective } from './directives/user-picture.directive';

@NgModule({
    declarations: [
        ListViewComponent,
        DynamicInputComponent,
        TranslateInputComponent,
        TransferListsComponent,
        ValidationSummaryComponent,
        RouterLinkMatchDirective,
        WizardStepDirective,
        FormReadonlyDirective,
        ImgUserPictureDirective
    ],
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        NgxDatatableModule,
        NgbDatepickerModule,
        NgbDropdownModule,
        CKEditorModule
    ],
    exports: [
        ListViewComponent,
        DynamicInputComponent,
        TranslateInputComponent,
        TransferListsComponent,
        ValidationSummaryComponent,
        RouterLinkMatchDirective,
        WizardStepDirective,
        FormReadonlyDirective,
        ImgUserPictureDirective
    ],
    providers: [
        NgbDatepickerConfig,
        { provide: NgbDateParserFormatter, useClass: NgbDateCustomParserFormatter }
    ]
})
export class SharedModule {
    constructor(config: NgbDatepickerConfig) {
        // Customize default values of date pickers used by this component tree.
        config.minDate = { year: 1900, month: 1, day: 1 };
        config.maxDate = { year: 2099, month: 12, day: 31 };
    }
}
