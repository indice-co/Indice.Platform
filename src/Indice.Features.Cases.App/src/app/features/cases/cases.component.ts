import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, RouterViewAction, ViewAction } from '@indice/ng-components';
import { SearchOption } from '@indice/ng-components/lib/controls/advanced-search/models';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { CasePartial, CasePartialResultSet, CasesApiService, CaseType } from 'src/app/core/services/cases-api.service';

@Component({
    selector: 'app-cases',
    templateUrl: './cases.component.html'
})
export class CasesComponent extends BaseListComponent<CasePartial> implements OnInit {
    public newItemLink = 'new-case';
    public formActions: ViewAction[] = [
        new RouterViewAction(Icons.Add, this.newItemLink, 'rightpane', 'υποβολή νέας αίτησης')
    ];

    constructor(
        private _route: ActivatedRoute,
        private _router: Router,
        private _api: CasesApiService
    ) {
        super(_route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'createdByWhen';
        this.sortdir = 'asc';
        this.sortOptions = [
            new MenuOption('Ημ. Υποβολής', 'createdByWhen')
        ];
    }

    public ngOnInit(): void {
        this._api.getCaseTypes()
            .pipe(
                take(1)
            )
            .subscribe(
                (caseTypes: CaseType[]) => {
                    const caseTypeSearchOption: SearchOption = {
                        field: 'caseTypeCodes',
                        name: 'Τύπος αίτησης',
                        dataType: 'array',
                        options: [],
                        multiTerm: true
                    }
                    for (let caseType of caseTypes) { // fill caseTypeSearchOption's SelectInputOptions
                        caseTypeSearchOption.options?.push({ value: caseType.code, label: caseType?.title! })
                    }
                    this.searchOptions = [
                        {
                            field: 'customerId',
                            name: 'Κωδικός Πελάτη',
                            dataType: 'string'
                        },
                        {
                            field: 'customerName',
                            name: 'Όνομα Πελάτη',
                            dataType: 'string'
                        },
                        {
                            field: 'TaxId', // this must be exactly the same "case-wise" with db's json property!
                            name: 'ΑΦΜ Πελάτη',
                            dataType: 'string'
                        },
                        {
                            field: 'dateRange',
                            name: 'Ημ/νία υποβολής',
                            dataType: 'daterange'
                        },
                        caseTypeSearchOption
                    ];
                    // now that we have the searchOptions, call parent's ngOnInit!
                    super.ngOnInit();
                }
            );
    }

    loadItems(): Observable<IResultSet<CasePartial> | null | undefined> {
        let customerId = this.filters?.find(f => f.member === 'customerId')?.value;
        let customerName = this.filters?.find(f => f.member === 'customerName')?.value;
        let from = this.filters?.find(f => f.member === 'from')?.value;
        let to = this.filters?.find(f => f.member === 'to')?.value;
        let caseTypeCodes: string[] = [];
        this.filters?.filter(f => f.member === 'caseTypeCodes')?.forEach(f => caseTypeCodes?.push(f.value));
        let filterMetadata: string[] = [];
        this.filters?.filter(f => f.member === 'TaxId')?.forEach(f => filterMetadata?.push(`metadata.${f.member}::eq::(${f.dataType})${f.value}`)); // this is the form that the server accepts

        return this._api
            .getCases(
                customerId,
                customerName,
                from ? new Date(from) : undefined,
                to ? new Date(to) : undefined,
                caseTypeCodes,
                undefined,
                undefined,
                filterMetadata,
                this.page,
                this.pageSize,
                this.sortdir === 'asc' ? this.sort! : this.sort + '-',
                this.search || undefined,
                undefined
            )
            .pipe(
                take(1),
                map((result: CasePartialResultSet) => (result as IResultSet<CasePartial>))
            );
    }
}
