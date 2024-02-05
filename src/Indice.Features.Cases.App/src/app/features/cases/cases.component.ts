import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BaseListComponent, FilterClause, Icons, IResultSet, ListViewType, MenuOption, ModalService, Operators, RouterViewAction, SearchOption, ViewAction } from '@indice/ng-components';
import { forkJoin, Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { settings } from 'src/app/core/models/settings';
import { CasePartial, CasePartialResultSet, CasesApiService, } from 'src/app/core/services/cases-api.service';
import { ParamsService } from 'src/app/core/services/params.service';
import { QueriesModalComponent } from 'src/app/shared/components/query-modal/query-modal.component';

@Component({
    selector: 'app-cases',
    templateUrl: './cases.component.html'
})
export class CasesComponent extends BaseListComponent<CasePartial> implements OnInit {
    public newItemLink = 'new-case';
    public formActions: ViewAction[] = [
        new RouterViewAction(Icons.EntryView, 'queries', 'rightpane', 'Οι αναζητήσεις μου', 'Οι αναζητήσεις μου'),
        new ViewAction('refresh', 'refresh', null, Icons.Refresh, 'Ανανέωση στοιχείων')
    ];
    public queryParamsHasFilter = false;
    public tableFilters = new TableFilters();
    public tableColumns = new TableColumns();

    constructor(
        private _route: ActivatedRoute,
        private _router: Router,
        private _api: CasesApiService,
        private _paramsService: ParamsService,
        private _modalService: ModalService
    ) {
        super(_route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'createdByWhen';
        this.sortdir = 'desc';
        this.sortOptions = [
            new MenuOption('Ημ. Υποβολής', 'createdByWhen')
        ];
    }

    public ngOnInit(): void {
        this.loadFilterSettings();
        this.loadColumnSettings();
        const storedParams = this._paramsService.getParams();
        if (storedParams) {
            this._router.navigate(['/cases'], { queryParams: storedParams });
        }
        // Are there any filters in queryParams?
        this._route.queryParams.subscribe((params: Params) => {
            this.queryParamsHasFilter = params['filter'] ? true : false;
        });
        forkJoin({
            caseTypes: this._api.getCaseTypes(),
            checkpointTypes: this._api.getDistinctCheckpointTypes()
        }).pipe(take(1)).subscribe(({ caseTypes, checkpointTypes }) => {
            const caseTypeSearchOption: SearchOption = {
                field: 'caseTypeCodes',
                name: 'ΤΥΠΟΣ ΑΙΤΗΣΗΣ',
                dataType: 'array',
                options: [],
                multiTerm: true
            }
            for (let caseType of caseTypes.items!) { // fill caseTypeSearchOption's SelectInputOptions
                caseTypeSearchOption.options?.push({ value: caseType.code, label: caseType?.title! })
            }
            const checkpointTypeSearchOption: SearchOption = {
                field: 'checkpointTypeCodes',
                name: 'ΤΡΕΧΟΝ ΣΗΜΕΙΟ ΕΛΕΓΧΟΥ',
                dataType: 'array',
                options: [],
                multiTerm: true
            }
            for (let checkpointType of checkpointTypes) { // fill checkpointTypeSearchOption's SelectInputOptions
                checkpointTypeSearchOption.options?.push({ value: checkpointType?.code, label: checkpointType?.title ?? checkpointType?.code! })
            }
            this.searchOptions = [];
            if (this.tableFilters.CustomerId) {
                this.searchOptions.push({
                    field: 'referenceNumber',
                    name: 'ΑΡΙΘΜΟΣ ΑΙΤΗΣΗΣ',
                    dataType: 'string'
                });
            }
            if (this.tableFilters.CustomerId) {
                this.searchOptions.push({
                    field: 'customerId',
                    name: 'ΚΩΔΙΚΟΣ ΠΕΛΑΤΗ',
                    dataType: 'string'
                });
            }
            if (this.tableFilters.CustomerName) {
                this.searchOptions.push({
                    field: 'customerName',
                    name: 'ΟΝΟΜΑ ΠΕΛΑΤΗ',
                    dataType: 'string'
                });
            }
            if (this.tableFilters.TaxId) {
                this.searchOptions.push({
                    field: 'TaxId', // this must be exactly the same "case-wise" with db's json property!
                    name: 'Α.Φ.Μ. ΠΕΛΑΤΗ',
                    dataType: 'string'
                });
            }
            if (this.tableFilters.GroupIds) {
                this.searchOptions.push({
                    field: 'groupIds',
                    name: 'ΑΡΙΘΜΟΣ ΚΑΤΑΣΤΗΜΑΤΟΣ',
                    dataType: 'string',
                    multiTerm: true
                });
            }
            if (this.tableFilters.DateRange) {
                this.searchOptions.push({
                    field: 'dateRange',
                    name: 'ΗΜ. ΥΠΟΒΟΛΗΣ',
                    dataType: 'daterange'
                });
            }
            if (this.tableFilters.CaseTypeCodes) {
                this.searchOptions.push(caseTypeSearchOption);
            }
            if (this.tableFilters.CheckpointTypeCodes) {
                this.searchOptions.push(checkpointTypeSearchOption);
            }
            // now that we have the searchOptions, call parent's ngOnInit!
            super.ngOnInit();
        });

        // independent call to fetch the case Types that the user can select for Case Creation
        this._api.getCaseTypes(true)
            .pipe(take(1))
            .subscribe(
                (caseTypesForCaseCreation: CasePartialResultSet) => {
                    if (caseTypesForCaseCreation.count !== 0) {
                        this.formActions.unshift(
                            new RouterViewAction(Icons.Add, this.newItemLink, 'rightpane', 'Υποβολή νέας αίτησης', 'Νέα αίτηση')
                        );
                    }
                }
            );
    }

    loadFilterSettings(): void {
        if (settings.caseListFilters === '') return;
        const filters = settings.caseListFilters.split(',')

        this.tableFilters.ReferenceNumber = filters.some(filter => filter === "ReferenceNumber");
        this.tableFilters.CustomerId = filters.some(filter => filter === "CustomerId");
        this.tableFilters.CustomerName = filters.some(filter => filter === "CustomerName");
        this.tableFilters.TaxId = filters.some(filter => filter === "TaxId");
        this.tableFilters.GroupIds = filters.some(filter => filter === "GroupIds");
        this.tableFilters.DateRange = filters.some(filter => filter === "DateRange");
        this.tableFilters.CaseTypeCodes = filters.some(filter => filter === "CaseTypeCodes");
        this.tableFilters.CheckpointTypeCodes = filters.some(filter => filter === "CheckpointTypeCodes");
    }

    loadColumnSettings(): void {
        if (settings.caseListColumns === '') return;
        const columns = settings.caseListColumns.split(',')

        this.tableColumns.ReferenceNumber = columns.some(column => column === "ReferenceNumber");
        this.tableColumns.CustomerId = columns.some(column => column === "CustomerId");
        this.tableColumns.CustomerName = columns.some(column => column === "CustomerName");
        this.tableColumns.TaxId = columns.some(column => column === "TaxId");
        this.tableColumns.GroupId = columns.some(column => column === "GroupId");
        this.tableColumns.CaseType = columns.some(column => column === "CaseType");
        this.tableColumns.CheckpointType = columns.some(column => column === "CheckpointType");
        this.tableColumns.AssignedTo = columns.some(column => column === "AssignedTo");
        this.tableColumns.SubmitDate = columns.some(column => column === "SubmitDate");
    }

    openQueryModal(): void {
        this._modalService.show(QueriesModalComponent, {
            backdrop: 'static',
            keyboard: false
        });
    }

    loadItems(): Observable<IResultSet<CasePartial> | null | undefined> {
        let customerIds: string[] = [];
        this.filters?.filter(f => f.member === 'customerId')?.forEach(f => customerIds.push(this.stringifyFilterClause(f)));
        let customerNames: string[] = [];
        this.filters?.filter(f => f.member === 'customerName')?.forEach(f => customerNames.push(this.stringifyFilterClause(f)));
        let referenceNumbers: string[] = [];
        this.filters?.filter(f => f.member === 'referenceNumber')?.forEach(f => referenceNumbers.push(this.stringifyFilterClause(f)));
        let groupIds: string[] = [];
        this.filters?.filter(f => f.member === 'groupIds')?.forEach(f => groupIds?.push(this.stringifyFilterClause(f)));
        let from = this.filters?.find(f => f.member === 'dateRange' && f.operator === Operators.GREATER_THAN_EQUAL.value as FilterClause.Op)?.value;
        let to = this.filters?.find(f => f.member === 'dateRange' && f.operator === Operators.LESS_THAN_EQUAL.value as FilterClause.Op)?.value;
        let caseTypeCodes: string[] = [];
        this.filters?.filter(f => f.member === 'caseTypeCodes')?.forEach(f => caseTypeCodes?.push(this.stringifyFilterClause(f)));
        let checkpointTypeCodes: string[] = [];
        this.filters?.filter(f => f.member === 'checkpointTypeCodes')?.forEach(f => checkpointTypeCodes?.push(this.stringifyFilterClause(f)));
        let filterMetadata: string[] = [];
        this.filters?.filter(f => f.member === 'TaxId')?.forEach(f => filterMetadata?.push(`metadata.${this.stringifyFilterClause(f)}`));
        this._paramsService.setParams({
            view: this.view,
            page: this.page,
            pagesize: this.pageSize,
            search: this.search,
            sort: this.sort,
            dir: this.sortdir,
            filter: this.stringifyFilters(this.filters)
        });
        return this._api
            .getCases(
                customerIds,
                customerNames,
                from ? new Date(from) : undefined,
                to ? new Date(to) : undefined,
                caseTypeCodes,
                checkpointTypeCodes,
                groupIds,
                filterMetadata,
                referenceNumbers,
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

    // TODO: make this public in Indice.Angular
    private stringifyFilters(filters: FilterClause[] | undefined) {
        return filters?.map((f: FilterClause) => {
            if (f.dataType === 'datetime') {
                f.value = (new Date(f.value)).toISOString();
            }
            return f.toString();
        }).join(',');
    }

    private stringifyFilterClause(filter: FilterClause): string {
        return `${filter.member}::${filter.operator}::${filter.value}`;
    }
}

class TableFilters {
    ReferenceNumber: boolean = false;
    CustomerId: boolean = true;
    CustomerName: boolean = true;
    TaxId: boolean = true;
    GroupIds: boolean = true;
    DateRange: boolean = true;
    CaseTypeCodes: boolean = true;
    CheckpointTypeCodes: boolean = true;
}

class TableColumns {
    ReferenceNumber: boolean = false;
    CustomerId: boolean = true;
    CustomerName: boolean = true;
    TaxId: boolean = true;
    GroupId: boolean = true;
    CaseType: boolean = true;
    CheckpointType: boolean = true;
    AssignedTo: boolean = true;
    SubmitDate: boolean = true;
}
