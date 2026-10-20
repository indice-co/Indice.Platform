import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { IndiceComponentsModule } from '@indice/ng-components';
import { SharedModule } from 'src/app/shared/shared.module';
import { AccessRulesComponent } from './access-rules.component';
import { AccessRuleCreateComponent } from './access-rule-create/access-rule-create.component';
import { AccessRuleDeleteModalComponent } from './access-rule-delete-modal/access-rule-delete-modal.component';

@NgModule({
    declarations: [
        AccessRulesComponent,
        AccessRuleCreateComponent,
        AccessRuleDeleteModalComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        SharedModule,
        IndiceComponentsModule
    ]
})
export class AccessRulesModule { }
