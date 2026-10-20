import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '@indice/ng-auth';
import { Observable, filter, forkJoin } from 'rxjs';
import {
  LucideArrowDown, LucideArrowUp, LucideBookmark, LucideBookmarkPlus, LucideCircleAlert, LucideFolderSearch, LucidePlus,
  LucideRefreshCw, LucideSearchX
} from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasePartial, CasesApiService, CaseTypePartial, CheckpointType } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { QueriesModalComponent, QueryModalData } from 'src/app/shared/components/query-modal/query-modal.component';
import { ValueFromPathPipe } from 'src/app/shared/pipes.services';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterBarComponent } from 'src/app/ui/list/filter-bar.component';
import { FilterClause, SearchOption } from 'src/app/ui/list/filter-clause';
import { ListQuery, ListState, ResultSet } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';

/** A list column as configured in settings (`caseListColumns`) or a case type's `gridColumnConfig`. */
export interface CaseColumnConfig {
  title: string;
  itemProperty?: string;
}

interface CaseColumn extends CaseColumnConfig {
  label: string;
  kind: 'reference' | 'checkpoint' | 'date' | 'text';
}

/** Filters shown by default; `settings.caseListFilters` (comma separated) overrides the set. */
const DEFAULT_FILTERS: Record<string, boolean> = {
  ReferenceNumber: false,
  CustomerId: true,
  CustomerName: true,
  TaxId: true,
  GroupIds: true,
  DateRange: true,
  CaseTypeCodes: true,
  CheckpointTypeCodes: true
};

const DEFAULT_COLUMNS: CaseColumnConfig[] = [
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

const ASSIGNED_TO = 'assignedTo';

/** Template dependencies, shared with {@link CaseTypeSpecificCasesComponent} which reuses the template. */
export const CASES_LIST_IMPORTS = [
  DatePipe, RouterLink, TranslatePipe, ValueFromPathPipe, PageHeaderComponent, SearchInputComponent, FilterBarComponent, PaginationComponent,
  EmptyStateComponent, LucidePlus, LucideRefreshCw, LucideBookmark, LucideBookmarkPlus, LucideArrowUp, LucideArrowDown, LucideFolderSearch,
  LucideSearchX, LucideCircleAlert
];

/**
 * The cases list. Also serves `/case/by-type/:caseTypeCode` (see {@link CaseTypeSpecificCasesComponent}), where the
 * route param arrives through the `caseTypeCode` input and narrows the list, columns and filters to that case type.
 */
@Component({
  selector: 'app-general-cases',
  imports: CASES_LIST_IMPORTS,
  templateUrl: './general-cases.component.html'
})
export class GeneralCasesComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _router = inject(Router);
  private readonly _route = inject(ActivatedRoute);
  private readonly _auth = inject(AuthService);
  private readonly _translate = inject(TranslateService);
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _filterCaching = inject(FilterCachingService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `:caseTypeCode` route param on case type pages; undefined on `/cases`. */
  public readonly caseTypeCode = input<string>();

  private readonly _lang = toSignal(inject(AppLanguagesService).onLanguageChange());
  private readonly _caseTypes = signal<CaseTypePartial[] | undefined>(undefined);
  private readonly _checkpointTypes = signal<CheckpointType[]>([]);
  protected readonly canCreateCase = signal(false);

  protected readonly list = new ListState<CasePartial>({
    pageSize: 10,
    sort: 'createdByWhen',
    dir: 'desc',
    load: query => this.loadItems(query)
  });

  protected readonly currentCaseType = computed(() => {
    const code = this.caseTypeCode();
    return code ? this._caseTypes()?.find(c => c.code === code) : undefined;
  });

  protected readonly assignedToMe = computed(() => this.list.filters().some(f => f.member === ASSIGNED_TO));

  protected readonly searchOptions = computed<SearchOption[]>(() => {
    this._lang();
    const caseTypes = this._caseTypes();
    if (!caseTypes) {
      return [];
    }
    const t = (key: string) => this._translate.instant(key);
    const enabled = this.enabledFilters();
    const options: SearchOption[] = [];
    if (enabled.ReferenceNumber) {
      options.push({ field: 'referenceNumber', name: t('cases.referenceNumber'), dataType: 'string' });
    }
    if (enabled.CustomerId) {
      options.push({ field: 'ownerId', name: t('cases.customerId'), dataType: 'string' });
    }
    if (enabled.CustomerName) {
      options.push({ field: 'ownerName', name: t('cases.customerName'), dataType: 'string' });
    }
    if (enabled.TaxId) {
      options.push({ field: 'TaxId', name: t('cases.taxId'), dataType: 'string' });
    }
    if (enabled.GroupIds) {
      options.push({ field: 'groupIds', name: t('cases.groupId'), dataType: 'string', multiTerm: true });
    }
    if (enabled.DateRange) {
      options.push({ field: 'dateRange', name: t('cases.submitDate'), dataType: 'daterange' });
    }
    if (enabled.CaseTypeCodes) {
      const current = this.currentCaseType();
      options.push(current
        ? { field: 'caseTypeCodes', name: t('cases.caseType'), dataType: 'array', multiTerm: true, readonly: true, options: [{ value: current.code, label: current.title ?? current.code! }] }
        : { field: 'caseTypeCodes', name: t('cases.caseType'), dataType: 'array', multiTerm: true, options: caseTypes.map(c => ({ value: c.code, label: c.title ?? c.code! })) });
    }
    if (enabled.CheckpointTypeCodes) {
      options.push({
        field: 'checkpointTypeCodes',
        name: t('cases.checkpointType'),
        dataType: 'array',
        multiTerm: true,
        options: this._checkpointTypes().map(c => ({ value: c.code, label: c.title ?? c.code! }))
      });
    }
    // case type pages may add filters on case metadata
    options.push(...((this.currentCaseType()?.gridFilterConfig as SearchOption[] | undefined) ?? []));
    return options;
  });

  protected readonly columns = computed<CaseColumn[]>(() => {
    this._lang();
    const gridColumns = this.currentCaseType()?.gridColumnConfig as CaseColumnConfig[] | undefined;
    const configs = gridColumns?.length ? gridColumns : this.configuredDefaultColumns();
    return configs.map(column => ({
      ...column,
      label: this.columnLabel(column.title),
      kind: column.title === 'ReferenceNumber' ? 'reference'
        : column.itemProperty === 'checkpointType.title' ? 'checkpoint'
        : column.itemProperty === 'createdByWhen' ? 'date'
        : 'text'
    }));
  });

  protected readonly title = computed(() => {
    this._lang();
    return this.currentCaseType()?.title ?? this._translate.instant('cases.title');
  });

  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6, 7, 8];

  constructor() {
    forkJoin({
      caseTypes: this._api.getCaseTypesList(),
      checkpointTypes: this._caseTypeService.getCheckpointTypes(true)
    }).subscribe(({ caseTypes, checkpointTypes }) => {
      this._checkpointTypes.set(checkpointTypes);
      this._caseTypes.set(caseTypes.items ?? []);
    });
    this._caseTypeService.getCanCreateCaseTypes().subscribe(result => this.canCreateCase.set((result.count ?? 0) > 0));

    // (re)activate once case types are known and whenever the case type route param changes (component reuse)
    effect(() => {
      if (!this._caseTypes()) {
        return;
      }
      const code = this.caseTypeCode();
      untracked(() => this.activate(code));
    });
  }

  protected toggleAssignedToMe(): void {
    const others = this.list.filters().filter(f => f.member !== ASSIGNED_TO);
    const userId = this._auth.getSubjectId();
    this.list.setFilters(this.assignedToMe() || !userId ? others : [...others, new FilterClause(ASSIGNED_TO, userId)]);
  }

  protected clearFilters(): void {
    const readonlyFields = new Set(this.searchOptions().filter(o => o.readonly).map(o => o.field));
    this.list.update({ filters: this.list.filters().filter(f => readonlyFields.has(f.member)), search: '' });
  }

  protected sortBySubmitDate(): void {
    this.list.setSort('createdByWhen');
  }

  protected openCase(item: CasePartial): void {
    this._router.navigate(['/cases', item.id, 'details']);
  }

  protected saveSearch(): void {
    const q = this.list.query;
    // page always resets to 1; same format as previously saved searches so they keep applying to /cases
    const parameters = `?page=1&pagesize=${q.pageSize}&search=${encodeURIComponent(q.search)}&sort=${q.sort}&dir=${q.dir}&filter=${FilterClause.stringifyList(q.filters)}`;
    this._dialog.open<boolean, QueryModalData>(QueriesModalComponent, { data: { parameters } })
      .closed.pipe(filter(Boolean))
      .subscribe(() => this._toast.success('toasts.success.title', 'toasts.searchSaved.body'));
  }

  protected statusClass(item: CasePartial): string {
    const status = item.checkpointType?.status;
    return `stamp stamp-${status ? status.toLowerCase() : 'neutral'}`;
  }

  private activate(code: string | undefined): void {
    const params = this._route.snapshot.queryParamMap;
    if (params.keys.length === 0) {
      const stored = this._filterCaching.getParams(this.cacheKey());
      if (stored) {
        this._router.navigate([], { relativeTo: this._route, queryParams: stored, replaceUrl: true }).then(() => this.startList());
        return;
      } else if (code) {
        // land on a case type page without params (e.g. a typed URL): show its readonly case type filter
        this._router.navigate([], {
          relativeTo: this._route,
          queryParams: { filter: new FilterClause('caseTypeCodes', code, 'eq', 'array').toString() },
          replaceUrl: true
        }).then(() => this.startList());
        return;
      }
    }
    this.startList();
  }

  /** Starts the list once; afterwards query param changes (incl. switching case type pages) reload it by themselves. */
  private startList(): void {
    if (!this.list.started) {
      this.list.start();
    }
  }

  private loadItems(query: ListQuery): Observable<ResultSet<CasePartial>> {
    const terms = (member: string) => query.filters.filter(f => f.member === member).map(f => f.toApiTerm());
    const dateValue = (op: string) => query.filters.find(f => f.member === 'dateRange' && f.operator === op)?.value;
    const from = dateValue('gte');
    const to = dateValue('lte');
    const code = this.routeCaseTypeCode();
    // case type pages always stay within their case type, whatever the URL says
    const caseTypeCodes = code ? [`caseTypeCodes::eq::${code}`] : terms('caseTypeCodes');
    const caseType = code ? this._caseTypes()?.find(c => c.code === code) : undefined;
    const metadataFields: string[] = ((caseType?.gridFilterConfig as SearchOption[] | undefined) ?? []).map(c => c.field);
    const metadata = metadataFields.flatMap(field => terms(field).map(term => `metadata.${term}`));
    this._filterCaching.setParams(this.cacheKey(), this.list.toQueryParams());
    return this._api.getCases(
      query.page,
      query.pageSize,
      query.dir === 'asc' ? query.sort : `${query.sort}-`,
      query.search || undefined,
      terms('ownerId'),
      terms('ownerName'),
      terms('TaxId'),
      from ? new Date(from) : undefined,
      to ? new Date(to) : undefined,
      undefined,
      caseTypeCodes,
      terms('checkpointTypeCodes'),
      terms('groupIds'),
      metadata,
      terms('referenceNumber'),
      undefined,
      undefined,
      query.filters.find(f => f.member === ASSIGNED_TO)?.value || undefined
    );
  }

  private cacheKey(): string {
    return this.routeCaseTypeCode() ?? 'cases';
  }

  /**
   * The case type code from the route snapshot. When the component is reused between case type pages the router
   * emits the new query params before the `caseTypeCode` input updates, but the snapshot is already current.
   */
  private routeCaseTypeCode(): string | undefined {
    return this._route.snapshot.paramMap.get('caseTypeCode') ?? undefined;
  }

  private enabledFilters(): Record<string, boolean> {
    if (!settings.caseListFilters) {
      return DEFAULT_FILTERS;
    }
    const configured = settings.caseListFilters.split(',').map(f => f.trim());
    return Object.fromEntries(Object.keys(DEFAULT_FILTERS).map(key => [key, configured.includes(key)]));
  }

  private configuredDefaultColumns(): CaseColumnConfig[] {
    if (!settings.caseListColumns) {
      return DEFAULT_COLUMNS;
    }
    const configured = settings.caseListColumns.split(',').map(c => c.trim());
    return DEFAULT_COLUMNS.filter(c => configured.includes(c.title));
  }

  /** `cases.<camelCaseTitle>` when translated, otherwise the configured title as is. */
  private columnLabel(title: string): string {
    const key = `cases.${title[0].toLowerCase()}${title.slice(1)}`;
    const translated = this._translate.instant(key);
    return translated && translated !== key ? translated : title;
  }
}
