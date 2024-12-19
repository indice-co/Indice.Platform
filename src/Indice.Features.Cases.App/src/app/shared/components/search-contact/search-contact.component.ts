import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CasesApiService, Contact } from 'src/app/core/services/cases-api.service';
@Component({
    selector: 'app-search-contact',
    templateUrl: './search-contact.component.html',
    styleUrls: ['./search-contact.component.scss']
})
export class SearchContactComponent {
    @Input() caseTypeCode: string | undefined;
    @Output() selectedContactEvent = new EventEmitter<Contact>();

    public searchValue: string | undefined
    public results$: Observable<Selectable<Contact>[]> | undefined;

    constructor(public api: CasesApiService) { }

    // when user clicks search, the obs of results is
    // set to be drawn by the view
    onSearch() {
        this.results$ = this.api.getContacts(this.searchValue, this.caseTypeCode)
            .pipe(
                map((res) => {
                    return res.items?.map(c => new Selectable(c)) ?? [];
                }));
    }

    // on select, the contact
    // from the ng-for loop that 
    // draws contact line results
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
