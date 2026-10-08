import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BaseListComponent, FilterClause, Icons, IResultSet, ListViewType, MenuOption, ModalService, Operators, RouterViewAction, SearchOption, ViewAction } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin, Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { settings } from 'src/app/core/models/settings';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasePartial, CasePartialResultSet, CasesApiService, CaseTypePartialResultSet, CheckpointType, } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { QueriesModalComponent } from 'src/app/shared/components/query-modal/query-modal.component';

@Component({
    selector: 'app-general-cases-component',
    templateUrl: './general-cases.component.html',
    standalone: false
})
export class GeneralCasesComponent extends BaseListComponent<CasePartial> implements OnInit {
    public newItemLink = 'new-case';
    public formActions: ViewAction[] = [];
    public queryParamsHasFilter = false;
    public tableFilters = new TableFilters();
    protected caseTypes: CaseTypePartialResultSet | undefined;
    /** Cached so search-option labels can be rebuilt on language change without re-fetching. */
    protected _checkpointTypes: CheckpointType[] = [];
    /** Whether the user can create cases — controls the "new case" form action. */
    protected _canCreateCase = false;
    public caseTypeTitle: string = "";
    public columns = this.setDefaultColumns();
    private destroyRef = inject(DestroyRef);

    constructor(
        protected _route: ActivatedRoute,
        protected _router: Router,
        protected _api: CasesApiService,
        protected _filterCachingService: FilterCachingService,
        protected _modalService: ModalService,
        protected _caseTypeService: CaseTypeService,
        protected _translate: TranslateService,
        protected _lang: AppLanguagesService
    ) {
        super(_route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'createdByWhen';
        this.sortdir = 'desc';
        this.buildSortOptions();
        this.buildFormActions();
        this.loadFilterSettings();
    }

    public ngOnInit(): void {
        this.initialize();
        this.createNewCaseButton();
        this.initColumns();
        // Single global signal: rebuild every TS-built label whenever the language changes
        // (and once immediately). Columns are translated in the template, so they're not rebuilt here.
        this._lang.onLanguageChange()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.buildSortOptions();
                this.buildFormActions();
                this.buildSearchOptions();
            });
    }

    protected buildSortOptions(): void {
        this.sortOptions = [
            new MenuOption(this._translate.instant('cases.submitDate'), 'createdByWhen')
        ];
    }

    protected buildFormActions(): void {
        const actions: ViewAction[] = [
            new RouterViewAction(Icons.EntryView, 'queries', 'rightpane', this._translate.instant('cases.mySearches'), this._translate.instant('cases.mySearches')),
            new ViewAction('refresh', 'refresh', null, Icons.Refresh, this._translate.instant('cases.refresh'))
        ];
        if (this._canCreateCase) {
            actions.unshift(
                new RouterViewAction(Icons.Add, this.newItemLink, 'rightpane', this._translate.instant('cases.submitNewCase'), this._translate.instant('cases.newCase'))
            );
        }
        this.formActions = actions;
    }

    protected initColumns() {
        //default columns are already in this.columns so if environment variables is empty then return
        if (settings.caseListColumns === '') {
            return;
        }
        const defaultColumnTitles = this.columns.map(x => x.title);
        const configColumns = settings.caseListColumns.split(',');

        for (const title of defaultColumnTitles) {
            //if environment variables do not have the already existing columns that we display then remove them
            if (!configColumns.includes(title)) {
                //renew column list to remove the titles that were not found
                this.columns = this.columns.filter(x => x.title != title);
            }
        }
    }

    protected setDefaultColumns() {
        return [
            { title: 'ReferenceNumber' },
            { title: 'CustomerId', itemProperty: 'ownerId' },
            { title: 'CustomerName', itemProperty: 'ownerName' },
            { title: 'TaxId', itemProperty: 'ownerTin' },
            { title: 'GroupId' },
            { title: 'CaseType', itemProperty: 'caseType.title' },
            { title: 'CheckpointType', itemProperty: 'checkpointType.title' },
            { title: 'AssignedTo', itemProperty: 'assignedToName' },
            { title: 'SubmitDate', itemProperty: 'createdByWhen' }
        ];
    }

    public initialize(): void {
        this.initColumns();

        const key = this.getFilterCacheKey();
        const storedParams = this._filterCachingService.getParams(key);
        if (storedParams) {
            this._router.navigate([], {
                relativeTo: this._route,
                queryParams: storedParams
            });
        }
        //Are there any filters in queryParams?
        this._route.queryParams.subscribe((params: Params) => {
            this.queryParamsHasFilter = params['filter'] ? true : false;
        });
        forkJoin({
            caseTypes: this._api.getCaseTypesList(),
            checkpointTypes: this._caseTypeService.getDistinctCheckpointTypes()
        }).pipe(take(1)).subscribe(({ caseTypes, checkpointTypes }) => {
            //TODO: this should not be needed - we assign this so its available for the async calls
            this.caseTypes = caseTypes;
            this._checkpointTypes = checkpointTypes;
            this.buildSearchOptions();
            // now that we have the searchOptions, call parent's ngOnInit!
            super.ngOnInit();
        });
    }

    /**
     * Builds the table's search/filter options with translated labels. Re-runs on language change
     * using the cached {@link caseTypes} / {@link _checkpointTypes}, so the filter labels stay localized.
     */
    protected buildSearchOptions(): void {
        const caseTypes = this.caseTypes;
        if (!caseTypes) {
            return;
        }
        const tempSearchOptions: SearchOption[] = [];
        if (this.tableFilters.ReferenceNumber) {
            tempSearchOptions.push({
                field: 'referenceNumber',
                name: this._translate.instant('cases.referenceNumber'),
                dataType: 'string'
            });
        }
        if (this.tableFilters.OwnerId) {
            tempSearchOptions.push({
                field: 'ownerId',
                name: this._translate.instant('cases.customerId'),
                dataType: 'string'
            });
        }
        if (this.tableFilters.OwnerName) {
            tempSearchOptions.push({
                field: 'ownerName',
                name: this._translate.instant('cases.customerName'),
                dataType: 'string'
            });
        }
        if (this.tableFilters.TaxId) {
            tempSearchOptions.push({
                field: 'TaxId',
                name: this._translate.instant('cases.taxId'),
                dataType: 'string'
            });
        }
        if (this.tableFilters.GroupIds) {
            tempSearchOptions.push({
                field: 'groupIds',
                name: this._translate.instant('cases.groupId'),
                dataType: 'string',
                multiTerm: true
            });
        }
        if (this.tableFilters.DateRange) {
            tempSearchOptions.push({
                field: 'dateRange',
                name: this._translate.instant('cases.submitDate'),
                dataType: 'daterange'
            });
        }
        if (this.tableFilters.CaseTypeCodes) {
            const caseTypeSearchOption = this.getCaseTypeSearchOption(caseTypes);
            tempSearchOptions.push(caseTypeSearchOption);
        }
        if (this.tableFilters.CheckpointTypeCodes) {
            const checkpointTypeSearchOption = this.getCaseTypeCheckpoints(this._checkpointTypes);
            tempSearchOptions.push(checkpointTypeSearchOption);
        }
        const otherSearchOptions = this.getOtherSearchOptions(caseTypes);
        if (otherSearchOptions) {
            tempSearchOptions.push(...otherSearchOptions);
        }
        this.searchOptions = tempSearchOptions;
    }

    public loadItems(): Observable<IResultSet<CasePartial> | null | undefined> {
        let ownerIds: string[] = [];
        this.filters?.filter(f => f.member === 'ownerId')?.forEach(f => ownerIds.push(this.stringifyFilterClause(f)));
        let ownerNames: string[] = [];
        this.filters?.filter(f => f.member === 'ownerName')?.forEach(f => ownerNames.push(this.stringifyFilterClause(f)));
        let ownerTins: string[] = [];
        this.filters?.filter(f => f.member === 'TaxId')?.forEach(f => ownerTins?.push(this.stringifyFilterClause(f)));
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
        
        const extraMetadataFilters = this.getExtraMetadataFilters(this.caseTypes);
        if (extraMetadataFilters) {
            filterMetadata?.push(...extraMetadataFilters)
        }
        this._filterCachingService.setParams(this.getFilterCacheKey(), {
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
                this.page,
                this.pageSize,
                this.sortdir === 'asc' ? this.sort! : this.sort + '-',
                this.search || undefined,
                ownerIds,
                ownerNames,
                ownerTins,
                from ? new Date(from) : undefined,
                to ? new Date(to) : undefined,
                undefined,
                caseTypeCodes,
                checkpointTypeCodes,
                groupIds,
                filterMetadata,
                referenceNumbers,
                undefined,
                undefined
            )
            .pipe(
                take(1),
                map((result: CasePartialResultSet) => (result as IResultSet<CasePartial>))
            );
    }

    private createNewCaseButton(): void {
        //Independent call to fetch the case Types that the user can select for Case Creation
        this._caseTypeService.getCanCreateCaseTypes()
            .pipe(take(1))
            .subscribe(
                (caseTypesForCaseCreation: CasePartialResultSet) => {
                    if (caseTypesForCaseCreation.count !== 0) {
                        this._canCreateCase = true;
                        this.buildFormActions();
                    }
                }
            );
    }

    public openQueryModal(): void {
        this._modalService.show(QueriesModalComponent, {
            backdrop: 'static',
            keyboard: false
        });
    }

    protected getOtherSearchOptions(caseTypes: CaseTypePartialResultSet): SearchOption[] | undefined {
        return undefined;
    }

    protected getCaseTypeCheckpoints(checkpointTypes: CheckpointType[]) {
        const checkpointTypeSearchOption: SearchOption = {
            field: 'checkpointTypeCodes',
            name: this._translate.instant('cases.checkpointType'),
            dataType: 'array',
            options: [],
            multiTerm: true
        }
        for (let checkpointType of checkpointTypes) { // fill checkpointTypeSearchOption's SelectInputOptions
            checkpointTypeSearchOption.options?.push({ value: checkpointType?.code, label: checkpointType?.title ?? checkpointType?.code! })
        }
        return checkpointTypeSearchOption;
    }

    //add all case types to search options
    protected getCaseTypeSearchOption(caseTypes: CaseTypePartialResultSet) {
        const caseTypeSearchOption: SearchOption = {
            field: 'caseTypeCodes',
            name: this._translate.instant('cases.caseType'),
            dataType: 'array',
            options: [],
            multiTerm: true
        }
        for (let caseType of caseTypes.items!) { //Fill caseTypeSearchOption's SelectInputOptions
            caseTypeSearchOption.options?.push({ value: caseType.code, label: caseType?.title! })
        }
        return caseTypeSearchOption;
    }

    protected getFilterCacheKey(): string {
        return "cases";
    }

    protected getExtraMetadataFilters(caseTypes: CaseTypePartialResultSet | undefined): string[] | undefined {
        return undefined;
    }

    private loadFilterSettings(): void {
        if (settings.caseListFilters === '') return;
        const filters = settings.caseListFilters.split(',')
        this.tableFilters.ReferenceNumber = filters.some(filter => filter === "ReferenceNumber");
        this.tableFilters.OwnerId = filters.some(filter => filter === "CustomerId");
        this.tableFilters.OwnerName = filters.some(filter => filter === "CustomerName");
        this.tableFilters.TaxId = filters.some(filter => filter === "TaxId");
        this.tableFilters.GroupIds = filters.some(filter => filter === "GroupIds");
        this.tableFilters.DateRange = filters.some(filter => filter === "DateRange");
        this.tableFilters.CaseTypeCodes = filters.some(filter => filter === "CaseTypeCodes");
        this.tableFilters.CheckpointTypeCodes = filters.some(filter => filter === "CheckpointTypeCodes");
    }

    //TODO: make this public in Indice.Angular
    private stringifyFilters(filters: FilterClause[] | undefined) {
        return filters?.map((f: FilterClause) => {
            if (f.dataType === 'datetime') {
                f.value = (new Date(f.value)).toISOString();
            }
            return f.toString();
        }).join(',');
    }

    public stringifyFilterClause(filter: FilterClause): string {
        return `${filter.member}::${filter.operator}::${filter.value}`;
    }
}

class TableFilters {
    ReferenceNumber: boolean = false;
    OwnerId: boolean = true;
    OwnerName: boolean = true;
    TaxId: boolean = true;
    GroupIds: boolean = true;
    DateRange: boolean = true;
    CaseTypeCodes: boolean = true;
    CheckpointTypeCodes: boolean = true;
}
