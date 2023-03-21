import { Component, Input, OnInit, EventEmitter, Output } from '@angular/core';
import { CaseTypePartial } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-display-case-types',
  templateUrl: './display-case-types.component.html',
  styleUrls: ['./display-case-types.component.scss']
})
export class DisplayCaseTypesComponent implements OnInit {
  @Input() public caseTypes: CaseTypePartial[] = [];
  @Output() selectedCaseTypeEvent = new EventEmitter<CaseTypePartial>();

  public displayCaseTypesViewModel: DisplayCaseTypesViewModel | undefined;
  public noCategoryName: string = 'ΛΟΙΠΕΣ';

  constructor() { }

  ngOnInit(): void {
    this.displayCaseTypesViewModel = this.createDisplayCaseTypesViewModel(this.caseTypes);
  }

  public onSelect(caseType: any) {
    this.selectedCaseTypeEvent.emit(caseType)
  }

  private createDisplayCaseTypesViewModel(caseTypes: CaseTypePartial[]): DisplayCaseTypesViewModel {
    const categoriesMap = new Map<string, CaseTypeCategoryViewModel>();
    for (const caseType of caseTypes) {
      this.addCaseTypeToCategory(categoriesMap, caseType);
    }
    return { categories: Array.from(categoriesMap.values()) };
  }

  private addCaseTypeToCategory(categoriesMap: Map<string, CaseTypeCategoryViewModel>, caseType: CaseTypePartial) {
    let categoryName = caseType?.category?.name ?? this.noCategoryName
    if (!categoriesMap.has(categoryName)) {
      categoriesMap.set(
        categoryName,
        { name: categoryName, caseTypes: [] }
      );
    }
    categoriesMap.get(categoryName)!.caseTypes!.push(caseType);
  }
}

export class CaseTypeCategoryViewModel {
  public name: string | undefined;
  public caseTypes: CaseTypePartial[] | undefined;
}

export class DisplayCaseTypesViewModel {
  public categories: CaseTypeCategoryViewModel[] | undefined;
}