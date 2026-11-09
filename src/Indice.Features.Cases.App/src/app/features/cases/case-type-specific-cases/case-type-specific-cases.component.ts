import { Component } from '@angular/core';
import { CASES_LIST_IMPORTS, GeneralCasesComponent } from '../general-cases/general-cases.component';

/**
 * Cases of a single case type (`/case/by-type/:caseTypeCode`). All behaviour lives in {@link GeneralCasesComponent},
 * driven by its `caseTypeCode` input which the router binds from the route param.
 */
@Component({
  selector: 'app-case-type-specific-cases',
  imports: CASES_LIST_IMPORTS,
  templateUrl: '../general-cases/general-cases.component.html'
})
export class CaseTypeSpecificCasesComponent extends GeneralCasesComponent { }
