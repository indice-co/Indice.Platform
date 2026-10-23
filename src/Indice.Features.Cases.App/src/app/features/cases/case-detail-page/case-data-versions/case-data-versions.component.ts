import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { map } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { JsonForms } from '@jsonforms/angular';
import { createAjv, Generate } from '@jsonforms/core';
import { LucideGlobe, LucideHistory, LucideSearchX, LucideTriangleAlert } from '@lucide/angular';
import { Case, CaseDataVersion, CaseDataVersionResultSet, CasesApiService } from 'src/app/core/services/cases-api.service';
import { CasesJsonFormsConfig } from 'src/app/shared/jsonforms/jsonforms-utilities';
import { displayRenderers } from 'src/app/shared/jsonforms/tailwind-renderers';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';

const PAGE_SIZES = [10, 20, 50, 100];

interface LoadedPage {
  query: { page: number; size: number; search: string };
  result: CaseDataVersionResultSet;
}

/**
 * The stored versions of the case data, newest first, paged and searchable by author. Picking a version shows its data
 * read-only, laid out by the case type like the case form.
 */
@Component({
  selector: 'app-case-data-versions',
  imports: [
    DatePipe, TranslatePipe, JsonForms, EmptyStateComponent, PaginationComponent, SearchInputComponent,
    LucideGlobe, LucideHistory, LucideSearchX, LucideTriangleAlert
  ],
  host: { class: 'block' },
  template: `
    @if (versions.error()) {
      <app-empty-state [title]="'caseDetails.versions.title' | translate" [message]="'caseDetails.versions.loadError' | translate">
        <svg icon lucideTriangleAlert class="size-6"></svg>
        <button actions type="button" class="btn btn-sm" (click)="versions.reload()">{{ 'common.retry' | translate }}</button>
      </app-empty-state>
    } @else if (!loaded()) {
      <div class="grid place-items-center py-14" aria-busy="true"><span class="loading loading-spinner"></span></div>
    } @else if (!count() && !loaded()!.query.search) {
      <app-empty-state [title]="'caseDetails.versions.empty' | translate">
        <svg icon lucideHistory class="size-6"></svg>
      </app-empty-state>
    } @else {
      <div class="border-b border-base-300 p-2">
        <app-search-input class="block md:w-72" [value]="search()" [placeholder]="'caseDetails.versions.search' | translate"
          (search)="setSearch($event)" />
      </div>
      @if (!items().length) {
        <app-empty-state [title]="'list.noResults' | translate">
          <svg icon lucideSearchX class="size-6"></svg>
          <button actions type="button" class="btn btn-sm" (click)="setSearch('')">{{ 'caseDetails.versions.clearSearch' | translate }}</button>
        </app-empty-state>
      } @else {
      <div class="grid transition-opacity md:grid-cols-[15rem_minmax(0,1fr)]" [class.opacity-60]="versions.isLoading()"
        [attr.aria-busy]="versions.isLoading()">
        <nav class="border-base-300 max-md:border-b md:border-r" [attr.aria-label]="'caseDetails.versions.title' | translate">
          <ul class="max-h-72 overflow-y-auto p-2 md:max-h-[36rem]">
            @for (version of items(); track version.id; let i = $index) {
              @let active = version.id === selected()?.id;
              <li>
                <button type="button" class="flex w-full flex-col items-start gap-1 rounded-field px-3 py-2 text-left"
                  [class]="active ? 'bg-base-200' : 'hover:bg-base-200/60'" [attr.aria-current]="active ? 'true' : null"
                  (click)="selectedId.set(version.id)">
                  <span class="flex w-full items-center gap-1.5">
                    @if (numberOf()(i); as number) {
                      <span class="docket text-sm font-semibold">v{{ number }}</span>
                    }
                    @if (version.isCurrent) {
                      <span class="badge badge-xs badge-primary badge-soft">{{ 'caseDetails.versions.current' | translate }}</span>
                    }
                    @if (version.isPublic) {
                      <span class="badge badge-xs badge-info badge-soft gap-0.5" [title]="'caseDetails.versions.publicHint' | translate">
                        <svg lucideGlobe class="size-3"></svg>{{ 'caseDetails.versions.public' | translate }}
                      </span>
                    }
                  </span>
                  <time class="docket text-xs text-base-content/70" [attr.datetime]="version.createdBy?.when | date: 'yyyy-MM-ddTHH:mm:ss'">
                    {{ version.createdBy?.when | date: 'dd/MM/yyyy, HH:mm:ss' }}
                  </time>
                  <span class="w-full truncate text-xs text-base-content/60" [title]="version.createdBy?.email ?? ''">
                    {{ version.createdBy?.name || version.createdBy?.email || '-' }}
                  </span>
                </button>
              </li>
            }
          </ul>
        </nav>

        <div class="min-w-0 p-5 sm:p-6">
          @if (selected(); as version) {
            <p class="mb-5 text-sm text-base-content/70">
              {{ 'caseDetails.versions.createdBy' | translate: { name: version.createdBy?.name || version.createdBy?.email || '-' } }},
              <span class="docket">{{ version.createdBy?.when | date: 'dd/MM/yyyy, HH:mm:ss' }}</span>
            </p>
            <div class="disableCheckbox">
              @for (id of [version.id]; track id) {
                <jsonforms [schema]="form().schema" [uischema]="form().uiSchema" [data]="version.data ?? {}" [renderers]="renderers"
                  [readonly]="true" [ajv]="ajv" [config]="config" />
              }
            </div>
          }
        </div>
      </div>
      }
      @if (count() > pageSizes[0]) {
        <app-pagination class="border-t border-base-300 px-4 py-3" [page]="page()" [pageSize]="size()" [count]="count()"
          [pageSizes]="pageSizes" (pageChange)="page.set($event)" (pageSizeChange)="setSize($event)" />
      }
    }
  `
})
export class CaseDataVersionsComponent {
  private readonly _api = inject(CasesApiService);

  /** The case whose data versions are listed; a new instance (a refresh, e.g. after a save) reloads them. */
  public readonly case = input<Case | undefined>();

  protected readonly renderers = displayRenderers;
  protected readonly ajv = createAjv();
  protected readonly config: CasesJsonFormsConfig = {};
  protected readonly pageSizes = PAGE_SIZES;

  protected readonly page = signal(1);
  protected readonly size = signal(20);
  protected readonly search = signal('');

  protected readonly versions = rxResource({
    params: () => ({ case: this.case(), page: this.page(), size: this.size(), search: this.search() }),
    stream: ({ params }) => this._api
      .getCaseDataVersions(params.case!.id!, params.page, params.size, undefined, params.search || undefined)
      .pipe(map(result => ({ query: params, result })))
  });
  /** The last loaded page and its query, kept while the next one loads so the list (and the search box focus) survive paging and searching. */
  protected readonly loaded = linkedSignal<LoadedPage | undefined, LoadedPage | undefined>({
    source: this.versions.value,
    computation: (value, previous) => value ?? previous?.value
  });
  protected readonly items = computed<CaseDataVersion[]>(() => this.loaded()?.result.items ?? []);
  protected readonly count = computed(() => this.loaded()?.result.count ?? 0);
  /** Version numbers only make sense over the whole, unfiltered history. */
  protected readonly numberOf = computed(() => {
    const query = this.loaded()?.query;
    const offset = query && !query.search ? this.count() - (query.page - 1) * query.size : undefined;
    return (index: number) => offset === undefined ? undefined : offset - index;
  });
  /** Keeps the picked version across reloads while it still exists, otherwise the newest one. */
  protected readonly selectedId = linkedSignal<CaseDataVersion[], string | undefined>({
    source: this.items,
    computation: (items, previous) => items.some(v => v.id === previous?.value) ? previous!.value : items[0]?.id
  });
  protected readonly selected = computed(() => this.items().find(v => v.id === this.selectedId()));

  protected readonly form = computed(() => {
    const caseType = this.case()?.caseType;
    const schema = parse(caseType?.dataSchema) ?? {};
    return { schema, uiSchema: structuredClone(caseType?.uiSchema) ?? Generate.uiSchema(schema) };
  });

  protected setSearch(search: string): void {
    this.search.set(search);
    this.page.set(1);
  }

  protected setSize(size: number): void {
    this.size.set(size);
    this.page.set(1);
  }
}

/** Case type schemas may arrive as JSON strings or as objects. */
function parse(value: any): any {
  return typeof value === 'string' ? JSON.parse(value) : value;
}
