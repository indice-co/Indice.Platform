import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CasesApiService, Contact } from 'src/app/core/services/cases-api.service';
@Component({
  selector: 'app-search-customer',
  templateUrl: './search-customer.component.html',
  styleUrls: ['./search-customer.component.scss']
})
export class SearchCustomerComponent {
  @Input() caseTypeCode: string | undefined;
  @Output() selectedContactEvent = new EventEmitter<Contact>();

  public searchValue: string | undefined
  public results$: Observable<Selectable<Contact>[]> | undefined;

  constructor(public api: CasesApiService) { }

  // when user clicks search, the obs of results is
  // set to be drawn by the view
  onSearch() {
    this.results$ = this.api.getCustomers(this.searchValue, this.caseTypeCode)
      .pipe(
        map((res) => {
          return res.items?.map(c => new Selectable(c)) ?? [];
        }));
  }

  // on select, the customer
  // from the ng-for loop that 
  // draws customer line results
  // is emitted to parents
  onSelect(value: Selectable<Contact>, allValues: Selectable<Contact>[]) {
    allValues.forEach(e => { e.selected = false });
    value.selected = true;
    this.selectedContactEvent.emit(value.item);
  }
}
class Selectable<T> {
  constructor(public item: T) { }
  public selected: boolean = false;
}
