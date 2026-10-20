import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { take } from 'rxjs/operators';
import { CaseTypePartialResultSet, CheckpointType } from 'src/app/core/services/cases-api.service';
import { GridColumn, ValueFromPathPipe } from 'src/app/shared/pipes';
import { EmptyStateComponent } from 'src/app/ui/feedback/empty-state.component';
import { FilterBarComponent } from 'src/app/ui/list/filter-bar.component';
import { SearchOption } from 'src/app/ui/list/filtering';
import { PagerComponent } from 'src/app/ui/list/pager.component';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';
import { PersonComponent } from 'src/app/ui/people/person.component';
import { StatusMarkComponent } from 'src/app/ui/status/case-status';
import { GeneralCasesComponent } from '../general-cases/general-cases.component';

/**
 * The cases of one case type (sidebar shortcuts). The case-type filter is fixed, the columns and extra filters come from
 * the case type's `gridColumnConfig` / `gridFilterConfig`, and extra filters are sent as metadata clauses.
 */
@Component({
  selector: 'app-case-type-specific-cases',
  imports: [
    DatePipe, DecimalPipe, RouterLink, NgIcon, TranslatePipe, ValueFromPathPipe,
    PageHeaderComponent, FilterBarComponent, PagerComponent, EmptyStateComponent, StatusMarkComponent, PersonComponent
  ],
  templateUrl: '../general-cases/general-cases.component.html'
})
export class CaseTypeSpecificCasesComponent extends GeneralCasesComponent implements OnInit {

  public override ngOnInit(): void {
    // Re-initialise whenever the sidebar switches to another case type (the component instance is reused).
    this.route.params
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.items.set(null);
        this.initialize();
      });
    super.ngOnInit();
  }

  /** The case type the list was last initialised for; the base ngOnInit calls initialize() again for the same one. */
  private initializedFor: string | undefined;

  public override initialize(): void {
    if (this.initializedFor === this.getFilterCacheKey()) {
      return;
    }
    this.initializedFor = this.getFilterCacheKey();
    super.initialize();
  }

  protected override initColumns(): void {
    super.initColumns();
    this.caseTypeService.getCaseType(this.getFilterCacheKey()).pipe(take(1)).subscribe(caseType => {
      // A case type may define its own columns.
      const gridColumns: GridColumn[] = caseType?.gridColumnConfig || [];
      if (gridColumns.length > 0) {
        this.columns.set(gridColumns);
      } else {
        this.columns.set(this.defaultColumns());
        super.initColumns();
      }
    });
  }

  protected override getOtherSearchOptions(caseTypes: CaseTypePartialResultSet): SearchOption[] | undefined {
    const code = this.getFilterCacheKey();
    const caseType = caseTypes?.items?.find(x => x.code == code);
    return caseType?.gridFilterConfig || [];
  }

  protected override getCaseTypeCheckpoints(checkpointTypes: CheckpointType[]): SearchOption {
    return {
      field: 'checkpointTypeCodes',
      name: this.translate.instant('cases.checkpointType'),
      dataType: 'array',
      multiTerm: true,
      options: checkpointTypes.map(checkpointType => ({ value: checkpointType?.code, label: checkpointType?.title ?? checkpointType?.code! }))
    };
  }

  /** Only this case type, as a chip that cannot be removed. */
  protected override getCaseTypeSearchOption(caseTypes: CaseTypePartialResultSet): SearchOption {
    const code = this.getFilterCacheKey();
    const caseType = caseTypes.items?.find(x => x.code == code);
    this.caseTypeTitle.set(caseType?.title ?? '');
    return {
      field: 'caseTypeCodes',
      name: this.translate.instant('cases.caseType'),
      dataType: 'array',
      multiTerm: true,
      readonly: true,
      options: [{ value: caseType?.code, label: caseType?.title! }]
    };
  }

  /** The case type's extra filter fields are matched against case metadata. */
  protected override getExtraMetadataFilters(caseTypes: CaseTypePartialResultSet | undefined): string[] | undefined {
    const code = this.getFilterCacheKey();
    const caseType = caseTypes?.items?.find(x => x.code == code);
    const metadata: string[] = [];
    if (caseType?.gridFilterConfig) {
      const fields: string[] = caseType.gridFilterConfig.map((config: any) => config.field);
      for (const field of fields) {
        this.filters().filter(f => f.member === field).forEach(f => metadata.push(`metadata.${this.stringifyFilterClause(f)}`));
      }
    }
    return metadata;
  }

  protected override getFilterCacheKey(): string {
    return this.route.snapshot.paramMap.get('caseTypeCode')!;
  }
}
