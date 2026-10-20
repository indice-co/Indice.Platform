import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Params, RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin, Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { settings } from 'src/app/core/models/settings';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasePartial, CasePartialResultSet, CasesApiService, CaseTypePartialResultSet, CheckpointType } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { GridColumn, ValueFromPathPipe } from 'src/app/shared/pipes';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { DialogService } from 'src/app/ui/feedback/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/feedback/empty-state.component';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { FilterBarComponent } from 'src/app/ui/list/filter-bar.component';
import { FilterClause, IResultSet, Operators, SearchOption, stringifyFilters } from 'src/app/ui/list/filtering';
import { ListPage } from 'src/app/ui/list/list-page';
import { PagerComponent } from 'src/app/ui/list/pager.component';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';
import { PersonComponent } from 'src/app/ui/people/person.component';
import { StatusMarkComponent } from 'src/app/ui/status/case-status';
import { SaveSearchDialogComponent } from '../save-search-dialog/save-search-dialog.component';

/** Columns that hold identifiers rather than words or quantities. */
const ID_COLUMNS = new Set(['ReferenceNumber', 'referenceNumber', 'ownerId', 'ownerTin', 'GroupId', 'groupId']);

/** All the cases the user can see, filterable by the configured fields and savable as "My searches". */
@Component({
  selector: 'app-general-cases',
  imports: [
    DatePipe, DecimalPipe, RouterLink, NgIcon, TranslatePipe, ValueFromPathPipe,
    PageHeaderComponent, FilterBarComponent, PagerComponent, EmptyStateComponent, StatusMarkComponent, PersonComponent
  ],
  templateUrl: './general-cases.component.html'
})
export class GeneralCasesComponent extends ListPage<CasePartial> implements OnInit {
  protected readonly api = inject(CasesApiService);
  protected readonly filterCaching = inject(FilterCachingService);
  protected readonly caseTypeService = inject(CaseTypeService);
  protected readonly translate = inject(TranslateService);
  protected readonly lang = inject(AppLanguagesService);
  private readonly dialogs = inject(DialogService);
  private readonly toaster = inject(ToastService);

  public readonly newItemLink = 'new-case';
  public readonly tableFilters = new TableFilters();
  public readonly caseTypeTitle = signal('');
  public readonly columns = signal<GridColumn[]>(this.defaultColumns());
  /** The user can create cases of at least one type. */
  public readonly canCreateCase = signal(false);
  public readonly filterOpen = signal(false);
  /** The query string carries filters, so the list can be saved as a search. */
  public readonly queryParamsHasFilter = signal(false);
  protected caseTypes: CaseTypePartialResultSet | undefined;
  /** Cached so search-option labels can be rebuilt on language change without re-fetching. */
  protected checkpointTypes: CheckpointType[] = [];

  protected readonly removableFilterCount = computed(() => {
    const readonlyFields = new Set(this.searchOptions().filter(o => o.readonly).map(o => o.field));
    return this.filters().filter(f => !readonlyFields.has(f.member)).length;
  });
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6];

  constructor() {
    super();
    this.view = 'table';
    this.pageSize.set(10);
    this.sort.set('createdByWhen');
    this.sortdir.set('desc');
    this.loadFilterSettings();
  }

  public ngOnInit(): void {
    this.initialize();
    this.loadCanCreate();
    // Rebuild every label built in TypeScript whenever the language changes (and once now).
    this.lang.onLanguageChange()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.buildSortOptions();
        this.buildSearchOptions();
      });
  }

  public initialize(): void {
    this.initColumns();
    const storedParams = this.filterCaching.getParams(this.getFilterCacheKey());
    if (storedParams) {
      this.router.navigate([], { relativeTo: this.route, queryParams: storedParams });
    }
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params: Params) => this.queryParamsHasFilter.set(!!params['filter']));
    forkJoin({
      caseTypes: this.api.getCaseTypesList(),
      checkpointTypes: this.caseTypeService.getCheckpointTypes(true)
    }).pipe(take(1)).subscribe(({ caseTypes, checkpointTypes }) => {
      this.caseTypes = caseTypes;
      this.checkpointTypes = checkpointTypes;
      this.buildSearchOptions();
      this.startList();
    });
  }

  public loadItems(): Observable<IResultSet<CasePartial> | null | undefined> {
    const clauses = (member: string) => this.filters().filter(f => f.member === member).map(f => this.stringifyFilterClause(f));
    const from = this.filters().find(f => f.member === 'dateRange' && f.operator === Operators.GREATER_THAN_EQUAL.value)?.value;
    const to = this.filters().find(f => f.member === 'dateRange' && f.operator === Operators.LESS_THAN_EQUAL.value)?.value;
    const filterMetadata: string[] = [...(this.getExtraMetadataFilters(this.caseTypes) ?? [])];
    this.filterCaching.setParams(this.getFilterCacheKey(), {
      view: this.view,
      page: this.page(),
      pagesize: this.pageSize(),
      search: this.search(),
      sort: this.sort(),
      dir: this.sortdir(),
      filter: stringifyFilters(this.filters())
    });
    return this.api
      .getCases(
        this.page(),
        this.pageSize(),
        this.sortdir() === 'asc' ? this.sort()! : this.sort() + '-',
        this.search() || undefined,
        clauses('ownerId'),
        clauses('ownerName'),
        clauses('TaxId'),
        from ? new Date(from) : undefined,
        to ? new Date(to) : undefined,
        undefined,
        clauses('caseTypeCodes'),
        clauses('checkpointTypeCodes'),
        clauses('groupIds'),
        filterMetadata,
        clauses('referenceNumber'),
        undefined,
        undefined
      )
      .pipe(
        take(1),
        map((result: CasePartialResultSet) => result as IResultSet<CasePartial>)
      );
  }

  public openCase(item: CasePartial): void {
    this.router.navigate(['/cases', item.id, 'details']);
  }

  /** Row activation that leaves links (and new-tab clicks) to the browser. */
  public onRowClick(event: MouseEvent, item: CasePartial): void {
    if ((event.target as HTMLElement).closest('a, button') || event.ctrlKey || event.metaKey) {
      return;
    }
    this.openCase(item);
  }

  public toggleSortDirection(): void {
    this.sortdirChanged(this.sortdir() === 'asc' ? 'desc' : 'asc');
  }

  public openQueryModal(): void {
    // Re-built so no user-added parameters reach the server and a saved search always opens on page 1.
    const params = this.route.snapshot.queryParamMap;
    const parameters = `?view=${params.get('view')}&page=1&pagesize=${params.get('pagesize')}&search=${params.get('search')}&sort=${params.get('sort')}&dir=${params.get('dir')}&filter=${params.get('filter')}`;
    this.dialogs.open<SaveSearchDialogComponent, boolean>(SaveSearchDialogComponent, { data: { parameters } })
      .closed.subscribe(saved => {
        if (saved) {
          this.toaster.show(ToastType.Success, undefined, 'toasts.searchSaved.body');
        }
      });
  }

  /** Identifiers people read out and quote are set in the mono face. */
  public isIdColumn(column: GridColumn): boolean {
    return ID_COLUMNS.has(column.itemProperty ?? column.title);
  }

  /** Dates line up on the right, like every other figure in a column. */
  public isDateColumn(column: GridColumn): boolean {
    return (column.itemProperty ?? column.title) === 'createdByWhen';
  }

  /** Columns use their title as the translation key suffix (`cases.<camelCaseTitle>`). */
  public columnKey(column: GridColumn): string {
    return 'cases.' + column.title[0].toLowerCase() + column.title.slice(1);
  }

  public stringifyFilterClause(filter: FilterClause): string {
    return `${filter.member}::${filter.operator}::${filter.value}`;
  }

  protected buildSortOptions(): void {
    this.sortOptions.set([{ label: this.translate.instant('cases.submitDate'), value: 'createdByWhen' }]);
  }

  protected initColumns(): void {
    // The default columns are already set; the host configuration may only narrow them down.
    if (settings.caseListColumns === '') {
      return;
    }
    const configColumns = settings.caseListColumns.split(',');
    this.columns.update(columns => columns.filter(column => configColumns.includes(column.title)));
  }

  protected defaultColumns(): GridColumn[] {
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

  /** The table's filter fields, with translated labels. Re-runs on language change from the cached lookups. */
  protected buildSearchOptions(): void {
    const caseTypes = this.caseTypes;
    if (!caseTypes) {
      return;
    }
    const options: SearchOption[] = [];
    const t = (key: string) => this.translate.instant(key);
    if (this.tableFilters.ReferenceNumber) {
      options.push({ field: 'referenceNumber', name: t('cases.referenceNumber'), dataType: 'string' });
    }
    if (this.tableFilters.OwnerId) {
      options.push({ field: 'ownerId', name: t('cases.customerId'), dataType: 'string' });
    }
    if (this.tableFilters.OwnerName) {
      options.push({ field: 'ownerName', name: t('cases.customerName'), dataType: 'string' });
    }
    if (this.tableFilters.TaxId) {
      options.push({ field: 'TaxId', name: t('cases.taxId'), dataType: 'string' });
    }
    if (this.tableFilters.GroupIds) {
      options.push({ field: 'groupIds', name: t('cases.groupId'), dataType: 'string', multiTerm: true });
    }
    if (this.tableFilters.DateRange) {
      options.push({ field: 'dateRange', name: t('cases.submitDate'), dataType: 'daterange' });
    }
    if (this.tableFilters.CaseTypeCodes) {
      options.push(this.getCaseTypeSearchOption(caseTypes));
    }
    if (this.tableFilters.CheckpointTypeCodes) {
      options.push(this.getCaseTypeCheckpoints(this.checkpointTypes));
    }
    options.push(...(this.getOtherSearchOptions(caseTypes) ?? []));
    this.searchOptions.set(options);
    // Chips show option labels, so refresh them in the new language too.
    this.filters.update(filters => filters.map(f => new FilterClause(f.member, f.value, f.operator, f.dataType, options)));
  }

  protected getOtherSearchOptions(caseTypes: CaseTypePartialResultSet): SearchOption[] | undefined {
    return undefined;
  }

  protected getCaseTypeCheckpoints(checkpointTypes: CheckpointType[]): SearchOption {
    return {
      field: 'checkpointTypeCodes',
      name: this.translate.instant('cases.checkpointType'),
      dataType: 'array',
      multiTerm: true,
      options: checkpointTypes.map(checkpointType => ({ value: checkpointType?.code, label: checkpointType?.title ?? checkpointType?.code! }))
    };
  }

  protected getCaseTypeSearchOption(caseTypes: CaseTypePartialResultSet): SearchOption {
    return {
      field: 'caseTypeCodes',
      name: this.translate.instant('cases.caseType'),
      dataType: 'array',
      multiTerm: true,
      options: (caseTypes.items ?? []).map(caseType => ({ value: caseType.code, label: caseType?.title! }))
    };
  }

  protected getFilterCacheKey(): string {
    return 'cases';
  }

  protected getExtraMetadataFilters(caseTypes: CaseTypePartialResultSet | undefined): string[] | undefined {
    return undefined;
  }

  private loadCanCreate(): void {
    // Independent call: the case types the user may create cases for.
    this.caseTypeService.getCanCreateCaseTypes()
      .pipe(take(1))
      .subscribe((caseTypesForCaseCreation: CaseTypePartialResultSet) => this.canCreateCase.set(caseTypesForCaseCreation.count !== 0));
  }

  private loadFilterSettings(): void {
    if (settings.caseListFilters === '') {
      return;
    }
    const filters = settings.caseListFilters.split(',');
    this.tableFilters.ReferenceNumber = filters.includes('ReferenceNumber');
    this.tableFilters.OwnerId = filters.includes('CustomerId');
    this.tableFilters.OwnerName = filters.includes('CustomerName');
    this.tableFilters.TaxId = filters.includes('TaxId');
    this.tableFilters.GroupIds = filters.includes('GroupIds');
    this.tableFilters.DateRange = filters.includes('DateRange');
    this.tableFilters.CaseTypeCodes = filters.includes('CaseTypeCodes');
    this.tableFilters.CheckpointTypeCodes = filters.includes('CheckpointTypeCodes');
  }
}

class TableFilters {
  ReferenceNumber = false;
  OwnerId = true;
  OwnerName = true;
  TaxId = true;
  GroupIds = true;
  DateRange = true;
  CaseTypeCodes = true;
  CheckpointTypeCodes = true;
}
